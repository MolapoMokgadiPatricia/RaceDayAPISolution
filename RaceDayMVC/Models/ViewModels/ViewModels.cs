using System.ComponentModel.DataAnnotations;
using RaceDayMVC.Models.Api;

namespace RaceDayMVC.Models.ViewModels
{
    public class EventCardVm
    {
        public ApiEvent Event { get; set; } = new();
        public int CategoryCount { get; set; }
        public bool IsPast => Event.EventDate.Date < DateTime.Today;
    }

    public class EventListVm
    {
        public List<EventCardVm> Events { get; set; } = new();
        public int UpcomingCount => Events.Count(e => !e.IsPast);
    }

    public class EventDetailsVm
    {
        public ApiEvent Event { get; set; } = new();
        public List<ApiCategory> Categories { get; set; } = new();
        public ApiEnrolment? MyEnrolment { get; set; }
        public ApiCategory? MyCategory { get; set; }
        public bool IsOwner { get; set; }
        public bool IsPast => Event.EventDate.Date < DateTime.Today;
    }

    public class LoginVm
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password.")]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public class RegisterVm
    {
        [Required(ErrorMessage = "Enter your first name.")]
        [StringLength(50, ErrorMessage = "First name can be up to 50 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your last name.")]
        [StringLength(50, ErrorMessage = "Last name can be up to 50 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(100, ErrorMessage = "Email can be up to 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Phone number can be up to 20 characters.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Choose a password.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm your password.")]
        [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose how you will use RaceDay.")]
        public string Role { get; set; } = "Participant";
    }

    public class ProfileVm
    {
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime MemberSince { get; set; }
        public string? ProfilePictureUrl { get; set; }

        [Required(ErrorMessage = "Enter your first name.")]
        [StringLength(50, ErrorMessage = "First name can be up to 50 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your last name.")]
        [StringLength(50, ErrorMessage = "Last name can be up to 50 characters.")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Phone number can be up to 20 characters.")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(100, ErrorMessage = "Contact name can be up to 100 characters.")]
        public string? EmergencyContactName { get; set; }

        [StringLength(20, ErrorMessage = "Contact phone can be up to 20 characters.")]
        public string? EmergencyContactPhone { get; set; }
    }

    public class MyEnrolmentRow
    {
        public ApiEnrolment Enrolment { get; set; } = new();
        public ApiEvent? Event { get; set; }
        public ApiCategory? Category { get; set; }
        public bool HasResult { get; set; }
    }

    public class MyResultsVm
    {
        public List<ApiHistoryRow> Rows { get; set; } = new();
        public int? BestPosition => Rows.Where(r => r.FinishPosition.HasValue).Select(r => r.FinishPosition).Min();
        public TimeSpan? FastestTime => Rows.Where(r => r.FinishTime.HasValue).Select(r => r.FinishTime).Min();
    }

    public class DashboardEventRow
    {
        public ApiEvent Event { get; set; } = new();
        public int Enrolments { get; set; }
        public int Pending { get; set; }
        public int Categories { get; set; }
        public bool IsPast => Event.EventDate.Date < DateTime.Today;
    }

    public class DashboardVm
    {
        public string Name { get; set; } = string.Empty;
        public List<DashboardEventRow> Rows { get; set; } = new();
        public int TotalEnrolments => Rows.Sum(r => r.Enrolments);
        public int PendingEnrolments => Rows.Sum(r => r.Pending);
        public int UpcomingEvents => Rows.Count(r => !r.IsPast);
        public ApiEvent? NextEvent => Rows.Where(r => !r.IsPast).OrderBy(r => r.Event.EventDate).Select(r => r.Event).FirstOrDefault();
    }

    public class EventFormVm
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Enter the event name.")]
        [StringLength(100, ErrorMessage = "Name can be up to 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description can be up to 500 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Choose the event date.")]
        [DataType(DataType.Date)]
        public DateTime? EventDate { get; set; }

        [Required(ErrorMessage = "Enter the location.")]
        [StringLength(100, ErrorMessage = "Location can be up to 100 characters.")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the distance in km.")]
        [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
        public decimal? DistanceKm { get; set; }

        [Required(ErrorMessage = "Choose an event type.")]
        public string EventType { get; set; } = "run";

        public string? BannerImageUrl { get; set; }

        public IFormFile? Banner { get; set; }
    }

    public class CategoryFormVm
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }

        [Required(ErrorMessage = "Enter a category name.")]
        [StringLength(100, ErrorMessage = "Name can be up to 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the minimum age.")]
        [Range(0, 150, ErrorMessage = "Minimum age must be between 0 and 150.")]
        public int? MinAge { get; set; }

        [Required(ErrorMessage = "Enter the maximum age.")]
        [Range(0, 150, ErrorMessage = "Maximum age must be between 0 and 150.")]
        public int? MaxAge { get; set; }

        [Required(ErrorMessage = "Enter the distance in km.")]
        [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
        public decimal? DistanceKm { get; set; }
    }

    public class CategoriesVm
    {
        public List<ApiEvent> Events { get; set; } = new();
        public ApiEvent? Selected { get; set; }
        public List<ApiCategory> Categories { get; set; } = new();
        public CategoryFormVm Form { get; set; } = new();
    }

    public class EnrolmentRow
    {
        public ApiEnrolment Enrolment { get; set; } = new();
        public string ParticipantName { get; set; } = "Unknown";
        public string Email { get; set; } = string.Empty;
        public string CategoryName { get; set; } = "-";
    }

    public class EnrolmentsVm
    {
        public List<ApiEvent> Events { get; set; } = new();
        public ApiEvent? Selected { get; set; }
        public List<EnrolmentRow> Rows { get; set; } = new();
    }

    public class ResultRowVm
    {
        public int EnrolmentId { get; set; }
        public int? ResultId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? TimeText { get; set; }
        public int? Position { get; set; }
    }

    public class ResultsVm
    {
        public List<ApiEvent> Events { get; set; } = new();
        public ApiEvent? Selected { get; set; }
        public List<ResultRowVm> Rows { get; set; } = new();
        public bool NotYetHeld => Selected != null && Selected.EventDate.Date > DateTime.Today;
    }

    public class ErrorVm
    {
        public int Code { get; set; }
        public string Title { get; set; } = "Something went wrong";
        public string Message { get; set; } = "Please try again in a moment.";
    }
}
