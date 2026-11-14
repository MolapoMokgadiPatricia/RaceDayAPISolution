using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Filters;
using RaceDayMVC.Models.ViewModels;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers
{
    public class AccountController : AppController
    {
        private const long MaxImageBytes = 5 * 1024 * 1024;
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

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

            await StartSessionAsync(response.Data.UserID, response.Data.Role, $"{response.Data.FirstName} {response.Data.LastName}");
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

            if (model.Role == "Participant" && model.ProfilePicture is { Length: > 0 })
            {
                string? pictureProblem = ValidateImage(model.ProfilePicture);
                if (pictureProblem != null)
                {
                    ModelState.AddModelError(nameof(model.ProfilePicture), pictureProblem);
                }
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

            await StartSessionAsync(login.Data.UserID, login.Data.Role, $"{login.Data.FirstName} {login.Data.LastName}");

            if (login.Data.Role == "Participant")
            {
                if (model.ProfilePicture is { Length: > 0 })
                {
                    var upload = await Api.UploadProfilePictureAsync(model.ProfilePicture);
                    if (upload.Success)
                    {
                        HttpContext.Session.SetString(SessionKeys.PictureUrl, upload.Data?.ProfilePictureUrl ?? string.Empty);
                        Success($"Welcome to RaceDay, {login.Data.FirstName}.");
                        return RedirectToHomeForRole();
                    }

                    Warning($"Your account is ready, but the profile picture could not be uploaded: {upload.Message} You can try again below.");
                    return RedirectToAction(nameof(Profile));
                }

                // New participants without a picture land on their profile, where adding one is a single click.
                Success($"Welcome to RaceDay, {login.Data.FirstName}. Add a profile picture below if you like (optional).");
                return RedirectToAction(nameof(Profile));
            }

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

            if (response.Success)
            {
                HttpContext.Session.SetString(SessionKeys.PictureUrl, response.Data?.ProfilePictureUrl ?? string.Empty);
                Success("Your profile picture has been updated.");
            }
            else
            {
                Error(response.Message);
            }

            return RedirectToAction(nameof(Profile));
        }

        // --------------------------------------------------------- Helpers

        private async Task StartSessionAsync(int userId, string role, string fullName)
        {
            HttpContext.Session.SetInt32(SessionKeys.UserId, userId);
            HttpContext.Session.SetString(SessionKeys.Role, role);
            HttpContext.Session.SetString(SessionKeys.FullName, fullName.Trim());
            HttpContext.Session.SetString(SessionKeys.PictureUrl, string.Empty);

            // Cache the profile picture so the menu can show it without an extra call on every page.
            if (role == "Participant")
            {
                var profile = await Api.GetProfileAsync(userId);
                if (profile.Success && profile.Data?.ProfilePictureUrl is { Length: > 0 } url)
                {
                    HttpContext.Session.SetString(SessionKeys.PictureUrl, url);
                }
            }
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
            if (!ImageExtensions.Contains(ext)) return "Only JPG, PNG or WEBP images are allowed.";

            // Look at the real file signature so a renamed text file is turned away before anything is sent.
            byte[] header = new byte[12];
            int read;
            using (var stream = file.OpenReadStream())
            {
                read = stream.Read(header, 0, header.Length);
            }

            bool isJpeg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            bool isPng = read >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
            bool isWebp = read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                          header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

            return (isJpeg || isPng || isWebp) ? null : "That file is not a valid JPG, PNG or WEBP image.";
        }
    }
}
