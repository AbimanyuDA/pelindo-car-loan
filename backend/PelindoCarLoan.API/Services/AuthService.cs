using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Models;
using PelindoCarLoan.API.Repositories;

namespace PelindoCarLoan.API.Services
{
    /// <summary>
    /// Service interface for authentication operations
    /// </summary>
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<UserDto?> RegisterAsync(RegisterUserDto request);
        Task<UserDto?> GetCurrentUserAsync(int userId);
        string GenerateJwtToken(User user);
        bool ValidatePassword(string password, string passwordHash);
        string HashPassword(string password);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", request.Email);
                
                var user = await _userRepository.GetByEmailAsync(request.Email);
                
                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed - user not found: {Email}", request.Email);
                    return null;
                }

                _logger.LogInformation("User found: {Name}, PasswordHash length: {Length}", user.Name, user.PasswordHash?.Length ?? 0);

                if (!ValidatePassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login attempt failed - invalid password for user: {Email}", request.Email);
                    return null;
                }

                var token = GenerateJwtToken(user);
                var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "480");

                _logger.LogInformation("User logged in successfully: {Email}", request.Email);

                return new LoginResponseDto
                {
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        Role = user.Role,
                        Division = user.Division,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                throw;
            }
        }

        public async Task<UserDto?> RegisterAsync(RegisterUserDto request)
        {
            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                _logger.LogWarning("Registration failed - email already exists: {Email}", request.Email);
                return null;
            }

            // Validate role
            if (!UserRoles.AllRoles.Contains(request.Role))
            {
                _logger.LogWarning("Registration failed - invalid role: {Role}", request.Role);
                return null;
            }

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                Role = request.Role,
                Division = request.Division,
                IsActive = true
            };

            var userId = await _userRepository.CreateAsync(user);
            user.Id = userId;

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Division = user.Division,
                IsActive = user.IsActive,
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task<UserDto?> GetCurrentUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Division = user.Division,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("division", user.Division ?? string.Empty)
            };

            var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "480");

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool ValidatePassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
