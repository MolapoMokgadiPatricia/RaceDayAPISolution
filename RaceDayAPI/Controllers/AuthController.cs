using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Attributes;
using RaceDayAPI.Data;
using RaceDayAPI.Extensions;
using RaceDayAPI.Models;
using RaceDayAPI.Services;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly PasswordService _passwordService;

        public AuthController(RaceDayContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "First name, last name, email and password are required."
                });
            }

            if (request.FirstName.Trim().Length > 50 ||
                request.LastName.Trim().Length > 50 ||
                request.Email.Trim().Length > 100 ||
                request.Password.Length < 8)
            {
                return BadRequest(new
                {
                    message = "Names must not exceed 50 characters, email must not exceed 100 characters, and password must contain at least 8 characters."
                });
            }

            string email = request.Email.Trim().ToLowerInvariant();

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email);

            if (emailExists)
            {
                return Conflict(new
                {
                    message = "An account with this email already exists."
                });
            }

            string role = request.Role?.Trim() ?? "Participant";

            if (role != "Participant" && role != "Organiser")
            {
                return BadRequest(new
                {
                    message = "Role must be Participant or Organiser."
                });
            }

            if (request.PhoneNumber?.Length > 20)
            {
                return BadRequest(new
                {
                    message = "Phone number must not exceed 20 characters."
                });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new User
                {
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    Email = email,
                    PasswordHash = _passwordService.HashPassword(request.Password),
                    Role = role,
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                if (role == "Organiser")
                {
                    _context.Organisers.Add(new Organiser
                    {
                        OrganiserID = user.UserID,
                        OrganisationName = $"{user.FirstName} {user.LastName}"
                    });
                }
                else
                {
                    _context.Participants.Add(new Participant
                    {
                        ParticipantID = user.UserID
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Registration successful.",
                    user.UserID,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Role
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Registration failed. Please try again." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "Email and password are required."
                });
            }

            string email = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null ||
                !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (user.Role == "Organiser")
            {
                bool profileExists = await _context.Organisers
                    .AnyAsync(o => o.OrganiserID == user.UserID);
                if (!profileExists)
                {
                    return StatusCode(500, new { message = "Organiser profile is missing." });
                }
            }
            else if (user.Role == "Participant")
            {
                bool profileExists = await _context.Participants
                    .AnyAsync(p => p.ParticipantID == user.UserID);
                if (!profileExists)
                {
                    return StatusCode(500, new { message = "Participant profile is missing." });
                }
            }
            else
            {
                return Unauthorized(new { message = "Invalid user role." });
            }

            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("Role", user.Role);

            return Ok(new
            {
                message = "Login successful.",
                user.UserID,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Logout successful." });
        }

        [HttpGet("me")]
        [AuthorizeRole]
        public async Task<IActionResult> GetCurrentUser()
        {
            int userId = HttpContext.GetUserId();

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserID == userId)
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
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(user);
        }
    }

    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Participant";
        public string? PhoneNumber { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}