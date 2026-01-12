namespace PelindoCarLoan.API.DTOs
{
    /// <summary>
    /// Create loan request DTO
    /// </summary>
    public class CreateLoanRequestDto
    {
        public string ServiceLetterBasis { get; set; } = string.Empty;
        public string? ServiceLetterFilePath { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string GuestList { get; set; } = string.Empty;
        public string? HotelAccommodation { get; set; }
        public int? VehicleId { get; set; }
        public int? DriverId { get; set; }
        public DateTime StartDatetime { get; set; }
        public DateTime EndDatetime { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Update loan request DTO
    /// </summary>
    public class UpdateLoanRequestDto
    {        public string ServiceLetterBasis { get; set; } = string.Empty;        public string Purpose { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string GuestList { get; set; } = string.Empty;
        public string? HotelAccommodation { get; set; }
        public int VehicleId { get; set; }
        public int DriverId { get; set; }
        public DateTime StartDatetime { get; set; }
        public DateTime EndDatetime { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Loan request response DTO
    /// </summary>
    public class LoanRequestDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string? RequesterName { get; set; }
        public string? RequesterEmail { get; set; }
        public string? RequesterPhone { get; set; }
        public string? RequesterDivision { get; set; }
        public string? RequesterUnitKerja { get; set; }
        public string ServiceLetterBasis { get; set; } = string.Empty;
        public string? ServiceLetterFilePath { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string GuestList { get; set; } = string.Empty;
        public string? HotelAccommodation { get; set; }
        public int? VehicleId { get; set; }
        public int? DriverId { get; set; }
        public string? DriverName { get; set; }
        public string? DriverPhone { get; set; }
        public DateTime StartDatetime { get; set; }
        public DateTime EndDatetime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Related data
        public UserDto? User { get; set; }
        public List<ApprovalDto>? Approvals { get; set; }
        public ScheduleDto? Schedule { get; set; }
    }

    /// <summary>
    /// Loan request list item DTO
    /// </summary>
    public class LoanRequestListDto
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
        public string ServiceLetterBasis { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string GuestList { get; set; } = string.Empty;
        public string? HotelAccommodation { get; set; }
        public int? VehicleId { get; set; }
        public int? DriverId { get; set; }
        public DateTime StartDatetime { get; set; }
        public DateTime EndDatetime { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
