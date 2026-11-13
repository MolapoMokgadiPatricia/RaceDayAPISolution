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
    public class ProfilePicturesController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly IBlobStorageService _blobStorageService;

        public ProfilePicturesController(
            RaceDayContext context,
            IBlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        /// <summary>
        /// Uploads or replaces the logged-in Participant's profile picture.
        /// The image is stored in Azure Blob Storage and its URL is saved on the participant.
        /// </summary>
        /// <param name="file">JPEG, PNG or WebP image, up to 5 MB.</param>
        /// <returns>The public URL of the stored image.</returns>
        [HttpPost]
        [AuthorizeRole("Participant")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile file)
        {
            int userId = HttpContext.GetUserId();

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.ParticipantID == userId);

            if (participant == null)
            {
                return NotFound(new
                {
                    message = "Participant profile not found."
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

            string imageUrl = await _blobStorageService.UploadProfilePictureAsync(
                imageStream,
                file.FileName,
                file.ContentType);

            participant.ProfilePictureUrl = imageUrl;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile picture uploaded successfully.",
                profilePictureUrl = imageUrl
            });
        }
    }
}
