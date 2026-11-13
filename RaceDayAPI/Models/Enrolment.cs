using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDayAPI.Models
{
    [Table("Enrolment", Schema = "dbo")]
    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        [Required]
        public int ParticipantID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        [StringLength(20)]
        public string? Status { get; set; } = "Pending";

        [JsonIgnore]
        public Participant? Participant { get; set; }

        [JsonIgnore]
        public Event? Event { get; set; }

        [JsonIgnore]
        public Category? Category { get; set; }

        [JsonIgnore]
        public Result? Result { get; set; }
    }
}