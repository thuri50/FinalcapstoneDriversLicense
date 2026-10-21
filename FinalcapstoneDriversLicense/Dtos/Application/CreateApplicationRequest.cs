using System.ComponentModel.DataAnnotations;

namespace FinalcapstoneDriversLicense.Dtos.Application
{
    public class CreateApplicationRequest
    {

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required, MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required, Phone, MaxLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LicenseType { get; set; } = "Standard";
    }
}
