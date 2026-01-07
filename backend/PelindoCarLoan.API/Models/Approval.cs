namespace PelindoCarLoan.API.Models
{
    /// <summary>
    /// Represents an approval record for a loan request
    /// </summary>
    public class Approval
    {
        public int Id { get; set; }
        public int LoanRequestId { get; set; }
        public int ApproverId { get; set; }
        public int ApprovalLevel { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime ApprovedAt { get; set; }
        
        // Navigation properties
        public LoanRequest? LoanRequest { get; set; }
        public User? Approver { get; set; }
    }

    /// <summary>
    /// Approval status constants
    /// </summary>
    public static class ApprovalStatus
    {
        public const string Approved = "APPROVED";
        public const string Rejected = "REJECTED";
        
        public static readonly string[] AllStatuses = { Approved, Rejected };
    }
    
    /// <summary>
    /// Approval level constants
    /// </summary>
    public static class ApprovalLevel
    {
        public const int Level1 = 1;
        public const int Level2 = 2;
    }
}
