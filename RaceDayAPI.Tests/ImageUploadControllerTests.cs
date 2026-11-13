using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.Services;
using RaceDayAPI.Tests.Helpers;

namespace RaceDayAPI.Tests
{
    public class ImageUploadControllerTests
    {
        private class FakeBlobStorage : IBlobStorageService
        {
            public Task<bool> TestConnectionAsync() => Task.FromResult(true);

            public Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType)
                => Task.FromResult($"https://storage.test/event-banners/{fileName}");

            public Task<string> UploadProfilePictureAsync(Stream imageStream, string fileName, string contentType)
                => Task.FromResult($"https://storage.test/profile-pictures/{fileName}");
        }

        private static IFormFile PngFile(string name = "banner.png")
        {
            byte[] bytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00 };

            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
        }

        private static IFormFile FakePng()
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes("this is plain text, not an image");

            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "evil.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
        }

        private static IFormFile GifFile()
        {
            byte[] bytes = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "anim.gif")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/gif"
            };
        }

        // ---------------------------------------------------------- Event banners

        [Fact]
        public async Task UploadBanner_OwnEvent_SavesUrlOnEvent()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EventBannersController(context, new FakeBlobStorage());
            TestHelpers.SetSession(controller, 1, "Organiser");

            var result = await controller.UploadBanner(1, PngFile());

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("https://storage.test/event-banners/banner.png", context.Events.Find(1)!.BannerImageUrl);
        }

        [Fact]
        public async Task UploadBanner_AnotherOrganisersEvent_ReturnsForbidden()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EventBannersController(context, new FakeBlobStorage());
            TestHelpers.SetSession(controller, 99, "Organiser");

            var result = await controller.UploadBanner(1, PngFile());

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
        }

        [Fact]
        public async Task UploadBanner_UnknownEvent_ReturnsNotFound()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EventBannersController(context, new FakeBlobStorage());
            TestHelpers.SetSession(controller, 1, "Organiser");

            var result = await controller.UploadBanner(404, PngFile());

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UploadBanner_FileIsNotReallyAnImage_ReturnsBadRequest()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EventBannersController(context, new FakeBlobStorage());
            TestHelpers.SetSession(controller, 1, "Organiser");

            var result = await controller.UploadBanner(1, FakePng());

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Null(context.Events.Find(1)!.BannerImageUrl);
        }

        [Fact]
        public async Task UploadBanner_UnsupportedImageType_ReturnsBadRequest()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new EventBannersController(context, new FakeBlobStorage());
            TestHelpers.SetSession(controller, 1, "Organiser");

            var result = await controller.UploadBanner(1, GifFile());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // -------------------------------------------------------- Profile pictures

        [Fact]
        public async Task UploadProfilePicture_AsParticipant_SavesUrl()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new ProfilePicturesController(context, new FakeBlobStorage());
            TestHelpers.SetSession(controller, 2, "Participant");

            var result = await controller.UploadProfilePicture(PngFile("me.png"));

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("https://storage.test/profile-pictures/me.png", context.Participants.Find(2)!.ProfilePictureUrl);
        }

        [Fact]
        public async Task UploadProfilePicture_NoFile_ReturnsBadRequest()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new ProfilePicturesController(context, new FakeBlobStorage());
            TestHelpers.SetSession(controller, 2, "Participant");

            var result = await controller.UploadProfilePicture(null!);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadProfilePicture_ParticipantRecordMissing_ReturnsNotFound()
        {
            var context = TestHelpers.CreateInMemoryContext();
            TestHelpers.SeedBasicData(context);
            var controller = new ProfilePicturesController(context, new FakeBlobStorage());
            TestHelpers.SetSession(controller, 77, "Participant");

            var result = await controller.UploadProfilePicture(PngFile());

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ------------------------------------------------------------ Validator

        [Fact]
        public async Task ImageValidator_ValidPng_ReturnsNull()
        {
            Assert.Null(await ImageValidator.ValidateAsync(PngFile()));
        }

        [Fact]
        public async Task ImageValidator_TooLarge_ReturnsMessage()
        {
            var big = new FormFile(new MemoryStream(new byte[16]), 0, ImageValidator.MaxFileSize + 1, "file", "big.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            Assert.Contains("5 MB", await ImageValidator.ValidateAsync(big));
        }
    }
}
