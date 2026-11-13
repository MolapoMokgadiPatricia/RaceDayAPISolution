using System.Globalization;

namespace RaceDayMVC.Helpers
{
    public static class UiHelpers
    {
        /// <summary>The API stores "Approved"; the site shows "Confirmed".</summary>
        public static string StatusLabel(string? status) => (status ?? "Pending") switch
        {
            "Approved" => "Confirmed",
            var s => s
        };

        public static string StatusClass(string? status) => (status ?? "Pending") switch
        {
            "Approved" => "status-confirmed",
            "Rejected" => "status-rejected",
            "Cancelled" => "status-cancelled",
            _ => "status-pending"
        };

        public static string TypeLabel(string? type) => string.IsNullOrWhiteSpace(type)
            ? "Event"
            : char.ToUpperInvariant(type[0]) + type.Substring(1).ToLowerInvariant();

        public static string Ordinal(int n)
        {
            int mod100 = n % 100;
            if (mod100 is >= 11 and <= 13) return $"{n}th";
            return (n % 10) switch { 1 => $"{n}st", 2 => $"{n}nd", 3 => $"{n}rd", _ => $"{n}th" };
        }

        public static string FormatTime(TimeSpan? t)
        {
            if (t == null) return "-";
            var v = t.Value;
            int hours = (int)v.TotalHours;
            return $"{hours}:{v.Minutes:00}:{v.Seconds:00}";
        }

        public static bool TryParseTime(string? text, out TimeSpan? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text)) return true;

            var parts = text.Trim().Split(':');
            if (parts.Length is < 2 or > 3) return false;

            int[] n = new int[3];
            int offset = parts.Length == 2 ? 1 : 0;
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out n[i + offset])) return false;
            }

            if (n[1] > 59 || n[2] > 59) return false;
            var span = new TimeSpan(n[0], n[1], n[2]);
            if (span <= TimeSpan.Zero) return false;

            value = span;
            return true;
        }

        public static string Km(decimal km) => km.ToString("0.##", CultureInfo.InvariantCulture) + " km";

        public static string AgeRange(int min, int max) => max >= 100 ? $"{min}+" : $"{min} - {max}";

        /// <summary>Only http(s) image URLs are rendered; anything else falls back to the placeholder.</summary>
        public static string? SafeImage(string? url)
            => !string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var u) &&
               (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps) ? url : null;

        public static string Initials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 1
                ? parts[0].Substring(0, 1).ToUpperInvariant()
                : (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpperInvariant();
        }
    }
}
