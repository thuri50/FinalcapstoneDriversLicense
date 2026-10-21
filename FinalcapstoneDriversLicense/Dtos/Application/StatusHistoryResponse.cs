namespace FinalcapstoneDriversLicense.Dtos.Application
{
    public class StatusHistoryResponse
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime DateChanged { get; set; }
    }
}
