namespace PelindoCarLoan.API.DTOs
{
    /// <summary>
    /// Vehicle response DTO
    /// </summary>
    public class VehicleDto
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Model { get; set; }
        public int? Year { get; set; }
        public int Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastMaintenance { get; set; }
        public DateTime? NextMaintenance { get; set; }
    }

    /// <summary>
    /// Create/Update vehicle DTO
    /// </summary>
    public class CreateVehicleDto
    {
        public string PlateNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Model { get; set; }
        public int? Year { get; set; }
        public int Capacity { get; set; } = 4;
    }

    /// <summary>
    /// Update vehicle status DTO
    /// </summary>
    public class UpdateVehicleStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
