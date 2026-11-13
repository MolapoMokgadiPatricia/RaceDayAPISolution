using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Filters;
using RaceDayMVC.Models.ViewModels;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers
{
    [SessionAuthorize("Participant")]
    public class ParticipantController : AppController
    {
        public ParticipantController(RaceDayApiClient api) : base(api) { }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int eventId, int? categoryId)
        {
            if (categoryId is null or <= 0)
            {
                Error("Choose a category before entering the event.");
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            var response = await Api.CreateEnrolmentAsync(eventId, categoryId.Value);
            var ended = RedirectIfSessionEnded(response);
            if (ended != null) return ended;

            if (response.Success)
            {
                Success("You are entered. Your enrolment is pending until the organiser confirms it.");
                return RedirectToAction(nameof(MyEvents));
            }

            Error(response.Message);
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        public async Task<IActionResult> MyEvents()
        {
            var enrolments = await Api.GetParticipantEnrolmentsAsync(CurrentUserId);
            var ended = RedirectIfSessionEnded(enrolments);
            if (ended != null) return ended;

            if (!enrolments.Success)
            {
                Error(enrolments.Message);
                return View(new List<MyEnrolmentRow>());
            }

            var events = (await Api.GetEventsAsync()).Data ?? new();
            var categories = (await Api.GetCategoriesAsync()).Data ?? new();
            var history = (await Api.GetMyHistoryAsync()).Data ?? new();
            var withResult = history.Select(h => h.EnrolmentID).ToHashSet();

            var rows = (enrolments.Data ?? new())
                .Select(e => new MyEnrolmentRow
                {
                    Enrolment = e,
                    Event = events.FirstOrDefault(x => x.EventID == e.EventID),
                    Category = categories.FirstOrDefault(c => c.CategoryID == e.CategoryID),
                    HasResult = withResult.Contains(e.EnrolmentID)
                })
                .OrderBy(r => r.Event?.EventDate ?? DateTime.MaxValue)
                .ToList();

            return View(rows);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var response = await Api.DeleteEnrolmentAsync(id);
            var ended = RedirectIfSessionEnded(response);
            if (ended != null) return ended;

            if (response.Success) Success("Your enrolment has been cancelled.");
            else Error(response.Message);

            return RedirectToAction(nameof(MyEvents));
        }

        public async Task<IActionResult> MyResults()
        {
            var history = await Api.GetMyHistoryAsync();
            var ended = RedirectIfSessionEnded(history);
            if (ended != null) return ended;

            if (!history.Success)
            {
                Error(history.Message);
            }

            return View(new MyResultsVm { Rows = history.Data ?? new() });
        }
    }
}
