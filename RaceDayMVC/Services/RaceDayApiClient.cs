using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaceDayMVC.Models.Api;

namespace RaceDayMVC.Services
{
    /// <summary>
    /// The only place the MVC site talks to the RaceDay API.
    /// It forwards the API's session cookie (kept in this site's own session)
    /// so the API still sees the right user and role on every call.
    /// </summary>
    public class RaceDayApiClient
    {
        private const string ApiCookieName = ".RaceDay.Session";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;

        public RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor)
        {
            _http = http;
            _accessor = accessor;
        }

        private ISession? Session => _accessor.HttpContext?.Session;

        // ------------------------------------------------------------ Auth

        public Task<ApiResponse<ApiLoginResponse>> LoginAsync(string email, string password)
        {
            // Start from a clean API session so one browser never reuses another user's.
            Session?.Remove(SessionKeys.ApiCookie);
            return SendAsync<ApiLoginResponse>(HttpMethod.Post, "api/auth/login", Json(new { email, password }));
        }

        public Task<ApiResponse<ApiLoginResponse>> RegisterAsync(
            string firstName, string lastName, string email, string? phone, string password, string role)
            => SendAsync<ApiLoginResponse>(HttpMethod.Post, "api/auth/register",
                Json(new { firstName, lastName, email, phoneNumber = phone, password, role }));

        public Task<ApiResponse> LogoutAsync()
            => SendAsync(HttpMethod.Post, "api/auth/logout", null);

        // ---------------------------------------------------------- Events

        public Task<ApiResponse<List<ApiEvent>>> GetEventsAsync()
            => SendAsync<List<ApiEvent>>(HttpMethod.Get, "api/events");

        public Task<ApiResponse<ApiEvent>> GetEventAsync(int id)
            => SendAsync<ApiEvent>(HttpMethod.Get, $"api/events/{id}");

        public Task<ApiResponse<ApiEvent>> CreateEventAsync(ApiEvent ev)
            => SendAsync<ApiEvent>(HttpMethod.Post, "api/events", Json(EventBody(ev)));

        public Task<ApiResponse<ApiEvent>> UpdateEventAsync(int id, ApiEvent ev)
            => SendAsync<ApiEvent>(HttpMethod.Put, $"api/events/{id}", Json(EventBody(ev)));

        public Task<ApiResponse> DeleteEventAsync(int id)
            => SendAsync(HttpMethod.Delete, $"api/events/{id}");

        public Task<ApiResponse<ApiUploadResponse>> UploadEventBannerAsync(int eventId, IFormFile file)
            => SendAsync<ApiUploadResponse>(HttpMethod.Post, $"api/EventBanners/{eventId}/banner", FileContent(file));

        // ------------------------------------------------------ Categories

        public Task<ApiResponse<List<ApiCategory>>> GetCategoriesAsync()
            => SendAsync<List<ApiCategory>>(HttpMethod.Get, "api/categories");

        public Task<ApiResponse<List<ApiCategory>>> GetCategoriesForEventAsync(int eventId)
            => SendAsync<List<ApiCategory>>(HttpMethod.Get, $"api/categories/event/{eventId}");

        public Task<ApiResponse<ApiCategory>> CreateCategoryAsync(ApiCategory c)
            => SendAsync<ApiCategory>(HttpMethod.Post, "api/categories", Json(new
            {
                eventID = c.EventID, name = c.Name, minAge = c.MinAge, maxAge = c.MaxAge, distanceKm = c.DistanceKm
            }));

        public Task<ApiResponse<ApiCategory>> UpdateCategoryAsync(int id, ApiCategory c)
            => SendAsync<ApiCategory>(HttpMethod.Put, $"api/categories/{id}", Json(new
            {
                categoryID = id, eventID = c.EventID, name = c.Name, minAge = c.MinAge, maxAge = c.MaxAge, distanceKm = c.DistanceKm
            }));

        public Task<ApiResponse> DeleteCategoryAsync(int id)
            => SendAsync(HttpMethod.Delete, $"api/categories/{id}");

        // ------------------------------------------------------ Enrolments

        public Task<ApiResponse<List<ApiEnrolment>>> GetOrganiserEnrolmentsAsync()
            => SendAsync<List<ApiEnrolment>>(HttpMethod.Get, "api/enrolments");

        public Task<ApiResponse<List<ApiEnrolment>>> GetEventEnrolmentsAsync(int eventId)
            => SendAsync<List<ApiEnrolment>>(HttpMethod.Get, $"api/enrolments/event/{eventId}");

        public Task<ApiResponse<List<ApiEnrolment>>> GetParticipantEnrolmentsAsync(int participantId)
            => SendAsync<List<ApiEnrolment>>(HttpMethod.Get, $"api/enrolments/participant/{participantId}");

        public Task<ApiResponse<ApiEnrolment>> CreateEnrolmentAsync(int eventId, int categoryId)
            => SendAsync<ApiEnrolment>(HttpMethod.Post, "api/enrolments", Json(new { eventID = eventId, categoryID = categoryId }));

        public Task<ApiResponse<ApiEnrolment>> UpdateEnrolmentStatusAsync(int id, string status)
            => SendAsync<ApiEnrolment>(HttpMethod.Put, $"api/enrolments/{id}", Json(new { status }));

        public Task<ApiResponse> DeleteEnrolmentAsync(int id)
            => SendAsync(HttpMethod.Delete, $"api/enrolments/{id}");

        // ----------------------------------------------------------- Users

        public Task<ApiResponse<List<ApiUser>>> GetUsersAsync()
            => SendAsync<List<ApiUser>>(HttpMethod.Get, "api/users");

