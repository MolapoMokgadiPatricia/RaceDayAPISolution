using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Filters;
using RaceDayMVC.Helpers;
using RaceDayMVC.Models.Api;
using RaceDayMVC.Models.ViewModels;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers
{
    [SessionAuthorize("Organiser")]
    public class OrganiserController : AppController
    {
        private static readonly string[] EventTypes = { "run", "walk", "cycle" };
        private static readonly string[] AllowedStatuses = { "Pending", "Approved", "Rejected", "Cancelled" };

        public OrganiserController(RaceDayApiClient api) : base(api) { }

        // ------------------------------------------------------- Dashboard

        public async Task<IActionResult> Dashboard()
        {
            var (events, failure) = await LoadMyEventsAsync();
            if (failure != null) return failure;

            var enrolments = await Api.GetOrganiserEnrolmentsAsync();
            var ended = RedirectIfSessionEnded(enrolments);
            if (ended != null) return ended;

            var categories = (await Api.GetCategoriesAsync()).Data ?? new();
            var allEnrolments = enrolments.Data ?? new();

            var model = new DashboardVm
            {
                Name = HttpContext.Session.GetString(SessionKeys.FullName) ?? "Organiser",
                Rows = events.OrderBy(e => e.EventDate).Select(e => new DashboardEventRow
                {
                    Event = e,
                    Enrolments = allEnrolments.Count(x => x.EventID == e.EventID && x.Status != "Cancelled" && x.Status != "Rejected"),
                    Pending = allEnrolments.Count(x => x.EventID == e.EventID && (x.Status ?? "Pending") == "Pending"),
                    Categories = categories.Count(c => c.EventID == e.EventID)
                }).ToList()
            };

            if (!enrolments.Success) Error(enrolments.Message);
            return View(model);
        }

        // ---------------------------------------------------------- Events

        public async Task<IActionResult> Events()
        {
            var (events, failure) = await LoadMyEventsAsync();
            if (failure != null) return failure;

            var enrolments = (await Api.GetOrganiserEnrolmentsAsync()).Data ?? new();
            var categories = (await Api.GetCategoriesAsync()).Data ?? new();

            var rows = events.OrderBy(e => e.EventDate).Select(e => new DashboardEventRow
            {
                Event = e,
                Enrolments = enrolments.Count(x => x.EventID == e.EventID && x.Status != "Cancelled" && x.Status != "Rejected"),
                Pending = enrolments.Count(x => x.EventID == e.EventID && (x.Status ?? "Pending") == "Pending"),
                Categories = categories.Count(c => c.EventID == e.EventID)
            }).ToList();

            return View(rows);
        }

        [HttpGet]
        public IActionResult CreateEvent() => View("EventForm", new EventFormVm { EventDate = DateTime.Today.AddDays(30) });

        [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> CreateEvent(EventFormVm model)
        {
            ValidateEventForm(model);
            if (!ModelState.IsValid) return View("EventForm", model);

            var created = await Api.CreateEventAsync(ToApiEvent(model));
            var ended = RedirectIfSessionEnded(created);
            if (ended != null) return ended;

            if (!created.Success || created.Data == null)
            {
                ModelState.AddModelError(string.Empty, created.Message);
                return View("EventForm", model);
            }

            if (model.Banner is { Length: > 0 })
            {
                var upload = await Api.UploadEventBannerAsync(created.Data.EventID, model.Banner);
                if (!upload.Success)
                {
                    Warning($"The event was created, but the banner could not be uploaded: {upload.Message}");
                    return RedirectToAction(nameof(EditEvent), new { id = created.Data.EventID });
                }
            }

            Success($"\"{created.Data.Name}\" has been created. Add categories so participants can enter.");
            return RedirectToAction(nameof(Categories), new { eventId = created.Data.EventID });
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var response = await Api.GetEventAsync(id);
            var ended = RedirectIfSessionEnded(response);
            if (ended != null) return ended;

            if (!response.Success || response.Data == null || response.Data.OrganiserID != CurrentUserId)
            {
                Error(response.Success ? "You can only edit your own events." : response.Message);
                return RedirectToAction(nameof(Events));
            }

            var e = response.Data;
            return View("EventForm", new EventFormVm
            {
                EventId = e.EventID,
                Name = e.Name,
                Description = e.Description,
                EventDate = e.EventDate.Date,
                Location = e.Location,
                DistanceKm = e.DistanceKm,
                EventType = e.EventType,
                BannerImageUrl = e.BannerImageUrl
            });
        }

        [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> EditEvent(int id, EventFormVm model)
        {
            model.EventId = id;
            ValidateEventForm(model);
            if (!ModelState.IsValid) return View("EventForm", model);

            var updated = await Api.UpdateEventAsync(id, ToApiEvent(model));
            var ended = RedirectIfSessionEnded(updated);
            if (ended != null) return ended;

            if (!updated.Success)
            {
                ModelState.AddModelError(string.Empty, updated.Message);
                return View("EventForm", model);
            }

            if (model.Banner is { Length: > 0 })
            {
                var upload = await Api.UploadEventBannerAsync(id, model.Banner);
                if (!upload.Success)
                {
                    Warning($"Your changes were saved, but the banner could not be uploaded: {upload.Message}");
                    return RedirectToAction(nameof(EditEvent), new { id });
                }
            }

            Success("Event updated.");
            return RedirectToAction(nameof(Events));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var response = await Api.DeleteEventAsync(id);
            var ended = RedirectIfSessionEnded(response);
            if (ended != null) return ended;

            if (response.Success) Success("Event deleted.");
            else Error(response.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase) || response.StatusCode >= 500
                ? "This event could not be deleted because it already has enrolments."
                : response.Message);

            return RedirectToAction(nameof(Events));
        }

        // ------------------------------------------------------ Categories

        public async Task<IActionResult> Categories(int? eventId)
        {
            var (events, failure) = await LoadMyEventsAsync();
            if (failure != null) return failure;

            var model = new CategoriesVm { Events = events.OrderBy(e => e.EventDate).ToList() };
            model.Selected = PickEvent(model.Events, eventId);

            if (model.Selected != null)
            {
                var cats = await Api.GetCategoriesForEventAsync(model.Selected.EventID);
                model.Categories = cats.Data ?? new();
                model.Form.EventId = model.Selected.EventID;
            }

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCategory(CategoryFormVm form)
        {
            if (form.MinAge.HasValue && form.MaxAge.HasValue && form.MaxAge < form.MinAge)
            {
                ModelState.AddModelError(nameof(form.MaxAge), "Maximum age must be the same as or higher than minimum age.");
            }

            if (!ModelState.IsValid)
            {
                Error(string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return RedirectToAction(nameof(Categories), new { eventId = form.EventId });
            }

            var category = new ApiCategory
            {
                CategoryID = form.CategoryId,
                EventID = form.EventId,
                Name = form.Name.Trim(),
                MinAge = form.MinAge!.Value,
                MaxAge = form.MaxAge!.Value,
                DistanceKm = form.DistanceKm!.Value
            };

            var response = form.CategoryId > 0
                ? (ApiResponse)await Api.UpdateCategoryAsync(form.CategoryId, category)
                : await Api.CreateCategoryAsync(category);

            var ended = RedirectIfSessionEnded(response);
            if (ended != null) return ended;

            if (response.Success) Success(form.CategoryId > 0 ? "Category updated." : "Category added.");
            else Error(response.Message);

            return RedirectToAction(nameof(Categories), new { eventId = form.EventId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id, int eventId)
        {
            var response = await Api.DeleteCategoryAsync(id);
            var ended = RedirectIfSessionEnded(response);
            if (ended != null) return ended;

            if (response.Success) Success("Category deleted.");
            else Error(response.Message);

            return RedirectToAction(nameof(Categories), new { eventId });
        }

        // ------------------------------------------------------ Enrolments

        public async Task<IActionResult> Enrolments(int? eventId)
        {
            var (events, failure) = await LoadMyEventsAsync();
            if (failure != null) return failure;

            var model = new EnrolmentsVm { Events = events.OrderBy(e => e.EventDate).ToList() };
            model.Selected = PickEvent(model.Events, eventId);

            if (model.Selected != null)
            {
                var enrolments = await Api.GetEventEnrolmentsAsync(model.Selected.EventID);
                var ended = RedirectIfSessionEnded(enrolments);
                if (ended != null) return ended;

                if (!enrolments.Success) Error(enrolments.Message);

                var users = (await Api.GetUsersAsync()).Data ?? new();
                var categories = (await Api.GetCategoriesForEventAsync(model.Selected.EventID)).Data ?? new();

                model.Rows = (enrolments.Data ?? new()).Select(e =>
                {
                    var user = users.FirstOrDefault(u => u.UserID == e.ParticipantID);
                    return new EnrolmentRow
                    {
                        Enrolment = e,
                        ParticipantName = user?.FullName ?? $"Participant #{e.ParticipantID}",
                        Email = user?.Email ?? string.Empty,
                        CategoryName = categories.FirstOrDefault(c => c.CategoryID == e.CategoryID)?.Name ?? "-"
                    };
                }).ToList();
            }

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEnrolmentStatus(int id, int eventId, string status)
        {
            if (!AllowedStatuses.Contains(status))
            {
                Error("Choose a valid status.");
                return RedirectToAction(nameof(Enrolments), new { eventId });
            }

            var response = await Api.UpdateEnrolmentStatusAsync(id, status);
            var ended = RedirectIfSessionEnded(response);
            if (ended != null) return ended;

            if (response.Success) Success($"Enrolment marked as {UiHelpers.StatusLabel(status)}.");
            else Error(response.Message);

            return RedirectToAction(nameof(Enrolments), new { eventId });
        }

        // --------------------------------------------------------- Results

        public async Task<IActionResult> Results(int? eventId)
        {
            var (events, failure) = await LoadMyEventsAsync();
            if (failure != null) return failure;

            var model = new ResultsVm { Events = events.OrderBy(e => e.EventDate).ToList() };
            model.Selected = PickEvent(model.Events, eventId);

            if (model.Selected != null)
            {
                var enrolments = await Api.GetEventEnrolmentsAsync(model.Selected.EventID);
                var ended = RedirectIfSessionEnded(enrolments);
                if (ended != null) return ended;

                var users = (await Api.GetUsersAsync()).Data ?? new();
                var categories = (await Api.GetCategoriesForEventAsync(model.Selected.EventID)).Data ?? new();
                var results = (await Api.GetResultsAsync()).Data ?? new();

                model.Rows = (enrolments.Data ?? new())
                    .Where(e => e.Status != "Rejected" && e.Status != "Cancelled")
                    .Select(e =>
                    {
                        var user = users.FirstOrDefault(u => u.UserID == e.ParticipantID);
                        var result = results.FirstOrDefault(r => r.EnrolmentID == e.EnrolmentID);
                        return new ResultRowVm
                        {
                            EnrolmentId = e.EnrolmentID,
                            ResultId = result?.ResultID,
                            ParticipantName = user?.FullName ?? $"Participant #{e.ParticipantID}",
                            CategoryName = categories.FirstOrDefault(c => c.CategoryID == e.CategoryID)?.Name ?? "-",
                            Status = e.Status,
                            TimeText = result?.FinishTime == null ? null : UiHelpers.FormatTime(result.FinishTime),
                            Position = result?.FinishPosition
                        };
                    })
                    .OrderBy(r => r.Position ?? int.MaxValue)
                    .ThenBy(r => r.ParticipantName)
                    .ToList();
            }

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResults(int eventId, List<ResultRowVm> rows)
        {
            int saved = 0;
            var problems = new List<string>();

            foreach (var row in rows ?? new())
            {
                bool hasTime = !string.IsNullOrWhiteSpace(row.TimeText);
                bool hasPosition = row.Position.HasValue;

                if (!hasTime && !hasPosition)
                {
                    continue;
                }

                if (!UiHelpers.TryParseTime(row.TimeText, out var time))
                {
                    problems.Add($"{row.ParticipantName}: finish time must look like 1:35:20.");
                    continue;
                }

                if (row.Position is <= 0)
                {
                    problems.Add($"{row.ParticipantName}: position must be 1 or higher.");
                    continue;
                }

                var response = row.ResultId.HasValue
                    ? (ApiResponse)await Api.UpdateResultAsync(row.ResultId.Value, time, row.Position)
                    : await Api.CreateResultAsync(row.EnrolmentId, time, row.Position);

                var ended = RedirectIfSessionEnded(response);
                if (ended != null) return ended;

                if (response.Success) saved++;
                else problems.Add($"{row.ParticipantName}: {response.Message}");
            }

            if (saved > 0) Success($"{saved} result{(saved == 1 ? "" : "s")} saved and published.");
            if (problems.Count > 0) Error(string.Join(" ", problems));
            if (saved == 0 && problems.Count == 0) Warning("Enter a finish time or position for at least one participant.");

            return RedirectToAction(nameof(Results), new { eventId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResult(int id, int eventId)
        {
            var response = await Api.DeleteResultAsync(id);
            var ended = RedirectIfSessionEnded(response);
            if (ended != null) return ended;

            if (response.Success) Success("Result removed.");
            else Error(response.Message);

            return RedirectToAction(nameof(Results), new { eventId });
        }

        // --------------------------------------------------------- Helpers

        private async Task<(List<ApiEvent> Events, IActionResult? Failure)> LoadMyEventsAsync()
        {
            var response = await Api.GetEventsAsync();
            var ended = RedirectIfSessionEnded(response);
            if (ended != null) return (new(), ended);

            if (!response.Success)
            {
                Error(response.Message);
                return (new(), null);
            }

            return ((response.Data ?? new()).Where(e => e.OrganiserID == CurrentUserId).ToList(), null);
        }

        private static ApiEvent? PickEvent(List<ApiEvent> events, int? eventId)
            => events.FirstOrDefault(e => e.EventID == eventId)
               ?? events.FirstOrDefault(e => e.EventDate.Date >= DateTime.Today)
               ?? events.FirstOrDefault();

        private void ValidateEventForm(EventFormVm model)
        {
            if (!EventTypes.Contains(model.EventType))
            {
                ModelState.AddModelError(nameof(model.EventType), "Choose run, walk or cycle.");
            }

            if (model.Banner is { Length: > 0 })
            {
                string? problem = AccountController.ValidateImage(model.Banner);
                if (problem != null) ModelState.AddModelError(nameof(model.Banner), problem);
            }
        }

        private static ApiEvent ToApiEvent(EventFormVm m) => new()
        {
            EventID = m.EventId,
            Name = m.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(m.Description) ? null : m.Description.Trim(),
            EventDate = m.EventDate!.Value.Date,
            Location = m.Location.Trim(),
            DistanceKm = m.DistanceKm!.Value,
            EventType = m.EventType,
            BannerImageUrl = null
        };
    }
}
