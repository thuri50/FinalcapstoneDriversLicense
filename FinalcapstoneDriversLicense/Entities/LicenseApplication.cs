using System.ComponentModel.DataAnnotations;

namespace FinalcapstoneDriversLicense.Entities
{
    public class LicenseApplication
    { 
        public int Id { get; set; }
        

        [Required, MaxLength(30)]
        public string ApplicationNumber { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; } 
        public ApplicationUser? User { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required, MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LicenseType { get; set; } = "Standard";

        [Required, MaxLength(40)]
        public string Status { get; set; } =ApplicationStatus.Submitted;

        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime? CollectionDate { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
    }

}

