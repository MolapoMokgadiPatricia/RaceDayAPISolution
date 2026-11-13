using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models.ViewModels;
using RaceDayMVC.Services;

namespace RaceDayMVC.Controllers
{
    public class HomeController : AppController
    {
        public HomeController(RaceDayApiClient api) : base(api) { }

        public async Task<IActionResult> Index()
        {
            var model = await EventCards.LoadAsync(Api);
            if (!model.Success)
            {
                Error(model.Message);
            }

            return View(new EventListVm { Events = model.Cards });
        }

        public IActionResult StatusPage(int code)
        {
            Response.StatusCode = code;
            return View("Error", code == 404
                ? new ErrorVm { Code = 404, Title = "Page not found", Message = "That page does not exist. Head back to the events and pick up the race from there." }
                : new ErrorVm { Code = code, Title = "Something went wrong", Message = "Please try again in a moment." });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            Response.StatusCode = 500;
            return View(new ErrorVm
            {
                Code = 500,
                Title = "Something went wrong",
                Message = "We hit a problem on our side. Please try again in a moment."
            });
        }
    }

    /// <summary>Builds event cards (event + category count) from two API calls.</summary>
    public static class EventCards
    {
        public record Result(bool Success, string Message, List<EventCardVm> Cards);

        public static async Task<Result> LoadAsync(RaceDayApiClient api)
        {
            var events = await api.GetEventsAsync();
            if (!events.Success || events.Data == null)
            {
                return new Result(false, events.Message, new List<EventCardVm>());
            }

            var categories = await api.GetCategoriesAsync();
            var counts = (categories.Data ?? new()).GroupBy(c => c.EventID).ToDictionary(g => g.Key, g => g.Count());

            var cards = events.Data
                .OrderBy(e => e.EventDate)
                .Select(e => new EventCardVm { Event = e, CategoryCount = counts.GetValueOrDefault(e.EventID) })
                .ToList();

            return new Result(true, string.Empty, cards);
        }
    }
}
