using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Services;

namespace PelindoCarLoan.API.Controllers
{
    [Authorize(Roles = "ADMIN")]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserListDto>>>> GetAll()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(ApiResponse<IEnumerable<UserListDto>>.SuccessResponse(users, "Users retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Terjadi kesalahan saat mengambil data users"));
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserListDto>>> GetById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("User tidak ditemukan"));
                }

                return Ok(ApiResponse<UserListDto>.SuccessResponse(user, "User retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Terjadi kesalahan saat mengambil data user"));
            }
        }

        /// <summary>
        /// Create new user
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] CreateUserDto dto)
        {
            try
            {
                var userId = await _userService.CreateUserAsync(dto);
                return Ok(ApiResponse<int>.SuccessResponse(userId, "User berhasil dibuat"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Terjadi kesalahan saat membuat user"));
            }
        }

        /// <summary>
        /// Update existing user
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var success = await _userService.UpdateUserAsync(id, dto);
                if (!success)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("User tidak ditemukan"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(null, "User berhasil diupdate"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Terjadi kesalahan saat mengupdate user"));
            }
        }

        /// <summary>
        /// Delete user (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            try
            {
                var success = await _userService.DeleteUserAsync(id);
                if (!success)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("User tidak ditemukan"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(null, "User berhasil dihapus"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Terjadi kesalahan saat menghapus user"));
            }
        }

        /// <summary>
        /// Import users from Excel file
        /// Format: FullName | Email | Role | Division | UnitKerja | PhoneNumber | Password
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<ApiResponse<BulkImportResultDto>>> ImportFromExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("File Excel tidak boleh kosong"));
                }

                if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("File harus berformat .xlsx"));
                }

                using var stream = file.OpenReadStream();
                var result = await _userService.ImportUsersFromExcelAsync(stream);

                return Ok(ApiResponse<BulkImportResultDto>.SuccessResponse(result,
                    $"Import selesai: {result.SuccessCount} berhasil, {result.FailedCount} gagal"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing users from Excel");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Terjadi kesalahan saat mengimport users dari Excel"));
            }
        }

        /// <summary>
        /// Download Excel template for bulk import
        /// </summary>
        [HttpGet("template")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                
                using var package = new OfficeOpenXml.ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Users");

                // Header
                worksheet.Cells[1, 1].Value = "FullName";
                worksheet.Cells[1, 2].Value = "Email";
                worksheet.Cells[1, 3].Value = "Role";
                worksheet.Cells[1, 4].Value = "Division";
                worksheet.Cells[1, 5].Value = "UnitKerja";
                worksheet.Cells[1, 6].Value = "PhoneNumber";
                worksheet.Cells[1, 7].Value = "Password";

                // Sample data
                worksheet.Cells[2, 1].Value = "John Doe";
                worksheet.Cells[2, 2].Value = "john.doe@pelindo.co.id";
                worksheet.Cells[2, 3].Value = "PEMOHON";
                worksheet.Cells[2, 4].Value = "IT Department";
                worksheet.Cells[2, 5].Value = "Software Development";
                worksheet.Cells[2, 6].Value = "081234567890";
                worksheet.Cells[2, 7].Value = "Password123!";

                worksheet.Cells[3, 1].Value = "Jane Smith";
                worksheet.Cells[3, 2].Value = "jane.smith@pelindo.co.id";
                worksheet.Cells[3, 3].Value = "DRIVER";
                worksheet.Cells[3, 4].Value = "Operations";
                worksheet.Cells[3, 5].Value = "Fleet Management";
                worksheet.Cells[3, 6].Value = "081234567891";
                worksheet.Cells[3, 7].Value = "Password123!";

                // Format header
                using (var range = worksheet.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream.ToArray(), 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "template_import_users.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel template");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Terjadi kesalahan saat membuat template"));
            }
        }
    }
}
