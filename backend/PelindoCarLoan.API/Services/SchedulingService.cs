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
        Task<bool> ReportEmergencyAsync(int scheduleId, int userId, EmergencyReportDto dto);
        Task<bool> DriverConfirmationAsync(int scheduleId, int userId, DriverConfirmationDto dto);
        Task<ScheduleDto?> StartJourneyAsync(int scheduleId, int userId, StartJourneyDto dto);
        Task<ScheduleDto?> CompleteJourneyAsync(int scheduleId, int userId, CompleteJourneyDto dto, string? refuelReceiptPath = null);
        Task<string?> UploadKmPhotoAsync(int scheduleId, int userId, IFormFile file);
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

            // Check for schedule conflicts
            var hasDriverConflict = await _scheduleRepository.HasDriverConflictAsync(
                driverId, 
                loanRequest.StartDatetime, 
                loanRequest.EndDatetime
            );

            if (hasDriverConflict)
            {
                await _loanRequestRepository.UpdateStatusAsync(loanRequestId, LoanRequestStatus.WaitingResource);
                _logger.LogWarning(
                    "Driver {DriverId} has schedule conflict for loan request {LoanRequestId} ({StartTime} - {EndTime})",
                    driverId, loanRequestId, loanRequest.StartDatetime, loanRequest.EndDatetime);
                throw new InvalidOperationException($"Driver sudah memiliki jadwal di rentang waktu tersebut. Silakan pilih driver lain atau ubah jadwal.");
            }

            var hasVehicleConflict = await _scheduleRepository.HasVehicleConflictAsync(
                vehicleId, 
                loanRequest.StartDatetime, 
                loanRequest.EndDatetime
            );

            if (hasVehicleConflict)
            {
                await _loanRequestRepository.UpdateStatusAsync(loanRequestId, LoanRequestStatus.WaitingResource);
                _logger.LogWarning(
                    "Vehicle {VehicleId} has schedule conflict for loan request {LoanRequestId} ({StartTime} - {EndTime})",
                    vehicleId, loanRequestId, loanRequest.StartDatetime, loanRequest.EndDatetime);
                throw new InvalidOperationException($"Kendaraan sudah dijadwalkan di rentang waktu tersebut. Silakan pilih kendaraan lain atau ubah jadwal.");
            }

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

            // Check for schedule conflicts
            var hasDriverConflict = await _scheduleRepository.HasDriverConflictAsync(
                dto.DriverId, 
                loanRequest.StartDatetime, 
                loanRequest.EndDatetime
            );

            if (hasDriverConflict)
            {
                _logger.LogWarning(
                    "Driver {DriverId} has schedule conflict for loan request {LoanRequestId} ({StartTime} - {EndTime})",
                    dto.DriverId, dto.LoanRequestId, loanRequest.StartDatetime, loanRequest.EndDatetime);
                throw new InvalidOperationException($"Driver sudah memiliki jadwal di rentang waktu {loanRequest.StartDatetime:dd/MM/yyyy HH:mm} - {loanRequest.EndDatetime:dd/MM/yyyy HH:mm}. Silakan pilih driver lain.");
            }

            var hasVehicleConflict = await _scheduleRepository.HasVehicleConflictAsync(
                dto.VehicleId, 
                loanRequest.StartDatetime, 
                loanRequest.EndDatetime
            );

            if (hasVehicleConflict)
            {
                _logger.LogWarning(
                    "Vehicle {VehicleId} has schedule conflict for loan request {LoanRequestId} ({StartTime} - {EndTime})",
                    dto.VehicleId, dto.LoanRequestId, loanRequest.StartDatetime, loanRequest.EndDatetime);
                throw new InvalidOperationException($"Kendaraan sudah dijadwalkan di rentang waktu {loanRequest.StartDatetime:dd/MM/yyyy HH:mm} - {loanRequest.EndDatetime:dd/MM/yyyy HH:mm}. Silakan pilih kendaraan lain.");
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
                VehicleId = schedule.VehicleId ?? 0,
                VehiclePlate = schedule.Vehicle?.PlateNumber ?? "",
                VehicleBrand = schedule.Vehicle?.Brand ?? "",
                VehicleModel = schedule.Vehicle?.Model ?? "",
                VehicleType = schedule.Vehicle?.Type ?? "",
                Status = schedule.Status,
                Notes = schedule.Notes,
                ActualVehicleId = schedule.ActualVehicleId,
                FuelCondition = schedule.FuelCondition,
                ActualStartTime = schedule.ActualStartTime,
                ActualEndTime = schedule.ActualEndTime,
                EmergencyReason = schedule.EmergencyReason
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
                // Update loan request status to CANCELLED
                await _loanRequestRepository.UpdateStatusAsync(schedule.LoanRequestId, LoanRequestStatus.Cancelled);
            }

            return result;
        }

        public async Task<bool> ReportEmergencyAsync(int scheduleId, int userId, EmergencyReportDto dto)
        {
            var schedule = await _scheduleRepository.GetByIdWithDetailsAsync(scheduleId);
            if (schedule == null) return false;

            // Verify driver owns this schedule
            var driver = await _driverRepository.GetByUserIdAsync(userId);
            if (driver == null || schedule.DriverId != driver.Id) return false;

            // Only allow emergency report if schedule is CONFIRMED
            if (schedule.Status != ScheduleStatus.Confirmed) return false;

            // Store emergency info in schedule
            schedule.EmergencyReason = dto.EmergencyReason;
            schedule.DriverMessage = dto.DriverMessage;
            
            // Detect emergency type - check for vehicle/mogok related keywords
            var reason = dto.EmergencyReason?.ToLower() ?? "";
            if (reason.Contains("[mogok]") || 
                reason.Contains("mogok") || 
                reason.Contains("mobil bermasalah") || 
                reason.Contains("kendaraan bermasalah") ||
                reason.Contains("ban pecah") ||
                reason.Contains("mesin mati") ||
                reason.Contains("kerusakan"))
            {
                schedule.EmergencyType = "MOGOK";
            }
            else
            {
                schedule.EmergencyType = "LAINNYA";
            }
            
            // Set status to WAITING (not immediately CANCELLED)
            schedule.Status = ScheduleStatus.Waiting;

            await _scheduleRepository.UpdateAsync(schedule);

            // Return loan request to SUBMITTED status (back to L1 approval for review)
            await _loanRequestRepository.UpdateStatusAsync(schedule.LoanRequestId, LoanRequestStatus.Submitted);

            return true;
        }

        public async Task<bool> DriverConfirmationAsync(int scheduleId, int userId, DriverConfirmationDto dto)
        {
            var schedule = await _scheduleRepository.GetByIdWithDetailsAsync(scheduleId);
            if (schedule == null) return false;

            // Verify driver owns this schedule
            var driver = await _driverRepository.GetByUserIdAsync(userId);
            if (driver == null || schedule.DriverId != driver.Id) return false;

            // Only allow confirmation if schedule is CONFIRMED
            if (schedule.Status != ScheduleStatus.Confirmed) return false;

            // Update with pre-departure data
            schedule.ActualVehicleId = dto.ActualVehicleId;
            schedule.FuelCondition = dto.FuelCondition;
            schedule.Status = ScheduleStatus.DriverConfirmed;

            await _scheduleRepository.UpdateAsync(schedule);

            return true;
        }

        public async Task<ScheduleDto?> StartJourneyAsync(int scheduleId, int userId, StartJourneyDto dto)
        {
            var schedule = await _scheduleRepository.GetByIdWithDetailsAsync(scheduleId);
            if (schedule == null) return null;

            // Verify driver owns this schedule
            var driver = await _driverRepository.GetByUserIdAsync(userId);
            if (driver == null || schedule.DriverId != driver.Id) return null;

            // Only allow start if schedule is DRIVER_CONFIRMED
            if (schedule.Status != ScheduleStatus.DriverConfirmed) return null;

            // Record actual start time
            schedule.ActualStartTime = dto.ActualStartTime;
            schedule.Status = ScheduleStatus.InProgress;

            await _scheduleRepository.UpdateAsync(schedule);

            // Update loan request status
            await _loanRequestRepository.UpdateStatusAsync(schedule.LoanRequestId, LoanRequestStatus.InProgress);

            return MapToDto(schedule);
        }

        public async Task<ScheduleDto?> CompleteJourneyAsync(int scheduleId, int userId, CompleteJourneyDto dto, string? refuelReceiptPath = null)
        {
            var schedule = await _scheduleRepository.GetByIdWithDetailsAsync(scheduleId);
            if (schedule == null) return null;

            // Verify driver owns this schedule
            var driver = await _driverRepository.GetByUserIdAsync(userId);
            if (driver == null || schedule.DriverId != driver.Id) return null;

            // Only allow complete if schedule is IN_PROGRESS
            if (schedule.Status != ScheduleStatus.InProgress) return null;

            // Update schedule with actual end time, final fuel condition, and refuel info
            schedule.ActualEndTime = dto.ActualEndTime;
            schedule.FinalFuelCondition = dto.FinalFuelCondition;
            schedule.IsRefueled = dto.IsRefueled;
            schedule.RefuelAmount = dto.RefuelAmount;
            schedule.RefuelReceiptPath = refuelReceiptPath;
            schedule.Status = ScheduleStatus.Completed;
            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                schedule.Notes = dto.Notes;
            }

            await _scheduleRepository.UpdateAsync(schedule);

            // Update loan request status
            await _loanRequestRepository.UpdateStatusAsync(schedule.LoanRequestId, LoanRequestStatus.Completed);

            return MapToDto(schedule);
        }

        public async Task<string?> UploadKmPhotoAsync(int scheduleId, int userId, IFormFile file)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
            if (schedule == null) return null;

            // Verify driver owns this schedule
            var driver = await _driverRepository.GetByUserIdAsync(userId);
            if (driver == null || schedule.DriverId != driver.Id) return null;

            // Only allow upload if schedule is CONFIRMED
            if (schedule.Status != ScheduleStatus.Confirmed) return null;

            // Save file (simplified - in production use proper storage)
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "km-photos");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{scheduleId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"km-photos/{uniqueFileName}";
            schedule.KmPhotoPath = relativePath;
            await _scheduleRepository.UpdateAsync(schedule);

            return relativePath;
        }
    }
}
