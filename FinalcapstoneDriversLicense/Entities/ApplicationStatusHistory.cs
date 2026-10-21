using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalcapstoneDriversLicense.Entities
{
    public class ApplicationStatusHistory
    {
        internal DateTime LastUpdated;
        internal string? RejectionReason;
        internal DateTime CollectionDate;

        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public LicenseApplication? Application { get; set; }

        [Required, MaxLength(40)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Comment { get; set; }

        public DateTime DateChanged { get; set; } = DateTime.UtcNow;
        public Guid ChangedByUserId { get; set; }
        [NotMapped]
        public DateTime ChangedAt { get; internal set; }
    }
}
