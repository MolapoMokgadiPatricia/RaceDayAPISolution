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
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.EventID)
                .ThenBy(c => c.MinAge)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            return Ok(category);
        }

        [HttpGet("event/{eventId:int}")]
        public async Task<IActionResult> GetCategoriesByEvent(int eventId)
        {
            bool eventExists = await _context.Events
                .AnyAsync(e => e.EventID == eventId);

            if (!eventExists)
            {
                return NotFound(new { message = "Event not found." });
            }

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.EventID == eventId)
                .OrderBy(c => c.MinAge)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            int userId = HttpContext.GetUserId();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (category.MinAge < 0 ||
                category.MaxAge < category.MinAge ||
                category.DistanceKm <= 0)
            {
                return BadRequest(new
                {
                    message = "Enter valid age limits and a distance greater than zero."
                });
            }

            var raceEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == category.EventID);

            if (raceEvent == null)
            {
                return BadRequest(new { message = "The specified event does not exist." });
            }

            if (raceEvent.OrganiserID != userId)
            {
                return StatusCode(403, new
                {
                    message = "You can only add categories to your own events."
                });
            }

            category.CategoryID = 0;
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryID }, category);
        }

        [HttpPut("{id:int}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category updatedCategory)
        {
            int userId = HttpContext.GetUserId();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (updatedCategory.MinAge < 0 ||
                updatedCategory.MaxAge < updatedCategory.MinAge ||
                updatedCategory.DistanceKm <= 0)
            {
                return BadRequest(new
                {
                    message = "Enter valid age limits and a distance greater than zero."
                });
            }

            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (existingCategory == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == existingCategory.EventID);

            if (existingEvent == null || existingEvent.OrganiserID != userId)
            {
                return StatusCode(403, new
                {
                    message = "You can only update categories belonging to your own events."
                });
            }

            if (updatedCategory.EventID != existingCategory.EventID)
            {
                return BadRequest(new
                {
                    message = "A category cannot be moved to another event."
                });
            }

            existingCategory.Name = updatedCategory.Name;
            existingCategory.MinAge = updatedCategory.MinAge;
            existingCategory.MaxAge = updatedCategory.MaxAge;
            existingCategory.DistanceKm = updatedCategory.DistanceKm;

            await _context.SaveChangesAsync();

            return Ok(existingCategory);
        }

        [HttpDelete("{id:int}")]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            int userId = HttpContext.GetUserId();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            var raceEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == category.EventID);

            if (raceEvent == null || raceEvent.OrganiserID != userId)
            {
                return StatusCode(403, new
                {
                    message = "You can only delete categories belonging to your own events."
                });
            }

            bool hasEnrolments = await _context.Enrolments
                .AnyAsync(e => e.CategoryID == id);

            if (hasEnrolments)
            {
                return Conflict(new
                {
                    message = "This category cannot be deleted because enrolments are linked to it."
                });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category deleted successfully." });
        }
    }
}