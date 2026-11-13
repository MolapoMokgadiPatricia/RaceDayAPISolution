using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers
{
    /// <summary>Shared helpers for every controller: session user, alerts and API failures.</summary>
    public abstract class AppController : Controller
    {
        protected readonly RaceDayApiClient Api;

        protected AppController(RaceDayApiClient api)
        {
            Api = api;
        }

        protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;
        protected string CurrentRole => HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;
        protected bool IsLoggedIn => CurrentUserId > 0;
        protected bool IsOrganiser => CurrentRole == "Organiser";
        protected bool IsParticipant => CurrentRole == "Participant";

        protected void Success(string message) => TempData["Success"] = message;
        protected void Error(string message) => TempData["Error"] = message;
        protected void Warning(string message) => TempData["Warning"] = message;

        /// <summary>
        /// If the API says the session has ended, clear ours and send the user to log in.
        /// Returns null when the response is not an authentication failure.
        /// </summary>
        protected IActionResult? RedirectIfSessionEnded(ApiResponse response)
        {
            if (!response.IsUnauthorized)
            {
                return null;
            }

            HttpContext.Session.Clear();
            Warning("Your session has ended. Please log in again.");
            return RedirectToAction("Login", "Account");
        }

        protected IActionResult RedirectToHomeForRole()
            => IsOrganiser
                ? RedirectToAction("Dashboard", "Organiser")
                : RedirectToAction("Index", "Home");
    }
}
