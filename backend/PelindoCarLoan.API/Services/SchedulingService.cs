using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Models;
using PelindoCarLoan.API.Repositories;

namespace PelindoCarLoan.API.Services
{
    /// <summary>
    /// Service interface for scheduling operations
    /// </summary>
    public interface ISchedulingService
    {
        Task<ScheduleDto?> AutoScheduleAsync(int loanRequestId);
        Task<ScheduleDto> ManualScheduleAsync(int assignedBy, AssignScheduleDto dto);
        Task<ScheduleDto?> GetByIdAsync(int id);
        Task<ScheduleDto?> GetByLoanRequestIdAsync(int loanRequestId);
        Task<IEnumerable<ScheduleDto>> GetAllAsync(string? status = null);
        Task<IEnumerable<DriverScheduleDto>> GetDriverSchedulesAsync(int userId);
        Task<IEnumerable<DriverScheduleDto>> GetUpcomingDriverSchedulesAsync(int userId);
        Task<bool> UpdateScheduleStatusAsync(int id, UpdateScheduleStatusDto dto);
        Task<IEnumerable<ScheduleDto>> GetWaitingResourceRequestsAsync();
        Task<bool> RetrySchedulingAsync(int loanRequestId);
        Task<int> GetScheduledCountAsync();
        Task<bool> CancelScheduleAsync(int scheduleId, int userId, string cancellationReason);
    }

    public class SchedulingService : ISchedulingService
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly ILoanRequestRepository _loanRequestRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ILogger<SchedulingService> _logger;

        public SchedulingService(
            IScheduleRepository scheduleRepository,
            ILoanRequestRepository loanRequestRepository,
            IDriverRepository driverRepository,
            IVehicleRepository vehicleRepository,
            ILogger<SchedulingService> logger)
        {
            _scheduleRepository = scheduleRepository;
            _loanRequestRepository = loanRequestRepository;
            _driverRepository = driverRepository;
            _vehicleRepository = vehicleRepository;
            _logger = logger;
        }

        /// <summary>
        /// Automatically schedules a driver and vehicle for an approved loan request
        /// </summary>
        public async Task<ScheduleDto?> AutoScheduleAsync(int loanRequestId)
        {
            var loanRequest = await _loanRequestRepository.GetByIdAsync(loanRequestId);
            if (loanRequest == null)
            {
                throw new ArgumentException("Loan request not found");
            }

            if (loanRequest.Status != LoanRequestStatus.ApprovedL2)
            {
                throw new InvalidOperationException("Loan request must be in APPROVED_L2 status for scheduling");
            }

            // Check if already scheduled
            var existingSchedule = await _scheduleRepository.GetByLoanRequestIdAsync(loanRequestId);
            if (existingSchedule != null)
            {
                throw new InvalidOperationException("Loan request already has a schedule assigned");
            }

            // Validate that driver and vehicle are already assigned in loan request
            if (!loanRequest.DriverId.HasValue || !loanRequest.VehicleId.HasValue)
            {
                await _loanRequestRepository.UpdateStatusAsync(loanRequestId, LoanRequestStatus.WaitingResource);
                _logger.LogWarning(
                    "No driver or vehicle assigned for loan request {LoanRequestId}. Driver: {DriverId}, Vehicle: {VehicleId}",
                    loanRequestId, loanRequest.DriverId, loanRequest.VehicleId);
                return null;
            }

            // Use driver and vehicle that were assigned during approval (L1 or L2)
            var driverId = loanRequest.DriverId.Value;
            var vehicleId = loanRequest.VehicleId.Value;

            // Create schedule
            var schedule = new Schedule
            {
                LoanRequestId = loanRequestId,
                DriverId = driverId,
                VehicleId = vehicleId,
                Status = ScheduleStatus.Confirmed,
                Notes = "Jadwal perjalanan telah dikonfirmasi. Mohon untuk saling berkoordinasi untuk mengatur detail penjemputan."
            };

            var scheduleId = await _scheduleRepository.CreateAsync(schedule);
            schedule.Id = scheduleId;

            // Update loan request status to SCHEDULED
            await _loanRequestRepository.UpdateStatusAsync(loanRequestId, LoanRequestStatus.Scheduled);

            _logger.LogInformation(
                "Schedule created: LoanRequest={LoanRequestId}, Driver={DriverId}, Vehicle={VehicleId}",
                loanRequestId, driverId, vehicleId);

            return await GetByIdAsync(scheduleId);
        }