        public Task<ApiResponse<ApiUser>> GetProfileAsync(int id)
            => SendAsync<ApiUser>(HttpMethod.Get, $"api/users/{id}");

        public Task<ApiResponse<ApiUser>> UpdateProfileAsync(int id, object body)
            => SendAsync<ApiUser>(HttpMethod.Put, $"api/users/{id}", Json(body));

        public Task<ApiResponse<ApiUploadResponse>> UploadProfilePictureAsync(IFormFile file)
            => SendAsync<ApiUploadResponse>(HttpMethod.Post, "api/ProfilePictures", FileContent(file));

        // --------------------------------------------------------- Results

        public Task<ApiResponse<List<ApiResult>>> GetResultsAsync()
            => SendAsync<List<ApiResult>>(HttpMethod.Get, "api/results");

        public Task<ApiResponse<List<ApiHistoryRow>>> GetMyHistoryAsync()
            => SendAsync<List<ApiHistoryRow>>(HttpMethod.Get, "api/results/my-history");

        public Task<ApiResponse<ApiResult>> CreateResultAsync(int enrolmentId, TimeSpan? time, int? position)
            => SendAsync<ApiResult>(HttpMethod.Post, "api/results",
                Json(new { enrolmentID = enrolmentId, finishTime = time, finishPosition = position }));

        public Task<ApiResponse<ApiResult>> UpdateResultAsync(int id, TimeSpan? time, int? position)
            => SendAsync<ApiResult>(HttpMethod.Put, $"api/results/{id}",
                Json(new { finishTime = time, finishPosition = position }));

        public Task<ApiResponse> DeleteResultAsync(int id)
            => SendAsync(HttpMethod.Delete, $"api/results/{id}");

        // --------------------------------------------------------- Plumbing

        private static object EventBody(ApiEvent ev) => new
        {
            name = ev.Name,
            description = ev.Description,
            eventDate = ev.EventDate,
            location = ev.Location,
            distanceKm = ev.DistanceKm,
            eventType = ev.EventType,
            bannerImageUrl = ev.BannerImageUrl
        };

        private static StringContent Json(object body)
            => new(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        private static MultipartFormDataContent FileContent(IFormFile file)
        {
            var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(file.OpenReadStream());
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "file", Path.GetFileName(file.FileName));
            return content;
        }

        private async Task<ApiResponse> SendAsync(HttpMethod method, string url, HttpContent? content = null)
        {
            var (response, body) = await SendRawAsync(method, url, content);
            return new ApiResponse
            {
                Success = response.Success,
                StatusCode = response.StatusCode,
                Message = response.Success ? string.Empty : ExtractMessage(body, response.StatusCode, response.Message)
            };
        }

        private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content = null)
        {
            var (response, body) = await SendRawAsync(method, url, content);
            var result = new ApiResponse<T> { Success = response.Success, StatusCode = response.StatusCode };

            if (!response.Success)
            {
                result.Message = ExtractMessage(body, response.StatusCode, response.Message);
                return result;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(body))
                {
                    result.Data = JsonSerializer.Deserialize<T>(body, JsonOptions);
                }
            }
            catch (JsonException)
            {
                result.Success = false;
                result.StatusCode = 502;
                result.Message = "The server sent a response we could not read.";
            }

            return result;
        }

        private async Task<(ApiResponse Response, string Body)> SendRawAsync(HttpMethod method, string url, HttpContent? content)
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };

            string? cookie = Session?.GetString(SessionKeys.ApiCookie);
            if (!string.IsNullOrEmpty(cookie))
            {
                request.Headers.TryAddWithoutValidation("Cookie", $"{ApiCookieName}={cookie}");
            }

            try
            {
                using var response = await _http.SendAsync(request);
                CaptureApiCookie(response);

                string body = await response.Content.ReadAsStringAsync();
                return (new ApiResponse { Success = response.IsSuccessStatusCode, StatusCode = (int)response.StatusCode }, body);
            }
            catch (HttpRequestException)
            {
                return (Unavailable("RaceDay could not reach the server. Please try again shortly."), string.Empty);
            }
            catch (TaskCanceledException)
            {
                return (Unavailable("The server took too long to respond. Please try again."), string.Empty);
            }
        }

        private static ApiResponse Unavailable(string message)
            => new() { Success = false, StatusCode = 503, Message = message };

        private void CaptureApiCookie(HttpResponseMessage response)
        {
            if (Session == null || !response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                return;
            }

            foreach (string header in cookies)
            {
                if (!header.StartsWith(ApiCookieName + "=", StringComparison.Ordinal))
                {
                    continue;
                }

                string value = header.Substring(ApiCookieName.Length + 1).Split(';')[0];
                if (!string.IsNullOrEmpty(value))
                {
                    Session.SetString(SessionKeys.ApiCookie, value);
                }
            }
        }

        private static string ExtractMessage(string body, int status, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback;
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    {
                        return message.GetString() ?? string.Empty;
                    }

                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var parts = new List<string>();
                        foreach (var field in errors.EnumerateObject())
                        {
                            foreach (var item in field.Value.EnumerateArray())
                            {
                                parts.Add(item.GetString() ?? string.Empty);
                            }
                        }

                        if (parts.Count > 0)
                        {
                            return string.Join(" ", parts);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // fall through to the friendly default below
            }

            return status switch
            {
                400 => "Some of the details are not valid. Please check and try again.",
                401 => "Please log in to continue.",
                403 => "You do not have permission to do that.",
                404 => "We could not find what you were looking for.",
                409 => "That conflicts with existing information.",
                >= 500 => "Something went wrong on our side. Please try again.",
                _ => "The request could not be completed."
            };
        }
    }
}
