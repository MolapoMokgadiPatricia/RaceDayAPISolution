using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.Models;
using RaceDayAPI.Tests.Helpers;

namespace RaceDayAPI.Tests
{
    public class EnrolmentsControllerTests
    {
        [Fact]
        public async Task CreateEnrolment_AsParticipant_Succeeds()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EnrolmentsController(context);
            TestHelpers.SetSession(controller, 2, "Participant");

            var result = await controller.CreateEnrolment(new Enrolment
            {
                EventID = 1,
                CategoryID = 1
            });

            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Single(context.Enrolments);
        }

        [Fact]
        public async Task CreateEnrolment_DuplicateEnrolment_ReturnsConflict()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);

            context.Enrolments.Add(new Enrolment
            {
                EnrolmentID = 1,
                ParticipantID = 2,
                EventID = 1,
                CategoryID = 1,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Pending"
            });
            context.SaveChanges();

            var controller = new EnrolmentsController(context);
            TestHelpers.SetSession(controller, 2, "Participant");

            var result = await controller.CreateEnrolment(new Enrolment
            {
                EventID = 1,
                CategoryID = 1
            });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task CreateEnrolment_InvalidCategoryForEvent_ReturnsBadRequest()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EnrolmentsController(context);
            TestHelpers.SetSession(controller, 2, "Participant");

            var result = await controller.CreateEnrolment(new Enrolment
            {
                EventID = 1,
                CategoryID = 999
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetParticipantEnrolments_OwnData_ReturnsOk()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);

            context.Enrolments.Add(new Enrolment
            {
                EnrolmentID = 1,
                ParticipantID = 2,
                EventID = 1,
                CategoryID = 1,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Pending"
            });
            context.SaveChanges();

            var controller = new EnrolmentsController(context);
            TestHelpers.SetSession(controller, 2, "Participant");

            var result = await controller.GetParticipantEnrolments(2);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetParticipantEnrolments_OtherParticipant_Forbidden()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EnrolmentsController(context);
            TestHelpers.SetSession(controller, 2, "Participant");

            var result = await controller.GetParticipantEnrolments(99);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, obj.StatusCode);
        }

        [Fact]
        public async Task GetEnrolment_NotFound_Returns404()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EnrolmentsController(context);
            TestHelpers.SetSession(controller, 2, "Participant");

            var result = await controller.GetEnrolment(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}