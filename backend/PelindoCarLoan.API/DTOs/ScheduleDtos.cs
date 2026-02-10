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
    /// Cancel schedule request DTO
    /// </summary>
    public class CancelScheduleRequestDto
    {
        public string CancellationReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Pre-departure preparation DTO
    /// </summary>
    public class PreDepartureDto
    {
        public int? ActualVehicleId { get; set; }
        public string? FuelCondition { get; set; }
    }

    /// <summary>
    /// Emergency report DTO
    /// </summary>
    public class EmergencyReportDto
    {
        public string EmergencyReason { get; set; } = string.Empty;
        public string? DriverMessage { get; set; }
    }

    /// <summary>
    /// Start journey DTO
    /// </summary>
    public class DriverConfirmationDto
    {
        public int? ActualVehicleId { get; set; }
        public string? FuelCondition { get; set; }
    }

    public class StartJourneyDto
    {
        public DateTime ActualStartTime { get; set; }
    }

    /// <summary>
    /// Complete journey DTO
    /// </summary>
    public class CompleteJourneyDto
    {
        public DateTime ActualEndTime { get; set; }
        public string? FinalFuelCondition { get; set; }
        public bool IsRefueled { get; set; }
        public decimal? RefuelAmount { get; set; }
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
        
        // Pre-departure & actual journey
        public int? ActualVehicleId { get; set; }
        public string? FuelCondition { get; set; }
        public string? KmPhotoPath { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        
        // Emergency
        public string? EmergencyReason { get; set; }
        public string? DriverMessage { get; set; }
        
        // Related data
        public LoanRequestDto? LoanRequest { get; set; }
        public DriverDto? Driver { get; set; }
        public VehicleDto? Vehicle { get; set; }
        public VehicleDto? ActualVehicle { get; set; }
    }

    /// <summary>
    /// Schedule list item DTO for driver view
    /// </summary>
    public class DriverScheduleDto
    {
        public int ScheduleId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
        public string RequesterEmail { get; set; } = string.Empty;
        public string RequesterPhone { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string GuestList { get; set; } = string.Empty;
        public bool HotelAccommodation { get; set; }
        public string? HotelName { get; set; }
        public DateTime StartDatetime { get; set; }
        public DateTime EndDatetime { get; set; }
        public int VehicleId { get; set; }
        public string VehiclePlate { get; set; } = string.Empty;
        public string VehicleBrand { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        
        // Actual journey data
        public int? ActualVehicleId { get; set; }
        public string? FuelCondition { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public string? EmergencyReason { get; set; }
    }
}
