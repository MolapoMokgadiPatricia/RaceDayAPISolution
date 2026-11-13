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
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AuthorizeRole]
        public async Task<IActionResult> GetResults()
        {
            int userId = HttpContext.GetUserId();

            if (HttpContext.IsOrganiser())
            {
                var organiserResults = await _context.Results
                    .AsNoTracking()
                    .Where(r => _context.Enrolments
                        .Any(e => e.EnrolmentID == r.EnrolmentID &&
                                  _context.Events.Any(ev => ev.EventID == e.EventID &&
                                                            ev.OrganiserID == userId)))
                    .OrderBy(r => r.ResultID)
                    .ToListAsync();

                return Ok(organiserResults);
            }

            var participantResults = await _context.Results
                .AsNoTracking()
                .Where(r => _context.Enrolments
                    .Any(e => e.EnrolmentID == r.EnrolmentID &&
                              e.ParticipantID == userId))
                .OrderBy(r => r.ResultID)
                .ToListAsync();

            return Ok(participantResults);
        }

        /// <summary>
        /// Race history for the logged-in Participant: event, date, category, finish time,
        /// finishing position and the number of finishers in that event.
        /// </summary>
        [HttpGet("my-history")]
        [AuthorizeRole("Participant")]
        public async Task<IActionResult> GetMyHistory()
        {
            int userId = HttpContext.GetUserId();

            var history = await _context.Results
                .AsNoTracking()
                .Where(r => r.Enrolment!.ParticipantID == userId)
                .Select(r => new
                {
                    r.ResultID,
                    r.EnrolmentID,
                    EventID = r.Enrolment!.EventID,
                    EventName = r.Enrolment.Event!.Name,
                    EventDate = r.Enrolment.Event.EventDate,
                    Location = r.Enrolment.Event.Location,
                    EventType = r.Enrolment.Event.EventType,
                    CategoryName = r.Enrolment.Category!.Name,
                    r.FinishTime,
                    r.FinishPosition,
                    TotalFinishers = _context.Results.Count(x =>
                        x.Enrolment!.EventID == r.Enrolment.EventID && x.FinishPosition != null)
                })
                .OrderByDescending(x => x.EventDate)
                .ToListAsync();

            return Ok(history);
        }

        [HttpGet("{id:int}")]
        [AuthorizeRole]
        public async Task<IActionResult> GetResult(int id)
        {
            int userId = HttpContext.GetUserId();

            var result = await _context.Results
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
            {
                return NotFound(new { message = "Result not found." });
            }

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentID == result.EnrolmentID);

            if (enrolment == null)
            {
                return NotFound(new { message = "Associated enrolment not found." });
            }

            if (HttpContext.IsParticipant() && enrolment.ParticipantID != userId)
            {
                return StatusCode(403, new { message = "You can only view your own results." });
            }

            if (HttpContext.IsOrganiser())
            {
                bool ownsEvent = await _context.Events
                    .AnyAsync(ev => ev.EventID == enrolment.EventID && ev.OrganiserID == userId);
                if (!ownsEvent)
                {
                    return StatusCode(403, new
                    {
                        message = "You can only view results for your own events."
                    });
                }
            }

            return Ok(result);
        }

        [HttpGet("enrolment/{enrolmentId:int}")]
        [AuthorizeRole]
        public async Task<IActionResult> GetResultByEnrolment(int enrolmentId)
        {
            int userId = HttpContext.GetUserId();

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            if (HttpContext.IsParticipant() && enrolment.ParticipantID != userId)
            {
                return StatusCode(403, new { message = "You can only view your own results." });
            }

            if (HttpContext.IsOrganiser())
            {
                bool ownsEvent = await _context.Events
                    .AnyAsync(ev => ev.EventID == enrolment.EventID && ev.OrganiserID == userId);
                if (!ownsEvent)
                {
                    return StatusCode(403, new
                    {
                        message = "You can only view results for your own events."
                    });
                }
            }

            var result = await _context.Results
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.EnrolmentID == enrolmentId);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "No result has been recorded for this enrolment."
                });
            }

            return Ok(result);
        }

        [HttpPost]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> CreateResult([FromBody] Result result)
        {
            int userId = HttpContext.GetUserId();

            if (result.EnrolmentID <= 0)
            {
                return BadRequest(new { message = "A valid EnrolmentID is required." });
            }

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentID == result.EnrolmentID);

            if (enrolment == null)
            {
                return BadRequest(new { message = "The enrolment does not exist." });
            }

            bool ownsEvent = await _context.Events
                .AnyAsync(ev => ev.EventID == enrolment.EventID && ev.OrganiserID == userId);

            if (!ownsEvent)
            {
                return StatusCode(403, new
                {
                    message = "You can only capture results for your own events."
                });
            }

            var existingResult = await _context.Results
                .AnyAsync(r => r.EnrolmentID == result.EnrolmentID);

            if (existingResult)
            {
                return Conflict(new
                {
                    message = "A result already exists for this enrolment."
                });
            }

            if (result.FinishPosition.HasValue && result.FinishPosition.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "FinishPosition must be greater than zero."
                });
            }

            result.ResultID = 0;
            result.RecordedAt = DateTime.UtcNow;

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetResult), new { id = result.ResultID }, result);
        }

        [HttpPut("{id:int}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> UpdateResult(int id, [FromBody] Result updatedResult)
        {
            int userId = HttpContext.GetUserId();

            var existingResult = await _context.Results
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (existingResult == null)
            {
                return NotFound(new { message = "Result not found." });
            }

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentID == existingResult.EnrolmentID);

            if (enrolment == null)
            {
                return NotFound(new { message = "Associated enrolment not found." });
            }

            bool ownsEvent = await _context.Events
                .AnyAsync(ev => ev.EventID == enrolment.EventID && ev.OrganiserID == userId);

            if (!ownsEvent)
            {
                return StatusCode(403, new
                {
                    message = "You can only update results for your own events."
                });
            }

            if (updatedResult.FinishPosition.HasValue && updatedResult.FinishPosition.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "FinishPosition must be greater than zero."
                });
            }

            existingResult.FinishTime = updatedResult.FinishTime;
            existingResult.FinishPosition = updatedResult.FinishPosition;

            await _context.SaveChangesAsync();

            return Ok(existingResult);
        }

        [HttpDelete("{id:int}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            int userId = HttpContext.GetUserId();

            var result = await _context.Results
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
            {
                return NotFound(new { message = "Result not found." });
            }

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentID == result.EnrolmentID);

            if (enrolment == null)
            {
                return NotFound(new { message = "Associated enrolment not found." });
            }

            bool ownsEvent = await _context.Events
                .AnyAsync(ev => ev.EventID == enrolment.EventID && ev.OrganiserID == userId);

            if (!ownsEvent)
            {
                return StatusCode(403, new
                {
                    message = "You can only delete results for your own events."
                });
            }

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Result deleted successfully." });
        }
    }
}