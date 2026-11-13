using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.Models;
using RaceDayAPI.Tests.Helpers;

namespace RaceDayAPI.Tests
{
    public class CategoriesControllerTests
    {
        [Fact]
        public async Task GetCategories_ReturnsAll()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new CategoriesController(context);

            var result = await controller.GetCategories();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetCategory_NotFound_Returns404()
        {
            var context = TestHelpers.CreateInMemoryContext();
            var controller = new CategoriesController(context);

            var result = await controller.GetCategory(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetCategoriesByEvent_ReturnsOk()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new CategoriesController(context);

            var result = await controller.GetCategoriesByEvent(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CreateCategory_AsOrganiserOwner_Succeeds()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new CategoriesController(context);
            TestHelpers.SetSession(controller, 1, "Organiser");

            var result = await controller.CreateCategory(new Category
            {
                EventID = 1,
                Name = "Junior",
                MinAge = 10,
                MaxAge = 19,
                DistanceKm = 5m
            });

            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(2, context.Categories.Count());
        }

        [Fact]
        public async Task CreateCategory_NotOwner_Forbidden()
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

            var controller = new CategoriesController(context);
            TestHelpers.SetSession(controller, 3, "Organiser");

            var result = await controller.CreateCategory(new Category
            {
                EventID = 1,
                Name = "Junior",
                MinAge = 10,
                MaxAge = 19,
                DistanceKm = 5m
            });

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_InvalidAgeRange_ReturnsBadRequest()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new CategoriesController(context);
            TestHelpers.SetSession(controller, 1, "Organiser");

            var result = await controller.CreateCategory(new Category
            {
                EventID = 1,
                Name = "Bad",
                MinAge = 50,
                MaxAge = 20,
                DistanceKm = 5m
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteCategory_NotOwner_Forbidden()
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

            var controller = new CategoriesController(context);
            TestHelpers.SetSession(controller, 3, "Organiser");

            var result = await controller.DeleteCategory(1);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task DeleteCategory_Owner_Succeeds()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new CategoriesController(context);
            TestHelpers.SetSession(controller, 1, "Organiser");

            var result = await controller.DeleteCategory(1);

            Assert.IsType<OkObjectResult>(result);
            Assert.Empty(context.Categories);
        }
    }
}