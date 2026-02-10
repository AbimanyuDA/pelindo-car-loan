namespace PelindoCarLoan.API.DTOs
{
    /// <summary>
    /// Process approval request DTO
    /// </summary>
    public class ProcessApprovalDto
    {
        public int LoanRequestId { get; set; }
        public string Status { get; set; } = string.Empty; // APPROVED or REJECTED
        public string? Notes { get; set; }
        public int? VehicleId { get; set; }
        public int? DriverId { get; set; }
    }

    /// <summary>
    /// Approval response DTO
    /// </summary>
    public class ApprovalDto
    {
        public int Id { get; set; }
        public int LoanRequestId { get; set; }
        public int ApproverId { get; set; }
        public int ApprovalLevel { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime ApprovedAt { get; set; }
        
        // Related data
        public string? ApproverName { get; set; }
    }

    /// <summary>
    /// Pending approval item DTO
    /// </summary>
    public class PendingApprovalDto
    {
        public int LoanRequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
        public string RequesterEmail { get; set; } = string.Empty;
        public string? RequesterPhone { get; set; }
        public string RequesterDivision { get; set; } = string.Empty;
        public string? RequesterUnitKerja { get; set; }
        public string? ServiceLetterBasis { get; set; }
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
        public int RequiredApprovalLevel { get; set; }
        public string? EmergencyReason { get; set; }
        public string? EmergencyType { get; set; }
    }
}
