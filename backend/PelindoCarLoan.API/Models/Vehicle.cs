namespace PelindoCarLoan.API.Models
{
    /// <summary>
    /// Represents a vehicle in the fleet
    /// </summary>
    public class Vehicle
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Capacity { get; set; } = 4;
        public string Status { get; set; } = VehicleStatus.Available;
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Vehicle status constants
    /// </summary>
    public static class VehicleStatus
    {
        public const string Available = "AVAILABLE";
        public const string InUse = "IN_USE";
        public const string Maintenance = "MAINTENANCE";
        public const string Retired = "RETIRED";
        
        public static readonly string[] AllStatuses = { Available, InUse, Maintenance, Retired };
    }
}
