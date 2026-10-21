using System.ComponentModel.DataAnnotations;

namespace FinalcapstoneDriversLicense.Dtos.Application
{
    public class UpdateApplicationStatusRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Comment { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }
    }
}
