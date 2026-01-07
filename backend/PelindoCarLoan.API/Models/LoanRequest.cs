namespace PelindoCarLoan.API.Models
{
    /// <summary>
    /// Represents a vehicle loan request
    /// </summary>
    public class LoanRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int PassengerCount { get; set; } = 1;
        public DateTime StartDatetime { get; set; }
        public DateTime EndDatetime { get; set; }
        public string Status { get; set; } = LoanRequestStatus.Submitted;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        public User? User { get; set; }
        public List<Approval>? Approvals { get; set; }
        public Schedule? Schedule { get; set; }
    }

    /// <summary>
    /// Loan request status constants
    /// </summary>
    public static class LoanRequestStatus
    {
        public const string Submitted = "SUBMITTED";
        public const string ApprovedL1 = "APPROVED_L1";
        public const string RejectedL1 = "REJECTED_L1";
        public const string ApprovedL2 = "APPROVED_L2";
        public const string RejectedL2 = "REJECTED_L2";
        public const string Scheduled = "SCHEDULED";
        public const string WaitingResource = "WAITING_RESOURCE";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
        
        public static readonly string[] AllStatuses = 
        { 
            Submitted, ApprovedL1, RejectedL1, ApprovedL2, RejectedL2, 
            Scheduled, WaitingResource, InProgress, Completed, Cancelled 
        };
    }
}
