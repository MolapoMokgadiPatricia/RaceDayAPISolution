namespace RaceDayMVC.Services
{
    /// <summary>Outcome of one call to the RaceDay API.</summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;

        public bool IsUnauthorized => StatusCode == 401;
        public bool IsForbidden => StatusCode == 403;
        public bool IsNotFound => StatusCode == 404;
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }
    }
}
