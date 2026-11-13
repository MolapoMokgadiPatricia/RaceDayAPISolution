using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models.ViewModels;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers
{
    public class EventsController : AppController
    {
        public EventsController(RaceDayApiClient api) : base(api) { }

        public async Task<IActionResult> Index()
        {
            var result = await EventCards.LoadAsync(Api);
            if (!result.Success)
            {
                Error(result.Message);
            }

            return View(new EventListVm { Events = result.Cards });
        }

        public async Task<IActionResult> Details(int id)
        {
            var eventResponse = await Api.GetEventAsync(id);
            if (!eventResponse.Success || eventResponse.Data == null)
            {
                return eventResponse.IsNotFound
                    ? NotFound()
                    : View("Error", new ErrorVm { Code = eventResponse.StatusCode, Message = eventResponse.Message });
            }

            var categories = await Api.GetCategoriesForEventAsync(id);

            var model = new EventDetailsVm
            {
                Event = eventResponse.Data,
                Categories = categories.Data ?? new(),
                IsOwner = IsOrganiser && eventResponse.Data.OrganiserID == CurrentUserId
            };

            if (IsParticipant)
            {
                var mine = await Api.GetParticipantEnrolmentsAsync(CurrentUserId);
                if (mine.IsUnauthorized)
                {
                    return RedirectIfSessionEnded(mine)!;
                }

                model.MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventID == id);
                model.MyCategory = model.Categories.FirstOrDefault(c => c.CategoryID == model.MyEnrolment?.CategoryID);
            }

            return View(model);
        }
    }
}
