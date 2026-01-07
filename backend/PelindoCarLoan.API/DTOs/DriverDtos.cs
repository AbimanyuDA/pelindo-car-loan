namespace PelindoCarLoan.API.DTOs
{
    /// <summary>
    /// Driver response DTO
    /// </summary>
    public class DriverDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? DriverName { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime LicenseExpiry { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? ExperienceYears { get; set; }
        public decimal? Rating { get; set; }
    }

    /// <summary>
    /// Create/Update driver DTO
    /// </summary>
    public class CreateDriverDto
    {
        public int? UserId { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime LicenseExpiry { get; set; }
        public int? ExperienceYears { get; set; }
    }

    /// <summary>
    /// Update driver status DTO
    /// </summary>
    public class UpdateDriverStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
