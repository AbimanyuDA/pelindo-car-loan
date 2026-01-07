namespace PelindoCarLoan.API.DTOs
{
    /// <summary>
    /// Dashboard statistics DTO
    /// </summary>
    public class DashboardStatsDto
    {
        public int TotalRequests { get; set; }
        public int PendingApprovals { get; set; }
        public int ScheduledTrips { get; set; }
        public int CompletedTrips { get; set; }
        public int AvailableVehicles { get; set; }
        public int AvailableDrivers { get; set; }
        public int WaitingResources { get; set; }
    }

    /// <summary>
    /// Recent activity DTO
    /// </summary>
    public class RecentActivityDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // REQUEST, APPROVAL, SCHEDULE
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ActorName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dashboard response DTO
    /// </summary>
    public class DashboardDto
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
        public List<LoanRequestListDto> MyRecentRequests { get; set; } = new();
        public List<DriverScheduleDto> UpcomingSchedules { get; set; } = new();
    }
}