        /// <summary>
        /// Manually assigns a schedule (Admin override)
        /// </summary>
        public async Task<ScheduleDto> ManualScheduleAsync(int assignedBy, AssignScheduleDto dto)
        {
            var loanRequest = await _loanRequestRepository.GetByIdAsync(dto.LoanRequestId);
            if (loanRequest == null)
            {
                throw new ArgumentException("Loan request not found");
            }

            // Allow manual scheduling for APPROVED_L2 or WAITING_RESOURCE status
            if (loanRequest.Status != LoanRequestStatus.ApprovedL2 && 
                loanRequest.Status != LoanRequestStatus.WaitingResource)
            {
                throw new InvalidOperationException("Loan request must be in APPROVED_L2 or WAITING_RESOURCE status");
            }

            // Check if already scheduled
            var existingSchedule = await _scheduleRepository.GetByLoanRequestIdAsync(dto.LoanRequestId);
            if (existingSchedule != null)
            {
                throw new InvalidOperationException("Loan request already has a schedule assigned");
            }

            // Validate driver exists
            var driver = await _driverRepository.GetByIdAsync(dto.DriverId);
            if (driver == null)
            {
                throw new ArgumentException("Invalid driver");
            }

            // Validate vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(dto.VehicleId);
            if (vehicle == null)
            {
                throw new ArgumentException("Invalid vehicle");
            }

            // Create schedule
            var schedule = new Schedule
            {
                LoanRequestId = dto.LoanRequestId,
                DriverId = dto.DriverId,
                VehicleId = dto.VehicleId,
                Status = ScheduleStatus.Confirmed,
                Notes = dto.Notes ?? "Manually assigned by admin"
            };

            var scheduleId = await _scheduleRepository.CreateAsync(schedule);
            schedule.Id = scheduleId;

            // Update loan request status to SCHEDULED
            await _loanRequestRepository.UpdateStatusAsync(dto.LoanRequestId, LoanRequestStatus.Scheduled);

            _logger.LogInformation(
                "Manual schedule created: LoanRequest={LoanRequestId}, Driver={DriverId}, Vehicle={VehicleId}, AssignedBy={AssignedBy}",
                dto.LoanRequestId, dto.DriverId, dto.VehicleId, assignedBy);

            return (await GetByIdAsync(scheduleId))!;
        }

        public async Task<ScheduleDto?> GetByIdAsync(int id)
        {
            var schedules = await _scheduleRepository.GetAllAsync();
            var schedule = schedules.FirstOrDefault(s => s.Id == id);
            if (schedule == null) return null;

            return MapToDto(schedule);
        }

        public async Task<ScheduleDto?> GetByLoanRequestIdAsync(int loanRequestId)
        {
            var schedule = await _scheduleRepository.GetByLoanRequestIdAsync(loanRequestId);
            if (schedule == null) return null;

            var schedules = await _scheduleRepository.GetAllAsync();
            var fullSchedule = schedules.FirstOrDefault(s => s.LoanRequestId == loanRequestId);
            
            return fullSchedule != null ? MapToDto(fullSchedule) : null;
        }

        public async Task<IEnumerable<ScheduleDto>> GetAllAsync(string? status = null)
        {
            var schedules = await _scheduleRepository.GetAllAsync(status);
            return schedules.Select(MapToDto);
        }

