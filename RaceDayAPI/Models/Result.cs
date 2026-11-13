using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDayAPI.Models
{
    [Table("Result", Schema = "dbo")]
    public class Result
    {
        [Key]
        public int ResultID { get; set; }

        [Required]
        public int EnrolmentID { get; set; }

        public TimeSpan? FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int? FinishPosition { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public Enrolment? Enrolment { get; set; }
    }
}