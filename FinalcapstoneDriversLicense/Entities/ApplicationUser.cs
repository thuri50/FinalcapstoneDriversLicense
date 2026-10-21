using Microsoft.AspNetCore.Identity;

namespace FinalcapstoneDriversLicense.Entities
{
    public class ApplicationUser:IdentityUser<Guid>
    {
        
        public string FullName { get; set; } = string.Empty;
       
        
        public ICollection<LicenseApplication> Applications { get; set; } = new List<LicenseApplication>();
    }
}