        public async Task<IEnumerable<DriverScheduleDto>> GetDriverSchedulesAsync(int userId)
        {
            var schedules = await _scheduleRepository.GetByDriverUserIdAsync(userId);
            return schedules.Select(MapToDriverScheduleDto);
        }

        public async Task<IEnumerable<DriverScheduleDto>> GetUpcomingDriverSchedulesAsync(int userId)
        {
            // Get driver by user id
            var driver = await _driverRepository.GetByUserIdAsync(userId);
            if (driver == null)
            {
                return Enumerable.Empty<DriverScheduleDto>();
            }

            var schedules = await _scheduleRepository.GetUpcomingByDriverIdAsync(driver.Id);
            return schedules.Select(MapToDriverScheduleDto);
        }

        public async Task<bool> UpdateScheduleStatusAsync(int id, UpdateScheduleStatusDto dto)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(id);
            if (schedule == null) return false;

            schedule.Status = dto.Status;
            schedule.Notes = dto.Notes ?? schedule.Notes;

            var result = await _scheduleRepository.UpdateAsync(schedule);

            // If completed, update loan request status
            if (result && dto.Status == ScheduleStatus.Completed)
            {
                await _loanRequestRepository.UpdateStatusAsync(schedule.LoanRequestId, LoanRequestStatus.Completed);
            }
            // If in progress, update loan request status
            else if (result && dto.Status == ScheduleStatus.InProgress)
            {
                await _loanRequestRepository.UpdateStatusAsync(schedule.LoanRequestId, LoanRequestStatus.InProgress);
            }

            _logger.LogInformation("Schedule {ScheduleId} status updated to {Status}", id, dto.Status);

