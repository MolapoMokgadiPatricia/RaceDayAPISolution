using Microsoft.AspNetCore.Http;

namespace RaceDayMVC.Services
{
    public class ApiSessionHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiSessionHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var mvcSession = _httpContextAccessor.HttpContext?.Session;

            var apiCookie = mvcSession?.GetApiCookie();
            if (!string.IsNullOrEmpty(apiCookie))
            {
                request.Headers.Add("Cookie", $".RaceDay.Session={apiCookie}");
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                foreach (var header in setCookieHeaders)
                {
                    var parts = header.Split(';', StringSplitOptions.TrimEntries);
                    var kv = parts.FirstOrDefault(p => p.StartsWith(".RaceDay.Session="));
                    if (kv != null)
                    {
                        var value = kv.Substring(".RaceDay.Session=".Length);
                        if (string.IsNullOrEmpty(value))
                        {
                            mvcSession?.Remove(SessionKeys.ApiCookie);
                        }
                        else
                        {
                            mvcSession?.SetString(SessionKeys.ApiCookie, value);
                        }
                    }
                }
            }

            return response;
        }
    }
}