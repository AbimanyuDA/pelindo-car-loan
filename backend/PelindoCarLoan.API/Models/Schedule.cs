namespace PelindoCarLoan.API.Models
{
    /// <summary>
    /// Represents a schedule assignment for a loan request
    /// </summary>
    public class Schedule
    {
        public int Id { get; set; }
        public int LoanRequestId { get; set; }
        public int DriverId { get; set; }
        public int VehicleId { get; set; }
        public int? AssignedBy { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public string Status { get; set; } = ScheduleStatus.Assigned;
        public string? Notes { get; set; }
        
        // Navigation properties
        public LoanRequest? LoanRequest { get; set; }
        public Driver? Driver { get; set; }
        public Vehicle? Vehicle { get; set; }
        public User? AssignedByUser { get; set; }
    }

    /// <summary>
    /// Schedule status constants
    /// </summary>
    public static class ScheduleStatus
    {
        public const string Assigned = "ASSIGNED";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
        
        public static readonly string[] AllStatuses = { Assigned, InProgress, Completed, Cancelled };
    }
}
