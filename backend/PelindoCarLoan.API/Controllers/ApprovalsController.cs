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
    /// Controller for managing approvals
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ApprovalsController : BaseController
    {
        private readonly IApprovalService _approvalService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ApprovalsController> _logger;
        private readonly IConfiguration _configuration;

        public ApprovalsController(
            IApprovalService approvalService,
            INotificationService notificationService,
            ILogger<ApprovalsController> logger,
            IConfiguration configuration)
        {
            _approvalService = approvalService;
            _notificationService = notificationService;
            _logger = logger;
            _configuration = configuration;
        }

        private bool TryGetUserFromToken(string token, out string userId, out string role)
        {
            userId = "anonymous";
            role = "ADMIN";

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
                role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "ADMIN";
                return !string.IsNullOrWhiteSpace(userId) && userId != "anonymous";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets pending approvals for Level 1 (PIC_APPROVAL_L1)
        /// </summary>
        /// <returns>List of pending approvals</returns>
        [HttpGet("pending/l1")]
        [Authorize(Roles = "PIC_APPROVAL_L1,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PendingApprovalDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingL1()
        {
            var approvals = await _approvalService.GetPendingApprovalsAsync(1);
            return Ok(ApiResponse<IEnumerable<PendingApprovalDto>>.SuccessResponse(approvals));
        }

        /// <summary>
        /// Gets pending approvals for Level 2 (PIC_APPROVAL_L2)
        /// </summary>
        /// <returns>List of pending approvals</returns>
        [HttpGet("pending/l2")]
        [Authorize(Roles = "PIC_APPROVAL_L2,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PendingApprovalDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingL2()
        {
            var approvals = await _approvalService.GetPendingApprovalsAsync(2);
            return Ok(ApiResponse<IEnumerable<PendingApprovalDto>>.SuccessResponse(approvals));
        }

        /// <summary>
        /// Gets emergency approvals for Level 1 (PIC_APPROVAL_L1)
        /// </summary>
        /// <returns>List of emergency approvals</returns>
        [HttpGet("emergency/l1")]
        [Authorize(Roles = "PIC_APPROVAL_L1,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PendingApprovalDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmergencyL1()
        {
            var approvals = await _approvalService.GetEmergencyApprovalsAsync(1);
            return Ok(ApiResponse<IEnumerable<PendingApprovalDto>>.SuccessResponse(approvals));
        }

        /// <summary>
        /// Gets emergency approvals for Level 2 (PIC_APPROVAL_L2)
        /// </summary>
        /// <returns>List of emergency approvals</returns>
        [HttpGet("emergency/l2")]
        [Authorize(Roles = "PIC_APPROVAL_L2,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PendingApprovalDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmergencyL2()
        {
            var approvals = await _approvalService.GetEmergencyApprovalsAsync(2);
            return Ok(ApiResponse<IEnumerable<PendingApprovalDto>>.SuccessResponse(approvals));
        }

        /// <summary>
        /// Process Level 1 approval (PIC_APPROVAL_L1)
        /// </summary>
        /// <param name="dto">Approval decision</param>
        /// <returns>Approval result</returns>
        [HttpPost("process/l1")]
        [Authorize(Roles = "PIC_APPROVAL_L1,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<ApprovalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ProcessL1([FromBody] ProcessApprovalDto dto)
        {
            var result = await _approvalService.ProcessApprovalL1Async(CurrentUserId, dto);
            var message = dto.Status == "APPROVED" ? "Approval Level 1 processed - Approved" : "Approval Level 1 processed - Rejected";
            return Ok(ApiResponse<ApprovalDto>.SuccessResponse(result, message));
        }

        /// <summary>
        /// Process Level 2 approval (PIC_APPROVAL_L2)
        /// </summary>
        /// <param name="dto">Approval decision</param>
        /// <returns>Approval result</returns>
        [HttpPost("process/l2")]
        [Authorize(Roles = "PIC_APPROVAL_L2,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<ApprovalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ProcessL2([FromBody] ProcessApprovalDto dto)
        {
            var result = await _approvalService.ProcessApprovalL2Async(CurrentUserId, dto);
            var message = dto.Status == "APPROVED" 
                ? "Approval Level 2 processed - Approved. Auto-scheduling initiated." 
                : "Approval Level 2 processed - Rejected";
            return Ok(ApiResponse<ApprovalDto>.SuccessResponse(result, message));
        }

        /// <summary>
        /// Gets approval history for a loan request
        /// </summary>
        /// <param name="loanRequestId">Loan request ID</param>
        /// <returns>List of approvals</returns>
        [HttpGet("history/{loanRequestId}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ApprovalDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistory(int loanRequestId)
        {
            var approvals = await _approvalService.GetApprovalHistoryAsync(loanRequestId);
            return Ok(ApiResponse<IEnumerable<ApprovalDto>>.SuccessResponse(approvals));
        }

        /// <summary>
        /// Gets pending count of approvals for each level
        /// </summary>
        /// <returns>Pending counts</returns>
        [HttpGet("pending/count")]
        [Authorize(Roles = "PIC_APPROVAL_L1,PIC_APPROVAL_L2,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingCount()
        {
            var l1Count = await _approvalService.GetPendingCountAsync(1);
            var l2Count = await _approvalService.GetPendingCountAsync(2);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                Level1 = l1Count,
                Level2 = l2Count,
                Total = l1Count + l2Count
            }));
        }

        /// <summary>
        /// Subscribe to real-time approval updates via Server-Sent Events (SSE)
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
                userRole = !string.IsNullOrEmpty(CurrentUserRole) ? CurrentUserRole : "ADMIN";
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
