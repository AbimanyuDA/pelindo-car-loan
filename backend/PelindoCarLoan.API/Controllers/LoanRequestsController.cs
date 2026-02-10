using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PelindoCarLoan.API.Controllers
{
    /// <summary>
    /// Controller for managing loan requests
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoanRequestsController : BaseController
    {
        private readonly ILoanRequestService _loanRequestService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<LoanRequestsController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public LoanRequestsController(
            ILoanRequestService loanRequestService,
            INotificationService notificationService,
            ILogger<LoanRequestsController> logger,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _loanRequestService = loanRequestService;
            _notificationService = notificationService;
            _logger = logger;
            _env = env;
            _configuration = configuration;
        }

        private bool TryGetUserFromToken(string token, out string userId, out string role)
        {
            userId = "anonymous";
            role = "PEMOHON";

            try
            {
                var secretKey = _configuration["JwtSettings:SecretKey"];
                var issuer = _configuration["JwtSettings:Issuer"];
                var audience = _configuration["JwtSettings:Audience"];

                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    return false;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                }, out _);

                userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "PEMOHON";
                return !string.IsNullOrWhiteSpace(userId) && userId != "anonymous";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets all loan requests (Admin) or user's own requests (PEMOHON)
        /// </summary>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of loan requests</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<LoanRequestListDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            IEnumerable<LoanRequestListDto> requests;

            // PEMOHON can only see their own requests
            if (CurrentUserRole == "PEMOHON")
            {
                requests = await _loanRequestService.GetMyRequestsAsync(CurrentUserId);
            }
            else
            {
                requests = await _loanRequestService.GetAllAsync(status: status);
            }

            return Ok(ApiResponse<IEnumerable<LoanRequestListDto>>.SuccessResponse(requests));
        }

        /// <summary>
        /// Gets the current user's loan requests
        /// </summary>
        /// <returns>List of user's loan requests</returns>
        [HttpGet("my-requests")]
        [Authorize(Roles = "PEMOHON")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<LoanRequestListDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyRequests()
        {
            var requests = await _loanRequestService.GetMyRequestsAsync(CurrentUserId);
            return Ok(ApiResponse<IEnumerable<LoanRequestListDto>>.SuccessResponse(requests));
        }

        /// <summary>
        /// Gets a specific loan request by ID
        /// </summary>
        /// <param name="id">Loan request ID</param>
        /// <returns>Loan request details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<LoanRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _loanRequestService.GetByIdAsync(id);
            
            if (request == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Loan request not found"));
            }

            // PEMOHON can only see their own requests
            if (CurrentUserRole == "PEMOHON" && request.UserId != CurrentUserId)
            {
                return Forbid();
            }

            return Ok(ApiResponse<LoanRequestDto>.SuccessResponse(request));
        }

        /// <summary>
        /// Creates a new loan request
        /// </summary>
        /// <param name="dto">Loan request details</param>
        /// <returns>Created loan request</returns>
        [HttpPost]
        [Authorize(Roles = "PEMOHON")]
        [ProducesResponseType(typeof(ApiResponse<LoanRequestDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateLoanRequestDto dto)
        {
            var request = await _loanRequestService.CreateAsync(CurrentUserId, dto);
            
            // Notify approval L1 about new loan request
            _ = _notificationService.NotifyNewLoanRequestAsync(
                CurrentUserId.ToString(), 
                User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value ?? "Pemohon");

            // Notify pemohon to refresh their own list
            _ = _notificationService.NotifyLoanRequestStatusChangeAsync(
                request.Id,
                request.Status,
                CurrentUserId.ToString());
            
            return CreatedAtAction(nameof(GetById), new { id = request.Id }, 
                ApiResponse<LoanRequestDto>.SuccessResponse(request, "Loan request created successfully"));
        }

        /// <summary>
        /// Updates an existing loan request (only SUBMITTED status)
        /// </summary>
        /// <param name="id">Loan request ID</param>
        /// <param name="dto">Updated loan request details</param>
        /// <returns>Updated loan request</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "PEMOHON")]
        [ProducesResponseType(typeof(ApiResponse<LoanRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLoanRequestDto dto)
        {
            var request = await _loanRequestService.UpdateAsync(id, CurrentUserId, dto);
            
            if (request == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Loan request not found"));
            }

            return Ok(ApiResponse<LoanRequestDto>.SuccessResponse(request, "Loan request updated successfully"));
        }

        /// <summary>
        /// Cancels a loan request
        /// </summary>
        /// <param name="id">Loan request ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "PEMOHON")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _loanRequestService.CancelAsync(id, CurrentUserId);
            
            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Loan request not found"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Loan request cancelled successfully"));
        }

        /// <summary>
        /// Upload service letter file
        /// </summary>
        /// <param name="file">File to upload</param>
        /// <returns>File path</returns>
        [HttpPost("upload-service-letter")]
        [Authorize(Roles = "PEMOHON")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadServiceLetter(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("No file uploaded"));
            }

            // Validate file type (PDF only)
            var allowedExtensions = new[] { ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Only PDF files are allowed"));
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("File size must not exceed 5MB"));
            }

            try
            {
                // Create uploads directory if not exists
                var uploadsPath = Path.Combine(_env.ContentRootPath, "uploads", "service-letters");
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path
                var relativePath = Path.Combine("uploads", "service-letters", fileName);
                _logger.LogInformation("File uploaded: {FileName} by user {UserId}", fileName, CurrentUserId);

                return Ok(ApiResponse<string>.SuccessResponse(relativePath, "File uploaded successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Error uploading file"));
            }
        }

        /// <summary>
        /// Download service letter file
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>File</returns>
        [HttpGet("download-service-letter/{fileName}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DownloadServiceLetter(string fileName)
        {
            var filePath = Path.Combine(_env.ContentRootPath, "uploads", "service-letters", fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(ApiResponse<object>.ErrorResponse("File not found"));
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf", fileName);
        }

        /// <summary>
        /// Subscribe to real-time loan request updates via Server-Sent Events (SSE)
        /// </summary>
        /// <returns>SSE stream</returns>
        [HttpGet("subscribe")]
        [AllowAnonymous]
        public async Task<IActionResult> Subscribe([FromQuery] string? token = null)
        {
            string userId;
            string userRole;

            // If token is passed as query parameter, validate it
            if (!string.IsNullOrEmpty(token))
            {
                if (!TryGetUserFromToken(token, out userId, out userRole))
                {
                    return Unauthorized();
                }
                _logger.LogInformation("SSE Subscribe with token from query parameter");
            }
            else if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }
            else
            {
                userId = CurrentUserId > 0 ? CurrentUserId.ToString() : "anonymous";
                userRole = !string.IsNullOrEmpty(CurrentUserRole) ? CurrentUserRole : "PEMOHON";
            }

            var response = Response;
            response.ContentType = "text/event-stream";
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");

            _notificationService.Subscribe(userId, userRole, response);

            await response.WriteAsync(": connected\n\n");
            await response.Body.FlushAsync();

            try
            {
                await Task.Delay(Timeout.Infinite, HttpContext.RequestAborted);
            }
            catch (TaskCanceledException)
            {
                // Client disconnected
            }
            finally
            {
                _notificationService.Unsubscribe(userId);
            }

            return new EmptyResult();
        }
    }
}
