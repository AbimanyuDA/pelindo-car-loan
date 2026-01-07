using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Services;

namespace PelindoCarLoan.API.Controllers
{
    /// <summary>
    /// Controller for managing schedules
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SchedulesController : BaseController
    {
        private readonly ISchedulingService _schedulingService;
        private readonly ILogger<SchedulesController> _logger;

        public SchedulesController(ISchedulingService schedulingService, ILogger<SchedulesController> logger)
        {
            _schedulingService = schedulingService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all schedules (Admin only)
        /// </summary>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of schedules</returns>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ScheduleDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var schedules = await _schedulingService.GetAllAsync(status);
            return Ok(ApiResponse<IEnumerable<ScheduleDto>>.SuccessResponse(schedules));
        }

        /// <summary>
        /// Gets driver's schedules
        /// </summary>
        /// <returns>List of driver's schedules</returns>
        [HttpGet("my-schedules")]
        [Authorize(Roles = "DRIVER")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DriverScheduleDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMySchedules()
        {
            var schedules = await _schedulingService.GetDriverSchedulesAsync(CurrentUserId);
            return Ok(ApiResponse<IEnumerable<DriverScheduleDto>>.SuccessResponse(schedules));
        }

        /// <summary>
        /// Gets driver's upcoming schedules
        /// </summary>
        /// <returns>List of upcoming schedules</returns>
        [HttpGet("upcoming")]
        [Authorize(Roles = "DRIVER")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DriverScheduleDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcomingSchedules()
        {
            var schedules = await _schedulingService.GetUpcomingDriverSchedulesAsync(CurrentUserId);
            return Ok(ApiResponse<IEnumerable<DriverScheduleDto>>.SuccessResponse(schedules));
        }

        /// <summary>
        /// Gets a specific schedule by ID
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Schedule details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var schedule = await _schedulingService.GetByIdAsync(id);
            
            if (schedule == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Schedule not found"));
            }

            return Ok(ApiResponse<ScheduleDto>.SuccessResponse(schedule));
        }

        /// <summary>
        /// Gets schedule by loan request ID
        /// </summary>
        /// <param name="loanRequestId">Loan request ID</param>
        /// <returns>Schedule details</returns>
        [HttpGet("by-loan-request/{loanRequestId}")]
        [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByLoanRequestId(int loanRequestId)
        {
            var schedule = await _schedulingService.GetByLoanRequestIdAsync(loanRequestId);
            
            if (schedule == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Schedule not found for this loan request"));
            }

            return Ok(ApiResponse<ScheduleDto>.SuccessResponse(schedule));
        }

        /// <summary>
        /// Manually assigns a schedule (Admin override)
        /// </summary>
        /// <param name="dto">Schedule assignment details</param>
        /// <returns>Created schedule</returns>
        [HttpPost("assign")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ManualAssign([FromBody] AssignScheduleDto dto)
        {
            var schedule = await _schedulingService.ManualScheduleAsync(CurrentUserId, dto);
            return CreatedAtAction(nameof(GetById), new { id = schedule.Id },
                ApiResponse<ScheduleDto>.SuccessResponse(schedule, "Schedule assigned successfully"));
        }

        /// <summary>
        /// Updates schedule status
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <param name="dto">Status update</param>
        /// <returns>Success status</returns>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "DRIVER,ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateScheduleStatusDto dto)
        {
            var result = await _schedulingService.UpdateScheduleStatusAsync(id, dto);
            
            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Schedule not found"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Schedule status updated successfully"));
        }

        /// <summary>
        /// Gets requests waiting for resources (Admin only)
        /// </summary>
        /// <returns>List of waiting requests</returns>
        [HttpGet("waiting-resources")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ScheduleDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWaitingResources()
        {
            var requests = await _schedulingService.GetWaitingResourceRequestsAsync();
            return Ok(ApiResponse<IEnumerable<ScheduleDto>>.SuccessResponse(requests));
        }

        /// <summary>
        /// Retries automatic scheduling for a waiting request (Admin only)
        /// </summary>
        /// <param name="loanRequestId">Loan request ID</param>
        /// <returns>Success status</returns>
        [HttpPost("retry/{loanRequestId}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RetryScheduling(int loanRequestId)
        {
            var result = await _schedulingService.RetrySchedulingAsync(loanRequestId);
            
            if (!result)
            {
                return Ok(ApiResponse<object>.ErrorResponse("No resources available. Request remains in waiting status."));
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Scheduling successful"));
        }
    }
}
