using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDayMVC.Services;

namespace RaceDayMVC.Filters
{
    /// <summary>
    /// Protects MVC pages. Visitors who are not logged in go to the login page;
    /// logged-in users with the wrong role see the access-denied page.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public SessionAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            int? userId = session.GetInt32(SessionKeys.UserId);
            string? role = session.GetString(SessionKeys.Role);

            if (userId == null || string.IsNullOrEmpty(role))
            {
                string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl, reason = "signin" });
                return;
            }

            if (_roles.Length > 0 && !_roles.Contains(role))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }
}
