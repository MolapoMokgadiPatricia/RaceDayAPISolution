using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDayAPI.Models
{
    [Table("Category", Schema = "dbo")]
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0, 150)]
        public int MinAge { get; set; }

        [Required]
        [Range(0, 150)]
        public int MaxAge { get; set; }

        [Required]
        [Range(0.01, 999.99)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DistanceKm { get; set; }

        [JsonIgnore]
        public Event? Event { get; set; }

        [JsonIgnore]
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}