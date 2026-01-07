namespace PelindoCarLoan.API.Models
{
    /// <summary>
    /// Represents a driver in the system
    /// </summary>
    public class Driver
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime LicenseExpiry { get; set; }
        public string? PhoneNumber { get; set; }
        public string Status { get; set; } = DriverStatus.Available;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation property
        public User? User { get; set; }
    }

    /// <summary>
    /// Driver status constants
    /// </summary>
    public static class DriverStatus
    {
        public const string Available = "AVAILABLE";
        public const string OnDuty = "ON_DUTY";
        public const string OffDuty = "OFF_DUTY";
        public const string Leave = "LEAVE";
        
        public static readonly string[] AllStatuses = { Available, OnDuty, OffDuty, Leave };
    }
}
