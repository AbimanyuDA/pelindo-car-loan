using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Services;

namespace PelindoCarLoan.API.Controllers
{
    /// <summary>
    /// Authentication controller for login and user management
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                
                if (result == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Email atau password salah"));
                }

                return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result, "Login successful"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Terjadi kesalahan saat login"));
            }
        }

        /// <summary>
        /// Registers a new user (Admin only)
        /// </summary>
        /// <param name="request">User registration details</param>
        /// <returns>Created user information</returns>
        [HttpPost("register")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto request)
        {
            var result = await _authService.RegisterAsync(request);
            
            if (result == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Registration failed. Email may already exist."));
            }

            return CreatedAtAction(nameof(GetCurrentUser), null, 
                ApiResponse<UserDto>.SuccessResponse(result, "User registered successfully"));
        }

        /// <summary>
        /// Gets the current authenticated user's profile
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _authService.GetCurrentUserAsync(CurrentUserId);
            
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
            }

            return Ok(ApiResponse<UserDto>.SuccessResponse(user));
        }

        /// <summary>
        /// Validates the current JWT token
        /// </summary>
        /// <returns>Token validation status</returns>
        [HttpGet("validate")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult ValidateToken()
        {
            return Ok(ApiResponse<object>.SuccessResponse(new 
            { 
                Valid = true, 
                UserId = CurrentUserId, 
                Role = CurrentUserRole 
            }, "Token is valid"));
        }

        /// <summary>
        /// Generate BCrypt hash for a password (Dev only)
        /// </summary>
        [HttpGet("hash/{password}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult GenerateHash(string password)
        {
            var hash = _authService.HashPassword(password);
            return Ok(ApiResponse<object>.SuccessResponse(new { Password = password, Hash = hash }));
        }
    }
}
