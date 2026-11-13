using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.Services;
using RaceDayAPI.Tests.Helpers;

namespace RaceDayAPI.Tests
{
    public class AuthControllerTests
    {
        [Fact]
        public async Task Register_ValidParticipant_ReturnsOk()
        {
            var context = TestHelpers.CreateInMemoryContext();
            var controller = new AuthController(context, new PasswordService());

            var result = await controller.Register(new RegisterRequest
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Password = "Password123!",
                Role = "Participant"
            });

            Assert.IsType<OkObjectResult>(result);
            Assert.Single(context.Users);
            Assert.Single(context.Participants);
        }

        [Fact]
        public async Task Register_ValidOrganiser_ReturnsOk()
        {
            var context = TestHelpers.CreateInMemoryContext();
            var controller = new AuthController(context, new PasswordService());

            var result = await controller.Register(new RegisterRequest
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane@example.com",
                Password = "Password123!",
                Role = "Organiser"
            });

            Assert.IsType<OkObjectResult>(result);
            Assert.Single(context.Organisers);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsConflict()
        {
            var context = TestHelpers.CreateInMemoryContext();
            var controller = new AuthController(context, new PasswordService());

            var request = new RegisterRequest
            {
                FirstName = "A",
                LastName = "B",
                Email = "dup@example.com",
                Password = "Password123!",
                Role = "Participant"
            };

            await controller.Register(request);
            var result = await controller.Register(request);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Theory]
        [InlineData("", "Doe", "e@e.com", "Password123!")]
        [InlineData("John", "", "e@e.com", "Password123!")]
        [InlineData("John", "Doe", "", "Password123!")]
        [InlineData("John", "Doe", "e@e.com", "short")]
        public async Task Register_MissingOrInvalidFields_ReturnsBadRequest(
            string first, string last, string email, string pwd)
        {
            var context = TestHelpers.CreateInMemoryContext();
            var controller = new AuthController(context, new PasswordService());

            var result = await controller.Register(new RegisterRequest
            {
                FirstName = first,
                LastName = last,
                Email = email,
                Password = pwd,
                Role = "Participant"
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_InvalidRole_ReturnsBadRequest()
        {
            var context = TestHelpers.CreateInMemoryContext();
            var controller = new AuthController(context, new PasswordService());

            var result = await controller.Register(new RegisterRequest
            {
                FirstName = "A",
                LastName = "B",
                Email = "a@b.com",
                Password = "Password123!",
                Role = "Admin"
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_CorrectCredentials_ReturnsOkAndSetsSession()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new AuthController(context, new PasswordService());
            TestHelpers.SetSession(controller, 0, "");

            var result = await controller.Login(new LoginRequest
            {
                Email = "org@raceday.co.za",
                Password = "Password123!"
            });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, controller.HttpContext.Session.GetInt32("UserID"));
            Assert.Equal("Organiser", controller.HttpContext.Session.GetString("Role"));
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsUnauthorized()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new AuthController(context, new PasswordService());
            TestHelpers.SetSession(controller, 0, "");

            var result = await controller.Login(new LoginRequest
            {
                Email = "org@raceday.co.za",
                Password = "WrongPassword!"
            });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_UnknownEmail_ReturnsUnauthorized()
        {
            var context = TestHelpers.CreateInMemoryContext();
            var controller = new AuthController(context, new PasswordService());
            TestHelpers.SetSession(controller, 0, "");

            var result = await controller.Login(new LoginRequest
            {
                Email = "nobody@example.com",
                Password = "Password123!"
            });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}