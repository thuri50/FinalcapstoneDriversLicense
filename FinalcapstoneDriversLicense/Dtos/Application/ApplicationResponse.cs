namespace FinalcapstoneDriversLicense.Dtos.Application
{
    public class ApplicationResponse
    {
        public int Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string LicenseType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime? CollectionDate { get; set; }
        public string? RejectionReason { get; set; }
        public string? ApplicantName { get; set; }
        public string? ApplicantEmail { get; set; }
        public string TrackingMessage { get; set; } = string.Empty;
    }
}
