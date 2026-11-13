using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Attributes;
using RaceDayAPI.Data;
using RaceDayAPI.Extensions;

namespace RaceDayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public UsersController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>Lists every user. Organiser only (used to show participant names on enrolments).</summary>
        [HttpGet]
        [AuthorizeRole("Organiser")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new
                {
                    u.UserID,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Role,
                    u.PhoneNumber,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>Gets the logged-in user's own profile, including Participant details and picture.</summary>
        [HttpGet("{id:int}")]
        [AuthorizeRole]
        public async Task<IActionResult> GetUser(int id)
        {
            int userId = HttpContext.GetUserId();

            if (HttpContext.IsParticipant() && id != userId)
            {
                return StatusCode(403, new { message = "You can only view your own profile." });
            }

            var profile = await BuildProfileAsync(id);

            if (profile == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(profile);
        }

        /// <summary>Updates the logged-in user's own profile.</summary>
        [HttpPut("{id:int}")]
        [AuthorizeRole]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            int userId = HttpContext.GetUserId();

            if (id != userId)
            {
                return StatusCode(403, new { message = "You can only update your own profile." });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                if (request.FirstName.Trim().Length > 50)
                {
                    return BadRequest(new
                    {
                        message = "First name must not exceed 50 characters."
                    });
                }

                user.FirstName = request.FirstName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                if (request.LastName.Trim().Length > 50)
                {
                    return BadRequest(new
                    {
                        message = "Last name must not exceed 50 characters."
                    });
                }

                user.LastName = request.LastName.Trim();
            }

            if (request.PhoneNumber != null)
            {
                if (request.PhoneNumber.Length > 20)
                {
                    return BadRequest(new
                    {
                        message = "Phone number must not exceed 20 characters."
                    });
                }

                user.PhoneNumber = request.PhoneNumber.Trim();
            }

            if (user.Role == "Participant")
            {
                var participant = await _context.Participants
                    .FirstOrDefaultAsync(p => p.ParticipantID == id);

                if (participant != null)
                {
                    if (request.DateOfBirth.HasValue)
                    {
                        if (request.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                        {
                            return BadRequest(new { message = "Date of birth cannot be in the future." });
                        }

                        participant.DateOfBirth = request.DateOfBirth.Value.Date;
                    }

                    if (request.EmergencyContactName != null)
                    {
                        if (request.EmergencyContactName.Length > 100)
                        {
                            return BadRequest(new
                            {
                                message = "Emergency contact name must not exceed 100 characters."
                            });
                        }

                        participant.EmergencyContactName = request.EmergencyContactName.Trim();
                    }

                    if (request.EmergencyContactPhone != null)
                    {
                        if (request.EmergencyContactPhone.Length > 20)
                        {
                            return BadRequest(new
                            {
                                message = "Emergency contact phone must not exceed 20 characters."
                            });
                        }

                        participant.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(await BuildProfileAsync(id));
        }

        private async Task<object?> BuildProfileAsync(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
            {
                return null;
            }

            var participant = await _context.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParticipantID == id);

            return new
            {
                user.UserID,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.PhoneNumber,
                user.CreatedAt,
                DateOfBirth = participant?.DateOfBirth,
                EmergencyContactName = participant?.EmergencyContactName,
                EmergencyContactPhone = participant?.EmergencyContactPhone,
                ProfilePictureUrl = participant?.ProfilePictureUrl
            };
        }
    }

    public class UpdateUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }
}
