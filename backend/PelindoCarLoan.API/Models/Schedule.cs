namespace PelindoCarLoan.API.Models
{
    /// <summary>
    /// Represents a schedule assignment for a loan request
    /// </summary>
    public class Schedule
    {
        public int Id { get; set; }
        public int LoanRequestId { get; set; }
        public int? DriverId { get; set; }
        public int? VehicleId { get; set; }
        public DateTime AssignedAt { get; set; }
        public string Status { get; set; } = ScheduleStatus.Pending;
        public string? Notes { get; set; }
        
        // Navigation properties
        public LoanRequest? LoanRequest { get; set; }
        public Driver? Driver { get; set; }
        public Vehicle? Vehicle { get; set; }
    }

    /// <summary>
    /// Schedule status constants - matches database CHECK constraint
    /// </summary>
    public static class ScheduleStatus
    {
        public const string Pending = "PENDING";
        public const string Confirmed = "CONFIRMED";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
        
        public static readonly string[] AllStatuses = { Pending, Confirmed, InProgress, Completed, Cancelled };
    }
}
