using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Models;
using PelindoCarLoan.API.Repositories;
using OfficeOpenXml;
using BC = BCrypt.Net.BCrypt;

namespace PelindoCarLoan.API.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserListDto>> GetAllUsersAsync();
        Task<UserListDto?> GetUserByIdAsync(int id);
        Task<int> CreateUserAsync(CreateUserDto dto);
        Task<bool> UpdateUserAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(int id);
        Task<BulkImportResultDto> ImportUsersFromExcelAsync(Stream excelStream);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<UserListDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(u => new UserListDto
            {
                Id = u.Id,
                FullName = u.Name,
                Email = u.Email,
                Role = u.Role,
                Division = u.Division,
                UnitKerja = u.UnitKerja,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            });
        }

        public async Task<UserListDto?> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return null;

            return new UserListDto
            {
                Id = user.Id,
                FullName = user.Name,
                Email = user.Email,
                Role = user.Role,
                Division = user.Division,
                UnitKerja = user.UnitKerja,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<int> CreateUserAsync(CreateUserDto dto)
        {
            // Validate email uniqueness
            if (await _userRepository.EmailExistsAsync(dto.Email))
            {
                throw new InvalidOperationException($"Email {dto.Email} sudah digunakan");
            }

            // Validate role
            var validRoles = new[] { "PEMOHON", "PIC_APPROVAL_L1", "PIC_APPROVAL_L2", "DRIVER", "ADMIN" };
            if (!validRoles.Contains(dto.Role.ToUpper()))
            {
                throw new ArgumentException($"Role tidak valid. Harus salah satu dari: {string.Join(", ", validRoles)}");
            }

            // Hash password
            var passwordHash = BC.HashPassword(dto.Password);

            var user = new User
            {
                Name = dto.FullName,
                Email = dto.Email.ToLower(),
                PasswordHash = passwordHash,
                Role = dto.Role.ToUpper(),
                Division = dto.Division,
                UnitKerja = dto.UnitKerja,
                PhoneNumber = dto.PhoneNumber,
                IsActive = true
            };

            return await _userRepository.CreateAsync(user);
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            // Check if email changed and is unique
            if (user.Email != dto.Email.ToLower())
            {
                if (await _userRepository.EmailExistsAsync(dto.Email))
                {
                    throw new InvalidOperationException($"Email {dto.Email} sudah digunakan");
                }
            }

            // Validate role
            var validRoles = new[] { "PEMOHON", "PIC_APPROVAL_L1", "PIC_APPROVAL_L2", "DRIVER", "ADMIN" };
            if (!validRoles.Contains(dto.Role.ToUpper()))
            {
                throw new ArgumentException($"Role tidak valid. Harus salah satu dari: {string.Join(", ", validRoles)}");
            }

            user.Name = dto.FullName;
            user.Email = dto.Email.ToLower();
            user.Role = dto.Role.ToUpper();
            user.Division = dto.Division;
            user.UnitKerja = dto.UnitKerja;
            user.PhoneNumber = dto.PhoneNumber;
            user.IsActive = dto.IsActive;

            return await _userRepository.UpdateAsync(user);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            return await _userRepository.DeleteAsync(id);
        }

        public async Task<BulkImportResultDto> ImportUsersFromExcelAsync(Stream excelStream)
        {
            var result = new BulkImportResultDto();
            var validRoles = new[] { "PEMOHON", "PIC_APPROVAL_L1", "PIC_APPROVAL_L2", "DRIVER", "ADMIN" };

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets[0]; // First sheet
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount < 2)
            {
                throw new InvalidOperationException("File Excel kosong atau tidak memiliki data");
            }

            result.TotalRows = rowCount - 1; // Exclude header row

            // Expected columns: FullName | Email | Role | Division | UnitKerja | PhoneNumber | Password
            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var fullName = worksheet.Cells[row, 1].Text.Trim();
                    var email = worksheet.Cells[row, 2].Text.Trim();
                    var role = worksheet.Cells[row, 3].Text.Trim().ToUpper();
                    var division = worksheet.Cells[row, 4].Text.Trim();
                    var unitKerja = worksheet.Cells[row, 5].Text.Trim();
                    var phoneNumber = worksheet.Cells[row, 6].Text.Trim();
                    var password = worksheet.Cells[row, 7].Text.Trim();

                    // Validation
                    if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(role))
                    {
                        result.Errors.Add(new BulkImportErrorDto
                        {
                            RowNumber = row,
                            Email = email,
                            ErrorMessage = "FullName, Email, dan Role harus diisi"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    if (!validRoles.Contains(role))
                    {
                        result.Errors.Add(new BulkImportErrorDto
                        {
                            RowNumber = row,
                            Email = email,
                            ErrorMessage = $"Role tidak valid: {role}. Harus salah satu dari: {string.Join(", ", validRoles)}"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    // Check email uniqueness
                    if (await _userRepository.EmailExistsAsync(email))
                    {
                        result.Errors.Add(new BulkImportErrorDto
                        {
                            RowNumber = row,
                            Email = email,
                            ErrorMessage = "Email sudah digunakan"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    // Use default password if not provided
                    var passwordToUse = string.IsNullOrWhiteSpace(password) ? "Password123!" : password;

                    // Create user
                    var user = new User
                    {
                        Name = fullName,
                        Email = email.ToLower(),
                        PasswordHash = BC.HashPassword(passwordToUse),
                        Role = role,
                        Division = string.IsNullOrWhiteSpace(division) ? null : division,
                        UnitKerja = string.IsNullOrWhiteSpace(unitKerja) ? null : unitKerja,
                        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber,
                        IsActive = true
                    };

                    await _userRepository.CreateAsync(user);
                    result.SuccessCount++;

                    _logger.LogInformation("User imported successfully: {Email} with role {Role}", email, role);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new BulkImportErrorDto
                    {
                        RowNumber = row,
                        Email = worksheet.Cells[row, 2].Text,
                        ErrorMessage = ex.Message
                    });
                    result.FailedCount++;
                    _logger.LogError(ex, "Error importing user from row {Row}", row);
                }
            }

            return result;
        }
    }
}
