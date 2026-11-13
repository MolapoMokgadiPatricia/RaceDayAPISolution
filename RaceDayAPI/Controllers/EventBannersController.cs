using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Attributes;
using RaceDayAPI.Data;
using RaceDayAPI.Extensions;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EventBannersController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IBlobStorageService _blobStorageService;

        public EventBannersController(
            RaceDayContext context,
            IBlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        [HttpPost("{id:int}/banner")]
        [AuthorizeRole("Organiser")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadBanner(
            int id,
            IFormFile file)
        {
            int userId = HttpContext.GetUserId();

            var raceEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (raceEvent == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            if (raceEvent.OrganiserID != userId)
            {
                return StatusCode(403, new
                {
                    message = "You can only upload banners for your own events."
                });
            }

            string? error = await ImageValidator.ValidateAsync(file);

            if (error != null)
            {
                return BadRequest(new
                {
                    message = error
                });
            }

            await using var imageStream = file.OpenReadStream();

            string imageUrl = await _blobStorageService.UploadImageAsync(
                imageStream,
                file.FileName,
                file.ContentType);

            raceEvent.BannerImageUrl = imageUrl;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Event banner uploaded successfully.",
                eventId = raceEvent.EventID,
                bannerImageUrl = raceEvent.BannerImageUrl
            });
        }

        [HttpGet("storage-test")]
        [AuthorizeRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> TestStorage()
        {
            bool connected =
                await _blobStorageService.TestConnectionAsync();

            if (!connected)
            {
                return StatusCode(503, new
                {
                    message = "Could not connect to Azure Blob Storage."
                });
            }

            return Ok(new
            {
                message = "Successfully connected to Azurite Blob Storage.",
                container = "event-banners"
            });
        }
    }
}