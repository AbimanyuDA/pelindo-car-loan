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
        
        // Pre-departure fields
        public int? ActualVehicleId { get; set; }
        public string? FuelCondition { get; set; }
        public string? KmPhotoPath { get; set; }
        
        // Actual journey times
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        
        // End journey fields
        public string? FinalFuelCondition { get; set; }
        public bool IsRefueled { get; set; }
        public decimal? RefuelAmount { get; set; }
        public string? RefuelReceiptPath { get; set; }
        
        // Emergency handling
        public string? EmergencyReason { get; set; }
        public string? DriverMessage { get; set; }
        public string? EmergencyType { get; set; } // "MOGOK" or "LAINNYA"
        
        // Navigation properties
        public LoanRequest? LoanRequest { get; set; }
        public Driver? Driver { get; set; }
        public Vehicle? Vehicle { get; set; }
        public Vehicle? ActualVehicle { get; set; }
    }

    /// <summary>
    /// Schedule status constants - matches database CHECK constraint
    /// </summary>
    public static class ScheduleStatus
    {
        public const string Pending = "PENDING";
        public const string Confirmed = "CONFIRMED";
        public const string WaitingDriver = "WAITING_DRIVER";
        public const string DriverConfirmed = "DRIVER_CONFIRMED";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
        public const string Emergency = "EMERGENCY";
        public const string Waiting = "WAITING";
        public const string WaitingL2 = "WAITING_L2";
        
        public static readonly string[] AllStatuses = { Pending, Confirmed, WaitingDriver, DriverConfirmed, InProgress, Completed, Cancelled, Emergency, Waiting, WaitingL2 };
    }
}
