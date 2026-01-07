using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Services;

namespace PelindoCarLoan.API.Controllers
{
    /// <summary>
    /// Controller for dashboard data
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Gets dashboard data for the current user
        /// </summary>
        /// <returns>Dashboard data including stats and recent activities</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard()
        {
            var dashboard = await _dashboardService.GetDashboardAsync(CurrentUserId, CurrentUserRole);
            return Ok(ApiResponse<DashboardDto>.SuccessResponse(dashboard));
        }

        /// <summary>
        /// Gets statistics summary
        /// </summary>
        /// <returns>Dashboard statistics</returns>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _dashboardService.GetStatsAsync(CurrentUserId, CurrentUserRole);
            return Ok(ApiResponse<DashboardStatsDto>.SuccessResponse(stats));
        }
    }
}
