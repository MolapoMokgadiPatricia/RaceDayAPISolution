using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Filters;
using RaceDayMVC.Models.ViewModels;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers
{
    public class AccountController : AppController
    {
        private const long MaxImageBytes = 5 * 1024 * 1024;
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        public AccountController(RaceDayApiClient api) : base(api) { }

        // ----------------------------------------------------------- Login

        [HttpGet]
        public IActionResult Login(string? returnUrl = null, string? reason = null)
        {
            if (IsLoggedIn)
            {
                return RedirectToHomeForRole();
            }

            if (reason == "signin")
            {
                ViewData["Info"] = "Log in to see that page.";
            }

            return View(new LoginVm { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await Api.LoginAsync(model.Email.Trim(), model.Password);
            if (!response.Success || response.Data == null)
            {
                ModelState.AddModelError(string.Empty, response.IsUnauthorized
                    ? "That email and password do not match. Check them and try again."
                    : response.Message);
                model.Password = string.Empty;
                return View(model);
            }

            StartSession(response.Data.UserID, response.Data.Role, $"{response.Data.FirstName} {response.Data.LastName}");
            Success($"Welcome back, {response.Data.FirstName}.");

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToHomeForRole();
        }

        // -------------------------------------------------------- Register

        [HttpGet]
        public IActionResult Register(string? role = null)
        {
            if (IsLoggedIn)
            {
                return RedirectToHomeForRole();
            }

            return View(new RegisterVm { Role = role == "Organiser" ? "Organiser" : "Participant" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVm model)
        {
            if (model.Role != "Participant" && model.Role != "Organiser")
            {
                ModelState.AddModelError(nameof(model.Role), "Choose Participant or Organiser.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var register = await Api.RegisterAsync(
                model.FirstName, model.LastName, model.Email.Trim(), model.PhoneNumber, model.Password, model.Role);

            if (!register.Success)
            {
                ModelState.AddModelError(string.Empty, register.Message);
                return View(model);
            }

            var login = await Api.LoginAsync(model.Email.Trim(), model.Password);
            if (!login.Success || login.Data == null)
            {
                Success("Your account is ready. Log in to get started.");
                return RedirectToAction(nameof(Login));
            }

            StartSession(login.Data.UserID, login.Data.Role, $"{login.Data.FirstName} {login.Data.LastName}");
            Success($"Welcome to RaceDay, {login.Data.FirstName}.");
            return RedirectToHomeForRole();
        }

        // ---------------------------------------------------------- Logout

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await Api.LogoutAsync();
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied() => View();

        // --------------------------------------------------------- Profile

        [HttpGet, SessionAuthorize]
        public async Task<IActionResult> Profile()
        {
            var response = await Api.GetProfileAsync(CurrentUserId);
            var ended = RedirectIfSessionEnded(response);
            if (ended != null) return ended;

            if (!response.Success || response.Data == null)
            {
                Error(response.Message);
                return RedirectToHomeForRole();
            }

            return View(ToVm(response.Data));
        }

        [HttpPost, SessionAuthorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileVm model)
        {
            if (model.DateOfBirth.HasValue && model.DateOfBirth.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth cannot be in the future.");
            }

            if (!ModelState.IsValid)
            {
                await FillReadOnlyFieldsAsync(model);
                return View(model);
            }

            object body = IsParticipant
                ? new
                {
                    firstName = model.FirstName,
                    lastName = model.LastName,
                    phoneNumber = model.PhoneNumber ?? string.Empty,
                    dateOfBirth = model.DateOfBirth,
                    emergencyContactName = model.EmergencyContactName ?? string.Empty,
                    emergencyContactPhone = model.EmergencyContactPhone ?? string.Empty
                }
                : new
                {
                    firstName = model.FirstName,
                    lastName = model.LastName,
                    phoneNumber = model.PhoneNumber ?? string.Empty
                };

            var response = await Api.UpdateProfileAsync(CurrentUserId, body);
            var ended = RedirectIfSessionEnded(response);
            if (ended != null) return ended;

            if (!response.Success)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                await FillReadOnlyFieldsAsync(model);
                return View(model);
            }

            HttpContext.Session.SetString(SessionKeys.FullName, $"{model.FirstName} {model.LastName}".Trim());
            Success("Your profile has been updated.");
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost, SessionAuthorize("Participant"), ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadPicture(IFormFile? picture)
        {
            string? problem = ValidateImage(picture);
            if (problem != null)
            {
                Error(problem);
                return RedirectToAction(nameof(Profile));
            }

            var response = await Api.UploadProfilePictureAsync(picture!);
            var ended = RedirectIfSessionEnded(response);
            if (ended != null) return ended;

            if (response.Success) Success("Your profile picture has been updated.");
            else Error(response.Message);

            return RedirectToAction(nameof(Profile));
        }

        // --------------------------------------------------------- Helpers

        private void StartSession(int userId, string role, string fullName)
        {
            HttpContext.Session.SetInt32(SessionKeys.UserId, userId);
            HttpContext.Session.SetString(SessionKeys.Role, role);
            HttpContext.Session.SetString(SessionKeys.FullName, fullName.Trim());
        }

        private static ProfileVm ToVm(Models.Api.ApiUser u) => new()
        {
            UserId = u.UserID,
            Role = u.Role,
            Email = u.Email,
            MemberSince = u.CreatedAt,
            FirstName = u.FirstName,
            LastName = u.LastName,
            PhoneNumber = u.PhoneNumber,
            DateOfBirth = u.DateOfBirth,
            EmergencyContactName = u.EmergencyContactName,
            EmergencyContactPhone = u.EmergencyContactPhone,
            ProfilePictureUrl = u.ProfilePictureUrl
        };

        private async Task FillReadOnlyFieldsAsync(ProfileVm model)
        {
            var response = await Api.GetProfileAsync(CurrentUserId);
            if (response.Data != null)
            {
                model.UserId = response.Data.UserID;
                model.Role = response.Data.Role;
                model.Email = response.Data.Email;
                model.MemberSince = response.Data.CreatedAt;
                model.ProfilePictureUrl = response.Data.ProfilePictureUrl;
            }
        }

        internal static string? ValidateImage(IFormFile? file)
        {
            if (file == null || file.Length == 0) return "Choose an image to upload.";
            if (file.Length > MaxImageBytes) return "The image must be 5 MB or smaller.";

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ImageExtensions.Contains(ext)) return "Only JPG, PNG, WEBP or GIF images are allowed.";

            return null;
        }
    }
}
