using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public AuthorizeRoleAttribute()
        {
            _allowedRoles = Array.Empty<string>();
        }

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _allowedRoles = roles ?? Array.Empty<string>();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            int? userId = session.GetInt32("UserID");
            string? role = session.GetString("Role");

            if (userId == null || string.IsNullOrEmpty(role))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    message = "Please log in first."
                });
                return;
            }

            if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(role))
            {
                context.Result = new ObjectResult(new
                {
                    message = $"Access denied. This action requires one of the following roles: {string.Join(", ", _allowedRoles)}."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            context.HttpContext.Items["UserID"] = userId.Value;
            context.HttpContext.Items["Role"] = role;
        }
    }
}