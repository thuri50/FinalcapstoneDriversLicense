namespace FinalcapstoneDriversLicense.Entities
{
    public class ApplicationStatus
    {
        public const string Submitted = "Submitted";
        public const string UnderReview = "Under Review";
        public const string Approved = "Approved";
        public const string Processing = "Processing";
        public const string ReadyForCollection = "Ready for Collection";
        public const string Collected = "Collected";
        public const string Rejected = "Rejected";
     

        public static readonly string[] All =
        {
        Submitted, UnderReview, Approved, Processing,
        ReadyForCollection, Collected, Rejected
        };
    
    }
}
