namespace PelindoCarLoan.API.DTOs
{
    /// <summary>
    /// Manual schedule assignment DTO (for Admin override)
    /// </summary>
    public class AssignScheduleDto
    {
        public int LoanRequestId { get; set; }
        public int DriverId { get; set; }
        public int VehicleId { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Update schedule status DTO
    /// </summary>
    public class UpdateScheduleStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Schedule response DTO
    /// </summary>
    public class ScheduleDto
    {
        public int Id { get; set; }
        public int LoanRequestId { get; set; }
        public int DriverId { get; set; }
        public int VehicleId { get; set; }
        public DateTime AssignedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        
        // Related data
        public LoanRequestDto? LoanRequest { get; set; }
        public DriverDto? Driver { get; set; }
        public VehicleDto? Vehicle { get; set; }
    }

    /// <summary>
    /// Schedule list item DTO for driver view
    /// </summary>
    public class DriverScheduleDto
    {
        public int ScheduleId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int PassengerCount { get; set; }
        public DateTime StartDatetime { get; set; }
        public DateTime EndDatetime { get; set; }
        public string VehiclePlate { get; set; } = string.Empty;
        public string VehicleBrand { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
