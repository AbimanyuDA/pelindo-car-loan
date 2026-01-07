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
        public int Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Create/Update vehicle DTO
    /// </summary>
    public class CreateVehicleDto
    {
        public string PlateNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Capacity { get; set; } = 4;
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Update vehicle status DTO
    /// </summary>
    public class UpdateVehicleStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
