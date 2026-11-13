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
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> GetEnrolments()
        {
            int userId = HttpContext.GetUserId();

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => _context.Events
                    .Any(ev => ev.EventID == e.EventID && ev.OrganiserID == userId))
                .OrderBy(e => e.EnrolmentID)
                .ToListAsync();

            return Ok(enrolments);
        }

        [HttpGet("{id:int}")]
        [AuthorizeRole]
        public async Task<IActionResult> GetEnrolment(int id)
        {
            int userId = HttpContext.GetUserId();

            var enrolment = await _context.Enrolments
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            if (HttpContext.IsParticipant() && enrolment.ParticipantID != userId)
            {
                return StatusCode(403, new { message = "You can only view your own enrolments." });
            }

            if (HttpContext.IsOrganiser())
            {
                bool ownsEvent = await _context.Events
                    .AnyAsync(ev => ev.EventID == enrolment.EventID && ev.OrganiserID == userId);
                if (!ownsEvent)
                {
                    return StatusCode(403, new
                    {
                        message = "You can only view enrolments for your own events."
                    });
                }
            }

            return Ok(enrolment);
        }

        [HttpGet("participant/{participantId:int}")]
        [AuthorizeRole]
        public async Task<IActionResult> GetParticipantEnrolments(int participantId)
        {
            int userId = HttpContext.GetUserId();

            if (HttpContext.IsParticipant() && participantId != userId)
            {
                return StatusCode(403, new { message = "You can only view your own enrolments." });
            }

            var participantExists = await _context.Participants
                .AnyAsync(p => p.ParticipantID == participantId);

            if (!participantExists)
            {
                return NotFound(new { message = "Participant not found." });
            }

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.ParticipantID == participantId)
                .OrderByDescending(e => e.EnrolmentDate)
                .ToListAsync();

            return Ok(enrolments);
        }

        [HttpGet("event/{eventId:int}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            int userId = HttpContext.GetUserId();

            bool ownsEvent = await _context.Events
                .AnyAsync(ev => ev.EventID == eventId && ev.OrganiserID == userId);

            if (!ownsEvent)
            {
                return StatusCode(403, new
                {
                    message = "You can only view enrolments for your own events."
                });
            }

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.EventID == eventId)
                .OrderByDescending(e => e.EnrolmentDate)
                .ToListAsync();

            return Ok(enrolments);
        }

        [HttpPost]
        [AuthorizeRole("Participant")]
        public async Task<IActionResult> CreateEnrolment([FromBody] Enrolment enrolment)
        {
            int userId = HttpContext.GetUserId();
            enrolment.ParticipantID = userId;

            if (enrolment.EventID <= 0 || enrolment.CategoryID <= 0)
            {
                return BadRequest(new
                {
                    message = "Valid EventID and CategoryID are required."
                });
            }

            var participantExists = await _context.Participants
                .AnyAsync(p => p.ParticipantID == enrolment.ParticipantID);

            if (!participantExists)
            {
                return BadRequest(new { message = "Participant does not exist." });
            }

            var eventExists = await _context.Events
                .AnyAsync(e => e.EventID == enrolment.EventID);

            if (!eventExists)
            {
                return BadRequest(new { message = "Event does not exist." });
            }

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryID == enrolment.CategoryID
                            && c.EventID == enrolment.EventID);

            if (!categoryExists)
            {
                return BadRequest(new
                {
                    message = "The category does not belong to the selected event."
                });
            }

            var alreadyEnrolled = await _context.Enrolments.AnyAsync(e =>
                e.ParticipantID == enrolment.ParticipantID &&
                e.EventID == enrolment.EventID);

            if (alreadyEnrolled)
            {
                return Conflict(new
                {
                    message = "You are already enrolled in this event."
                });
            }

            enrolment.EnrolmentDate = DateTime.UtcNow;
            enrolment.Status = "Pending";
            enrolment.EnrolmentID = 0;

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEnrolment), new { id = enrolment.EnrolmentID }, enrolment);
        }

        [HttpPut("{id:int}")]
        [AuthorizeRole]
        public async Task<IActionResult> UpdateEnrolment(int id, [FromBody] Enrolment updatedEnrolment)
        {
            int userId = HttpContext.GetUserId();

            var existingEnrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);

            if (existingEnrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            if (HttpContext.IsOrganiser())
            {
                bool ownsEvent = await _context.Events
                    .AnyAsync(ev => ev.EventID == existingEnrolment.EventID && ev.OrganiserID == userId);
                if (!ownsEvent)
                {
                    return StatusCode(403, new
                    {
                        message = "You can only update enrolments for your own events."
                    });
                }

                if (!string.IsNullOrWhiteSpace(updatedEnrolment.Status))
                {
                    var allowedStatuses = new[] { "Pending", "Approved", "Rejected", "Cancelled" };
                    if (!allowedStatuses.Contains(updatedEnrolment.Status))
                    {
                        return BadRequest(new
                        {
                            message = "Status must be Pending, Approved, Rejected or Cancelled."
                        });
                    }

                    existingEnrolment.Status = updatedEnrolment.Status;
                }

                await _context.SaveChangesAsync();
                return Ok(existingEnrolment);
            }

            if (HttpContext.IsParticipant())
            {
                if (existingEnrolment.ParticipantID != userId)
                {
                    return StatusCode(403, new
                    {
                        message = "You can only update your own enrolments."
                    });
                }

                if (updatedEnrolment.CategoryID > 0)
                {
                    var categoryExists = await _context.Categories
                        .AnyAsync(c => c.CategoryID == updatedEnrolment.CategoryID
                                    && c.EventID == existingEnrolment.EventID);

                    if (!categoryExists)
                    {
                        return BadRequest(new
                        {
                            message = "The category does not belong to the selected event."
                        });
                    }

                    existingEnrolment.CategoryID = updatedEnrolment.CategoryID;
                }

                await _context.SaveChangesAsync();
                return Ok(existingEnrolment);
            }

            return StatusCode(403, new { message = "You are not allowed to update this enrolment." });
        }

        [HttpDelete("{id:int}")]
        [AuthorizeRole]
        public async Task<IActionResult> DeleteEnrolment(int id)
        {
            int userId = HttpContext.GetUserId();

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            if (HttpContext.IsParticipant() && enrolment.ParticipantID != userId)
            {
                return StatusCode(403, new { message = "You can only delete your own enrolments." });
            }

            if (HttpContext.IsOrganiser())
            {
                bool ownsEvent = await _context.Events
                    .AnyAsync(ev => ev.EventID == enrolment.EventID && ev.OrganiserID == userId);
                if (!ownsEvent)
                {
                    return StatusCode(403, new
                    {
                        message = "You can only delete enrolments for your own events."
                    });
                }
            }

            var hasResult = await _context.Results.AnyAsync(r => r.EnrolmentID == id);
            if (hasResult)
            {
                return Conflict(new
                {
                    message = "This enrolment has a result and cannot be deleted."
                });
            }

            _context.Enrolments.Remove(enrolment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Enrolment deleted successfully." });
        }
    }
}