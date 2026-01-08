using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Services;

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
        private readonly ILogger<ApprovalsController> _logger;

        public ApprovalsController(IApprovalService approvalService, ILogger<ApprovalsController> logger)
        {
            _approvalService = approvalService;
            _logger = logger;
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
        /// Gets count of pending approvals for each level
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
    }
}
