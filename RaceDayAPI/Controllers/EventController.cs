using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Attributes;
using RaceDayAPI.Data;
using RaceDayAPI.Extensions;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EventsController(RaceDayContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .AsNoTracking()
                .Include(e => e.Categories)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return Ok(events);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var raceEvent = await _context.Events
                .AsNoTracking()
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (raceEvent == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            return Ok(raceEvent);
        }

        [HttpPost]
        [AuthorizeRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent([FromBody] Event newEvent)
        {
            int userId = HttpContext.GetUserId();

            bool organiserExists = await _context.Organisers
                .AnyAsync(o => o.OrganiserID == userId);

            if (!organiserExists)
            {
                return BadRequest(new
                {
                    message = "An organiser profile must exist before creating events."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            newEvent.EventID = 0;
            newEvent.OrganiserID = userId;
            newEvent.CreatedAt = DateTime.UtcNow;

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = newEvent.EventID }, newEvent);
        }

        [HttpPut("{id:int}")]
        [AuthorizeRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] Event updatedEvent)
        {
            int userId = HttpContext.GetUserId();

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (existingEvent == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (existingEvent.OrganiserID != userId)
            {
                return StatusCode(403, new { message = "You can only update your own events." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            existingEvent.Name = updatedEvent.Name;
            existingEvent.EventDate = updatedEvent.EventDate;
            existingEvent.Location = updatedEvent.Location;
            existingEvent.DistanceKm = updatedEvent.DistanceKm;
            existingEvent.EventType = updatedEvent.EventType;
            existingEvent.Description = updatedEvent.Description;

            // The banner is normally changed through POST /api/uploads/event-banner/{id}.
            // Only overwrite it here when the caller explicitly sends a value.
            if (!string.IsNullOrWhiteSpace(updatedEvent.BannerImageUrl))
            {
                existingEvent.BannerImageUrl = updatedEvent.BannerImageUrl;
            }

            await _context.SaveChangesAsync();

            return Ok(existingEvent);
        }

        [HttpDelete("{id:int}")]
        [AuthorizeRole("Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            int userId = HttpContext.GetUserId();

            var raceEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (raceEvent == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (raceEvent.OrganiserID != userId)
            {
                return StatusCode(403, new { message = "You can only delete your own events." });
            }

            _context.Events.Remove(raceEvent);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Event deleted successfully." });
        }
    }
}