using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Models;
using PelindoCarLoan.API.Repositories;

namespace PelindoCarLoan.API.Services
{
    /// <summary>
    /// Service interface for dashboard operations
    /// </summary>
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardAsync(int userId, string role);
        Task<DashboardStatsDto> GetStatsAsync(int? userId = null, string? role = null);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ILoanRequestRepository _loanRequestRepository;
        private readonly IApprovalRepository _approvalRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ISchedulingService _schedulingService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ILoanRequestRepository loanRequestRepository,
            IApprovalRepository approvalRepository,
            IScheduleRepository scheduleRepository,
            IDriverRepository driverRepository,
            IVehicleRepository vehicleRepository,
            ISchedulingService schedulingService,
            ILogger<DashboardService> logger)
        {
            _loanRequestRepository = loanRequestRepository;
            _approvalRepository = approvalRepository;
            _scheduleRepository = scheduleRepository;
            _driverRepository = driverRepository;
            _vehicleRepository = vehicleRepository;
            _schedulingService = schedulingService;
            _logger = logger;
        }

        public async Task<DashboardDto> GetDashboardAsync(int userId, string role)
        {
            var dashboard = new DashboardDto
            {
                Stats = await GetStatsAsync(userId, role)
            };

            // Get role-specific data
            switch (role)
            {
                case UserRoles.Pemohon:
                    var myRequests = await _loanRequestRepository.GetAllAsync(userId: userId);
                    dashboard.MyRecentRequests = myRequests.Take(5).Select(lr => new LoanRequestListDto
                    {
                        Id = lr.Id,
                        RequestNumber = lr.RequestNumber,
                        RequesterName = lr.User?.Name ?? "Unknown",
                        Purpose = lr.Purpose,
                        Destination = lr.Destination,
                        StartDatetime = lr.StartDatetime,
                        EndDatetime = lr.EndDatetime,
                        Status = lr.Status,
                        CreatedAt = lr.CreatedAt
                    }).ToList();
                    break;

                case UserRoles.Driver:
                    dashboard.UpcomingSchedules = (await _schedulingService.GetUpcomingDriverSchedulesAsync(userId)).ToList();
                    break;

                case UserRoles.Admin:
                case UserRoles.PicApprovalL1:
                case UserRoles.PicApprovalL2:
                    // Get recent activities
                    var recentRequests = await _loanRequestRepository.GetAllAsync();
                    dashboard.RecentActivities = recentRequests.Take(10).Select(lr => new RecentActivityDto
                    {
                        Id = lr.Id,
                        Type = "REQUEST",
                        Description = $"Loan request {lr.RequestNumber} - {lr.Destination}",
                        Status = lr.Status,
                        Timestamp = lr.UpdatedAt,
                        ActorName = lr.User?.Name ?? "Unknown"
                    }).ToList();
                    break;
            }

            return dashboard;
        }

        public async Task<DashboardStatsDto> GetStatsAsync(int? userId = null, string? role = null)
        {
            var stats = new DashboardStatsDto();

            // Total requests (based on role)
            if (role == UserRoles.Pemohon && userId.HasValue)
            {
                stats.TotalRequests = await _loanRequestRepository.GetTotalCountAsync(userId);
            }
            else
            {
                stats.TotalRequests = await _loanRequestRepository.GetTotalCountAsync();
            }

            // Pending approvals
            var pendingL1 = await _loanRequestRepository.GetCountByStatusAsync(LoanRequestStatus.Submitted);
            var pendingL2 = await _loanRequestRepository.GetCountByStatusAsync(LoanRequestStatus.ApprovedL1);
            
            if (role == UserRoles.PicApprovalL1)
            {
                stats.PendingApprovals = pendingL1;
            }
            else if (role == UserRoles.PicApprovalL2)
            {
                stats.PendingApprovals = pendingL2;
            }
            else
            {
                stats.PendingApprovals = pendingL1 + pendingL2;
            }

            // Scheduled trips
            stats.ScheduledTrips = await _schedulingService.GetScheduledCountAsync();

            // Completed trips
            stats.CompletedTrips = await _loanRequestRepository.GetCountByStatusAsync(LoanRequestStatus.Completed);

            // Available resources
            stats.AvailableVehicles = await _vehicleRepository.GetAvailableCountAsync();
            stats.AvailableDrivers = await _driverRepository.GetAvailableCountAsync();

            // Waiting resources
            stats.WaitingResources = await _loanRequestRepository.GetCountByStatusAsync(LoanRequestStatus.WaitingResource);

            return stats;
        }
    }
}
