namespace RaceDayAPI.Extensions
{
    public static class SessionExtensions
    {
        public static int GetUserId(this HttpContext context)
        {
            return context.Session.GetInt32("UserID") ?? 0;
        }

        public static string GetUserRole(this HttpContext context)
        {
            return context.Session.GetString("Role") ?? string.Empty;
        }

        public static bool IsOrganiser(this HttpContext context)
        {
            return context.GetUserRole() == "Organiser";
        }

        public static bool IsParticipant(this HttpContext context)
        {
            return context.GetUserRole() == "Participant";
        }
    }
}