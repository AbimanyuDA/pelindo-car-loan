namespace PelindoCarLoan.API.DTOs
{
    /// <summary>
    /// DTO for creating a new user
    /// </summary>
    public class CreateUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Division { get; set; }
        public string? UnitKerja { get; set; }
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// DTO for updating a user
    /// </summary>
    public class UpdateUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Division { get; set; }
        public string? UnitKerja { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for bulk user import from Excel
    /// </summary>
    public class BulkUserImportDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Division { get; set; }
        public string? UnitKerja { get; set; }
        public string? PhoneNumber { get; set; }
        public string? DefaultPassword { get; set; } // Optional, jika tidak ada akan generate default
    }

    /// <summary>
    /// Result of bulk import operation
    /// </summary>
    public class BulkImportResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<BulkImportErrorDto> Errors { get; set; } = new();
    }

    public class BulkImportErrorDto
    {
        public int RowNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// User list response DTO
    /// </summary>
    public class UserListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Division { get; set; }
        public string? UnitKerja { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