            return result;
        }

        public async Task<IEnumerable<ScheduleDto>> GetWaitingResourceRequestsAsync()
        {
            var loanRequests = await _loanRequestRepository.GetByStatusAsync(LoanRequestStatus.WaitingResource);
            
            return loanRequests.Select(lr => new ScheduleDto
            {
                LoanRequestId = lr.Id,
                LoanRequest = new LoanRequestDto
                {
                    Id = lr.Id,
                    RequestNumber = lr.RequestNumber,
                    Purpose = lr.Purpose,
                    Destination = lr.Destination,
                    GuestList = lr.GuestList,
                    HotelAccommodation = lr.HotelAccommodation,
                    VehicleId = lr.VehicleId,
                    DriverId = lr.DriverId,
                    StartDatetime = lr.StartDatetime,
                    EndDatetime = lr.EndDatetime,
                    Status = lr.Status,
                    User = lr.User != null ? new UserDto
                    {
                        Id = lr.User.Id,
                        Name = lr.User.Name,
                        Email = lr.User.Email
                    } : null
                }
            });
        }

        public async Task<bool> RetrySchedulingAsync(int loanRequestId)
        {
            var result = await AutoScheduleAsync(loanRequestId);
            return result != null;
        }

        public async Task<int> GetScheduledCountAsync()
        {
            return await _scheduleRepository.GetScheduledCountAsync();
        }

        private static ScheduleDto MapToDto(Schedule schedule)
        {
            return new ScheduleDto
            {
                Id = schedule.Id,
                LoanRequestId = schedule.LoanRequestId,
                DriverId = schedule.DriverId ?? 0,
                VehicleId = schedule.VehicleId ?? 0,
                AssignedAt = schedule.AssignedAt,
                Status = schedule.Status,
                Notes = schedule.Notes,
                LoanRequest = schedule.LoanRequest != null ? new LoanRequestDto
                {
                    Id = schedule.LoanRequest.Id,
                    RequestNumber = schedule.LoanRequest.RequestNumber,
                    Purpose = schedule.LoanRequest.Purpose,
                    Destination = schedule.LoanRequest.Destination,
                    GuestList = schedule.LoanRequest.GuestList,
                    HotelAccommodation = schedule.LoanRequest.HotelAccommodation,
                    VehicleId = schedule.LoanRequest.VehicleId,
                    DriverId = schedule.LoanRequest.DriverId,
                    StartDatetime = schedule.LoanRequest.StartDatetime,
                    EndDatetime = schedule.LoanRequest.EndDatetime,
                    Status = schedule.LoanRequest.Status,
                    User = schedule.LoanRequest.User != null ? new UserDto
                    {
                        Id = schedule.LoanRequest.User.Id,
                        Name = schedule.LoanRequest.User.Name,
                        Email = schedule.LoanRequest.User.Email
                    } : null
                } : null,
                Driver = schedule.Driver != null ? new DriverDto
                {
                    Id = schedule.Driver.Id,
                    DriverName = schedule.Driver.User?.Name,
                    LicenseNumber = schedule.Driver.LicenseNumber,
                    Status = schedule.Driver.Status,
                    ExperienceYears = schedule.Driver.ExperienceYears,
                    Rating = schedule.Driver.Rating
                } : null,
                Vehicle = schedule.Vehicle != null ? new VehicleDto
                {
                    Id = schedule.Vehicle.Id,
                    PlateNumber = schedule.Vehicle.PlateNumber,
                    Brand = schedule.Vehicle.Brand,
                    Type = schedule.Vehicle.Type,
                    Model = schedule.Vehicle.Model,
                    Capacity = schedule.Vehicle.Capacity,
                    Status = schedule.Vehicle.Status
                } : null
            };
        }

        private static DriverScheduleDto MapToDriverScheduleDto(Schedule schedule)
        {
            return new DriverScheduleDto
            {
                ScheduleId = schedule.Id,
                RequestNumber = schedule.LoanRequest?.RequestNumber ?? "",
                RequesterName = schedule.LoanRequest?.User?.Name ?? "Unknown",
                RequesterEmail = schedule.LoanRequest?.User?.Email ?? "",
                RequesterPhone = schedule.LoanRequest?.User?.PhoneNumber ?? "",
                Purpose = schedule.LoanRequest?.Purpose ?? "",
                Destination = schedule.LoanRequest?.Destination ?? "",
                GuestList = schedule.LoanRequest?.GuestList ?? "",
                HotelAccommodation = !string.IsNullOrWhiteSpace(schedule.LoanRequest?.HotelAccommodation),
                HotelName = schedule.LoanRequest?.HotelAccommodation,
                StartDatetime = schedule.LoanRequest?.StartDatetime ?? DateTime.MinValue,
                EndDatetime = schedule.LoanRequest?.EndDatetime ?? DateTime.MinValue,
                VehiclePlate = schedule.Vehicle?.PlateNumber ?? "",
                VehicleBrand = schedule.Vehicle?.Brand ?? "",
                VehicleType = schedule.Vehicle?.Type ?? "",
                Status = schedule.Status,
                Notes = schedule.Notes
            };
        }

        public async Task<bool> CancelScheduleAsync(int scheduleId, int userId, string cancellationReason)
        {
            // Get schedule with loan request to verify ownership
            var schedule = await _scheduleRepository.GetByIdWithDetailsAsync(scheduleId);
            if (schedule == null) return false;

            // Verify that the user is the owner of the loan request
            if (schedule.LoanRequest?.UserId != userId) return false;

            // Only allow cancellation if schedule is CONFIRMED
            if (schedule.Status != ScheduleStatus.Confirmed) return false;

            // Cancel the schedule with reason
            var result = await _scheduleRepository.CancelScheduleAsync(scheduleId, $"Dibatalkan oleh pemohon: {cancellationReason}");
            
            if (result)
            {
                // Update loan request status back to APPROVED (assuming it was fully approved before scheduling)
                await _loanRequestRepository.UpdateStatusAsync(schedule.LoanRequestId, "APPROVED");
            }

            return result;
        }
    }
}
