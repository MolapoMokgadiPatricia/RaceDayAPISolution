using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.Models;
using RaceDayAPI.Tests.Helpers;

namespace RaceDayAPI.Tests
{
    public class EventsControllerTests
    {
        [Fact]
        public async Task GetEvents_ReturnsAllEvents()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EventsController(context);

            var result = await controller.GetEvents();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetEvent_ExistingEvent_ReturnsOk()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EventsController(context);

            var result = await controller.GetEvent(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetEvent_NotFound_Returns404()
        {
            var context = TestHelpers.CreateInMemoryContext();
            var controller = new EventsController(context);

            var result = await controller.GetEvent(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateEvent_AsOrganiser_Succeeds()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EventsController(context);
            TestHelpers.SetSession(controller, 1, "Organiser");

            var result = await controller.CreateEvent(new Event
            {
                Name = "Cape Town Cycle Tour",
                Description = "Annual cycle tour",
                EventDate = DateTime.UtcNow.AddMonths(3),
                Location = "Cape Town",
                DistanceKm = 109m,
                EventType = "cycle"
            });

            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(2, context.Events.Count());
        }

        [Fact]
        public async Task CreateEvent_InvalidModel_ReturnsBadRequest()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EventsController(context);
            TestHelpers.SetSession(controller, 1, "Organiser");
            controller.ModelState.AddModelError("Name", "Required");

            var result = await controller.CreateEvent(new Event
            {
                EventDate = DateTime.UtcNow.AddDays(1),
                Location = "X",
                DistanceKm = 5m,
                EventType = "run"
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateEvent_NotOwner_Forbidden()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);

            context.Users.Add(new User
            {
                UserID = 3,
                FirstName = "Other",
                LastName = "Org",
                Email = "other@org.com",
                PasswordHash = "x",
                Role = "Organiser"
            });
            context.Organisers.Add(new Organiser
            {
                OrganiserID = 3,
                OrganisationName = "Other Org"
            });
            context.SaveChanges();

            var controller = new EventsController(context);
            TestHelpers.SetSession(controller, 3, "Organiser");

            var result = await controller.UpdateEvent(1, new Event
            {
                Name = "Hacked",
                EventDate = DateTime.UtcNow.AddDays(1),
                Location = "X",
                DistanceKm = 5m,
                EventType = "run"
            });

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_Owner_Succeeds()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EventsController(context);
            TestHelpers.SetSession(controller, 1, "Organiser");

            var result = await controller.UpdateEvent(1, new Event
            {
                Name = "Updated Comrades",
                EventDate = DateTime.UtcNow.AddMonths(3),
                Location = "Durban",
                DistanceKm = 90m,
                EventType = "run"
            });

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteEvent_NotOwner_Forbidden()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);

            context.Users.Add(new User
            {
                UserID = 3,
                FirstName = "Other",
                LastName = "Org",
                Email = "other@org.com",
                PasswordHash = "x",
                Role = "Organiser"
            });
            context.Organisers.Add(new Organiser
            {
                OrganiserID = 3,
                OrganisationName = "Other Org"
            });
            context.SaveChanges();

            var controller = new EventsController(context);
            TestHelpers.SetSession(controller, 3, "Organiser");

            var result = await controller.DeleteEvent(1);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_Owner_Succeeds()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EventsController(context);
            TestHelpers.SetSession(controller, 1, "Organiser");

            var result = await controller.DeleteEvent(1);

            Assert.IsType<OkObjectResult>(result);
            Assert.Empty(context.Events);
        }
    }
}