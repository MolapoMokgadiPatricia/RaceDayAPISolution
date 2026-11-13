using Microsoft.AspNetCore.Http;

namespace RaceDayMVC.Services
{
    public static class SessionExtensions
    {
        public static void SetUser(
            this ISession session,
            int userId,
            string role,
            string firstName,
            string email,
            string apiCookie)
        {
            session.SetInt32(SessionKeys.UserId, userId);
            session.SetString(SessionKeys.Role, role);
            session.SetString(SessionKeys.FirstName, firstName);
            session.SetString(SessionKeys.Email, email);
            session.SetString(SessionKeys.ApiCookie, apiCookie);
        }

        public static void ClearUser(this ISession session)
        {
            session.Clear();
        }

        public static int GetUserId(this ISession session)
            => session.GetInt32(SessionKeys.UserId) ?? 0;

        public static string GetRole(this ISession session)
            => session.GetString(SessionKeys.Role) ?? string.Empty;

        public static string GetFirstName(this ISession session)
            => session.GetString(SessionKeys.FirstName) ?? string.Empty;

        public static string GetEmail(this ISession session)
            => session.GetString(SessionKeys.Email) ?? string.Empty;

        public static string? GetApiCookie(this ISession session)
            => session.GetString(SessionKeys.ApiCookie);

        public static bool IsAuthenticated(this ISession session)
            => session.GetUserId() > 0;

        public static bool IsOrganiser(this ISession session)
            => session.GetRole() == "Organiser";

        public static bool IsParticipant(this ISession session)
            => session.GetRole() == "Participant";
    }
}
