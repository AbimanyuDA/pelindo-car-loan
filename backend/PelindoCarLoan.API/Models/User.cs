namespace PelindoCarLoan.API.Models
{
    /// <summary>
    /// Represents a user in the system
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Division { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// User roles enumeration
    /// </summary>
    public static class UserRoles
    {
        public const string Pemohon = "PEMOHON";
        public const string PicApprovalL1 = "PIC_APPROVAL_L1";
        public const string PicApprovalL2 = "PIC_APPROVAL_L2";
        public const string Driver = "DRIVER";
        public const string Admin = "ADMIN";
        
        public static readonly string[] AllRoles = { Pemohon, PicApprovalL1, PicApprovalL2, Driver, Admin };
    }
}
