using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Services;

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
        private readonly ILogger<LoanRequestsController> _logger;

        public LoanRequestsController(ILoanRequestService loanRequestService, ILogger<LoanRequestsController> logger)
        {
            _loanRequestService = loanRequestService;
            _logger = logger;
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
    }
}
