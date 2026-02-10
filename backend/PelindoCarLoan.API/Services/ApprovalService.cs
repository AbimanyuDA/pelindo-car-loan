using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Models;
using PelindoCarLoan.API.Repositories;

namespace PelindoCarLoan.API.Services
{
    /// <summary>
    /// Service interface for approval operations
    /// </summary>
    public interface IApprovalService
    {
        Task<IEnumerable<PendingApprovalDto>> GetPendingApprovalsAsync(int level);
        Task<IEnumerable<PendingApprovalDto>> GetEmergencyApprovalsAsync(int level);
        Task<ApprovalDto> ProcessApprovalL1Async(int approverId, ProcessApprovalDto dto);
        Task<ApprovalDto> ProcessApprovalL2Async(int approverId, ProcessApprovalDto dto);
        Task<IEnumerable<ApprovalDto>> GetApprovalHistoryAsync(int loanRequestId);
        Task<int> GetPendingCountAsync(int level);
    }

    public class ApprovalService : IApprovalService
    {
        private readonly IApprovalRepository _approvalRepository;
        private readonly ILoanRequestRepository _loanRequestRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly ISchedulingService _schedulingService;
        private readonly IUserRepository _userRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalService(
            IApprovalRepository approvalRepository,
            ILoanRequestRepository loanRequestRepository,
            IScheduleRepository scheduleRepository,
            ISchedulingService schedulingService,
            IUserRepository userRepository,
            IDriverRepository driverRepository,
            IVehicleRepository vehicleRepository,
            IEmailService emailService,
            ILogger<ApprovalService> logger)
        {
            _approvalRepository = approvalRepository;
            _loanRequestRepository = loanRequestRepository;
            _scheduleRepository = scheduleRepository;
            _schedulingService = schedulingService;
            _userRepository = userRepository;
            _driverRepository = driverRepository;
            _vehicleRepository = vehicleRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<IEnumerable<PendingApprovalDto>> GetPendingApprovalsAsync(int level)
        {
            var loanRequests = await _loanRequestRepository.GetPendingForApprovalAsync(level);

            return loanRequests.Select(lr => new PendingApprovalDto
            {
                LoanRequestId = lr.Id,
                RequestNumber = lr.RequestNumber,
                RequesterName = lr.User?.Name ?? "Unknown",
                RequesterEmail = lr.User?.Email ?? "",
                RequesterPhone = lr.User?.PhoneNumber,
                RequesterDivision = lr.User?.Division ?? "Unknown",
                RequesterUnitKerja = lr.User?.UnitKerja,
                ServiceLetterBasis = lr.ServiceLetterBasis,
                ServiceLetterFilePath = lr.ServiceLetterFilePath,
                Purpose = lr.Purpose,
                Destination = lr.Destination,
                GuestList = lr.GuestList,
                HotelAccommodation = lr.HotelAccommodation,
                VehicleId = lr.VehicleId,
                DriverId = lr.DriverId,
                DriverName = lr.Driver?.User?.Name,
                DriverPhone = lr.Driver?.User?.PhoneNumber,
                StartDatetime = lr.StartDatetime,
                EndDatetime = lr.EndDatetime,
                Status = lr.Status,
                Notes = lr.Notes,
                CreatedAt = lr.CreatedAt,
                RequiredApprovalLevel = level
            });
        }

        public async Task<IEnumerable<PendingApprovalDto>> GetEmergencyApprovalsAsync(int level)
        {
            var loanRequests = await _loanRequestRepository.GetEmergencyForApprovalAsync(level);

            return loanRequests.Select(lr => new PendingApprovalDto
            {
                LoanRequestId = lr.Id,
                RequestNumber = lr.RequestNumber,
                RequesterName = lr.User?.Name ?? "Unknown",
                RequesterEmail = lr.User?.Email ?? "",
                RequesterPhone = lr.User?.PhoneNumber,
                RequesterDivision = lr.User?.Division ?? "Unknown",
                RequesterUnitKerja = lr.User?.UnitKerja,
                ServiceLetterBasis = lr.ServiceLetterBasis,
                ServiceLetterFilePath = lr.ServiceLetterFilePath,
                Purpose = lr.Purpose,
                Destination = lr.Destination,
                GuestList = lr.GuestList,
                HotelAccommodation = lr.HotelAccommodation,
                VehicleId = lr.VehicleId,
                DriverId = lr.DriverId,
                DriverName = lr.Driver?.User?.Name,
                DriverPhone = lr.Driver?.User?.PhoneNumber,
                StartDatetime = lr.StartDatetime,
                EndDatetime = lr.EndDatetime,
                Status = lr.Status,
                Notes = lr.Notes,
                CreatedAt = lr.CreatedAt,
                RequiredApprovalLevel = level,
                EmergencyReason = lr.Schedule?.EmergencyReason,
                EmergencyType = lr.Schedule?.EmergencyType
            });
        }

        public async Task<ApprovalDto> ProcessApprovalL1Async(int approverId, ProcessApprovalDto dto)
        {
            return await ProcessApprovalAsync(approverId, dto, ApprovalLevel.Level1);
        }

        public async Task<ApprovalDto> ProcessApprovalL2Async(int approverId, ProcessApprovalDto dto)
        {
            var approval = await ProcessApprovalAsync(approverId, dto, ApprovalLevel.Level2);

            // If approved at L2, trigger automatic scheduling
            if (dto.Status == ApprovalStatus.Approved)
            {
                try
                {
                    await _schedulingService.AutoScheduleAsync(dto.LoanRequestId);
                    _logger.LogInformation("Auto-scheduling triggered for loan request {LoanRequestId}", dto.LoanRequestId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auto-scheduling failed for loan request {LoanRequestId}", dto.LoanRequestId);
                    // Don't throw - the approval was successful, scheduling will need manual intervention
                }
            }

            return approval;
        }

        private async Task<ApprovalDto> ProcessApprovalAsync(int approverId, ProcessApprovalDto dto, int level)
        {
            // Validate loan request exists
            var loanRequest = await _loanRequestRepository.GetByIdAsync(dto.LoanRequestId);
            if (loanRequest == null)
            {
                throw new ArgumentException("Loan request not found");
            }

            // Get latest schedule to check if it's an emergency case
            var schedules = await _scheduleRepository.GetAllByLoanRequestIdAsync(dto.LoanRequestId);
            var latestSchedule = schedules?.OrderByDescending(s => s.Id).FirstOrDefault();
            var emergencyType = latestSchedule?.EmergencyType;
            var isEmergencyCase = latestSchedule?.Status == ScheduleStatus.Waiting ||
                                  latestSchedule?.Status == ScheduleStatus.WaitingL2 ||
                                  (latestSchedule?.Status == ScheduleStatus.Cancelled &&
                                   !string.IsNullOrEmpty(latestSchedule?.EmergencyReason) &&
                                   emergencyType == "MOGOK");

            // Validate loan request is in correct status for this approval level
            var expectedStatus = level == 1 ? LoanRequestStatus.Submitted : LoanRequestStatus.ApprovedL1;
            if (loanRequest.Status != expectedStatus)
            {
                if (level == 2 && isEmergencyCase &&
                    (latestSchedule?.Status == ScheduleStatus.WaitingL2 ||
                     (latestSchedule?.Status == ScheduleStatus.Cancelled && emergencyType == "MOGOK")))
                {
                    // Legacy emergency mogok data might still be SUBMITTED; normalize to APPROVED_L1
                    await _loanRequestRepository.UpdateStatusAsync(dto.LoanRequestId, LoanRequestStatus.ApprovedL1);
                    loanRequest.Status = LoanRequestStatus.ApprovedL1;
                }
                else
                {
                    throw new InvalidOperationException($"Loan request is not in {expectedStatus} status");
                }
            }

            // Validate status value
            if (dto.Status != ApprovalStatus.Approved && dto.Status != ApprovalStatus.Rejected)
            {
                throw new ArgumentException("Invalid approval status. Must be APPROVED or REJECTED");
            }

            // Check if approval already exists for this level
            var existingApproval = await _approvalRepository.GetByLoanRequestAndLevelAsync(dto.LoanRequestId, level);
            Approval approval;
            if (existingApproval != null)
            {
                existingApproval.Status = dto.Status;
                existingApproval.Notes = dto.Notes;
                existingApproval.ApproverId = approverId;
                existingApproval.ApprovedAt = DateTime.UtcNow;
                await _approvalRepository.UpdateAsync(existingApproval);
                approval = existingApproval;
            }
            else
            {
                // Create approval record
                approval = new Approval
                {
                    LoanRequestId = dto.LoanRequestId,
                    ApproverId = approverId,
                    ApprovalLevel = level,
                    Status = dto.Status,
                    Notes = dto.Notes,
                    ApprovedAt = DateTime.UtcNow
                };

                var approvalId = await _approvalRepository.CreateAsync(approval);
                approval.Id = approvalId;
            }

            // Handle emergency cases
            if (isEmergencyCase && dto.Status == ApprovalStatus.Approved)
            {
                if (level == 1)
                {
                    // L1 Emergency Logic
                    if (emergencyType == "MOGOK")
                    {
                        // Mogok: Assign vehicle/driver and set status to WAITING_L2
                        if (!dto.VehicleId.HasValue || !dto.DriverId.HasValue)
                        {
                            throw new InvalidOperationException("Mogok emergency requires vehicle and driver reassignment");
                        }

                        loanRequest.VehicleId = dto.VehicleId;
                        loanRequest.DriverId = dto.DriverId;
                        loanRequest.Status = LoanRequestStatus.ApprovedL1;
                        await _loanRequestRepository.UpdateAsync(loanRequest);

                        // Update schedule status to WAITING_L2
                        if (latestSchedule != null)
                        {
                            latestSchedule.Status = ScheduleStatus.WaitingL2;
                            await _scheduleRepository.UpdateAsync(latestSchedule);
                        }

                        _logger.LogInformation(
                            "Mogok emergency approved by L1: LoanRequest={LoanRequestId}, NewVehicle={VehicleId}, NewDriver={DriverId}, Status=WAITING_L2",
                            dto.LoanRequestId, dto.VehicleId, dto.DriverId);
                    }
                    else if (emergencyType == "LAINNYA")
                    {
                        // Alasan Lain:
                        // L1 harus assign kendaraan/driver lalu lanjut ke L2 (status WAITING_L2)

                        if (!dto.VehicleId.HasValue || !dto.DriverId.HasValue)
                        {
                            throw new InvalidOperationException("Alasan lain requires vehicle and driver assignment");
                        }

                        loanRequest.VehicleId = dto.VehicleId;
                        loanRequest.DriverId = dto.DriverId;
                        await _loanRequestRepository.UpdateAsync(loanRequest);

                        // Update schedule with new assignment and set to WAITING_L2 (for L2 review)
                        if (latestSchedule != null)
                        {
                            latestSchedule.VehicleId = dto.VehicleId.Value;
                            latestSchedule.DriverId = dto.DriverId.Value;
                            latestSchedule.AssignedAt = DateTime.UtcNow;
                            latestSchedule.Status = ScheduleStatus.WaitingL2;
                            await _scheduleRepository.UpdateAsync(latestSchedule);
                        }

                        // Update loan request status to APPROVED_L1 (for L2 review)
                        await _loanRequestRepository.UpdateStatusAsync(dto.LoanRequestId, LoanRequestStatus.ApprovedL1);
                        _logger.LogInformation(
                            "Alasan lain emergency processed by L1: LoanRequest={LoanRequestId}, NewVehicle={VehicleId}, NewDriver={DriverId}, Status=WAITING_L2",
                            dto.LoanRequestId, dto.VehicleId, dto.DriverId);

                        return MapToApprovalDto(approval);
                    }
                }
                else if (level == 2)
                {
                    // L2 Emergency Logic
                    if (latestSchedule?.Status == ScheduleStatus.WaitingL2)
                    {
                        // Mogok case: Check if driver changed
                        var oldDriverId = latestSchedule.DriverId;
                        var newDriverId = dto.DriverId;

                        if (newDriverId.HasValue && newDriverId.Value != oldDriverId)
                        {
                            // Driver changed: update existing schedule to new driver/vehicle
                            latestSchedule.DriverId = newDriverId.Value;
                            if (dto.VehicleId.HasValue)
                            {
                                latestSchedule.VehicleId = dto.VehicleId.Value;
                            }
                            latestSchedule.Status = ScheduleStatus.Confirmed;
                            latestSchedule.AssignedAt = DateTime.UtcNow;
                            await _scheduleRepository.UpdateAsync(latestSchedule);

                            // Update loan request
                            loanRequest.VehicleId = latestSchedule.VehicleId;
                            loanRequest.DriverId = newDriverId.Value;
                            loanRequest.Status = LoanRequestStatus.Scheduled;
                            await _loanRequestRepository.UpdateAsync(loanRequest);

                            _logger.LogInformation(
                                "L2 approved mogok with driver change: LoanRequest={LoanRequestId}, OldDriver={OldDriverId}, NewDriver={NewDriverId}",
                                dto.LoanRequestId, oldDriverId, newDriverId);
                        }
                        else
                        {
                            // Driver same or no change: keep L1 assignment, allow vehicle update, and confirm schedule
                            if (dto.VehicleId.HasValue)
                            {
                                latestSchedule.VehicleId = dto.VehicleId.Value;
                            }
                            latestSchedule.Status = ScheduleStatus.Confirmed;
                            await _scheduleRepository.UpdateAsync(latestSchedule);

                            loanRequest.VehicleId = latestSchedule.VehicleId;
                            loanRequest.DriverId = latestSchedule.DriverId;
                            loanRequest.Status = LoanRequestStatus.Scheduled;
                            await _loanRequestRepository.UpdateAsync(loanRequest);

                            _logger.LogInformation(
                                "L2 approved mogok with same driver: LoanRequest={LoanRequestId}, ConfirmedAsIs",
                                dto.LoanRequestId);
                        }
                    }
                    else if (latestSchedule?.Status == ScheduleStatus.Cancelled && emergencyType == "MOGOK")
                    {
                        // Legacy cancelled emergency mogok: revive assignment
                        if (dto.VehicleId.HasValue)
                        {
                            latestSchedule.VehicleId = dto.VehicleId.Value;
                        }
                        if (dto.DriverId.HasValue)
                        {
                            latestSchedule.DriverId = dto.DriverId.Value;
                        }

                        latestSchedule.Status = ScheduleStatus.Confirmed;
                        latestSchedule.AssignedAt = DateTime.UtcNow;
                        await _scheduleRepository.UpdateAsync(latestSchedule);

                        loanRequest.VehicleId = latestSchedule.VehicleId;
                        loanRequest.DriverId = latestSchedule.DriverId;
                        loanRequest.Status = LoanRequestStatus.Scheduled;
                        await _loanRequestRepository.UpdateAsync(loanRequest);

                        _logger.LogInformation(
                            "L2 revived cancelled mogok schedule: LoanRequest={LoanRequestId}, Driver={DriverId}, Vehicle={VehicleId}",
                            dto.LoanRequestId, latestSchedule.DriverId, latestSchedule.VehicleId);
                    }
                    else if (latestSchedule?.Status == ScheduleStatus.Waiting && emergencyType == "LAINNYA")
                    {
                        // Alasan Lain case: L2 tinggal approve (tidak boleh ganti driver)
                        // L2 hanya approve dari assignment L1
                        
                        // Approve: Set schedule to CONFIRMED
                        latestSchedule.Status = ScheduleStatus.Confirmed;
                        await _scheduleRepository.UpdateAsync(latestSchedule);

                        loanRequest.Status = LoanRequestStatus.Scheduled;
                        await _loanRequestRepository.UpdateAsync(loanRequest);

                        _logger.LogInformation(
                            "L2 approved alasan lain emergency: LoanRequest={LoanRequestId}, ApprovedAsIs",
                            dto.LoanRequestId);
                    }
                }
            }
            else if (!isEmergencyCase)
            {
                // Normal flow: L1 dan L2 bisa assign/reassign vehicle dan driver
                if (dto.VehicleId.HasValue && dto.DriverId.HasValue)
                {
                    loanRequest.VehicleId = dto.VehicleId;
                    loanRequest.DriverId = dto.DriverId;
                    await _loanRequestRepository.UpdateAsync(loanRequest);
                    _logger.LogInformation(
                        "Vehicle and Driver assigned/reassigned for LoanRequest {LoanRequestId}: Vehicle={VehicleId}, Driver={DriverId} by Approver={ApproverId}",
                        dto.LoanRequestId, dto.VehicleId, dto.DriverId, approverId);
                }

                // Validate that vehicle and driver are assigned before approving
                if (dto.Status == ApprovalStatus.Approved && (!loanRequest.VehicleId.HasValue || !loanRequest.DriverId.HasValue))
                {
                    throw new InvalidOperationException("Cannot approve request without vehicle and driver assignment");
                }

                // Update loan request status
                var newStatus = GetNewLoanRequestStatus(level, dto.Status);
                await _loanRequestRepository.UpdateStatusAsync(dto.LoanRequestId, newStatus);
            }

            _logger.LogInformation(
                "Approval processed: LoanRequest={LoanRequestId}, Level={Level}, Status={Status}, Approver={ApproverId}",
                dto.LoanRequestId, level, dto.Status, approverId);

            // Send email notifications
            _ = Task.Run(async () =>
            {
                try
                {
                    var requester = await _userRepository.GetByIdAsync(loanRequest.UserId);
                    if (requester == null || string.IsNullOrEmpty(requester.Email))
                        return;

                    if (level == ApprovalLevel.Level1)
                    {
                        if (dto.Status == ApprovalStatus.Approved)
                        {
                            // Email to requester: L1 approved, waiting L2
                            await _emailService.SendLoanRequestApprovedL1EmailAsync(
                                requester.Email,
                                requester.FullName,
                                loanRequest.RequestNumber
                            );

                            // Email to all L2 approvers
                            var l2Approvers = await _userRepository.GetByRoleAsync("PIC_APPROVAL_L2");
                            
                            // Get vehicle and driver details
                            var vehicle = await _vehicleRepository.GetByIdAsync(loanRequest.VehicleId!.Value);
                            var driver = await _driverRepository.GetByIdAsync(loanRequest.DriverId!.Value);
                            
                            // Get full loan request DTO
                            var loanRequestDto = new LoanRequestDto
                            {
                                Id = loanRequest.Id,
                                RequestNumber = loanRequest.RequestNumber,
                                RequesterName = requester.FullName,
                                RequesterEmail = requester.Email,
                                RequesterPhone = requester.PhoneNumber,
                                RequesterDivision = requester.Division,
                                RequesterUnitKerja = requester.UnitKerja,
                                ServiceLetterBasis = loanRequest.ServiceLetterBasis,
                                ServiceLetterFilePath = loanRequest.ServiceLetterFilePath,
                                Purpose = loanRequest.Purpose,
                                Destination = loanRequest.Destination,
                                GuestList = loanRequest.GuestList,
                                HotelAccommodation = loanRequest.HotelAccommodation,
                                StartDatetime = loanRequest.StartDatetime,
                                EndDatetime = loanRequest.EndDatetime,
                                Status = loanRequest.Status.ToString(),
                                CreatedAt = loanRequest.CreatedAt
                            };
                            
                            foreach (var approver in l2Approvers)
                            {
                                if (!string.IsNullOrEmpty(approver.Email))
                                {
                                    await _emailService.SendApprovalL1NotificationToL2Async(
                                        approver.Email,
                                        approver.FullName,
                                        loanRequestDto,
                                        vehicle?.PlateNumber ?? "-",
                                        vehicle?.Type ?? "-",
                                        driver?.User?.FullName ?? "-",
                                        driver?.User?.PhoneNumber ?? "-"
                                    );
                                }
                            }
                        }
                        else if (dto.Status == ApprovalStatus.Rejected)
                        {
                            // Email to requester: L1 rejected
                            await _emailService.SendLoanRequestRejectedL1EmailAsync(
                                requester.Email,
                                requester.FullName,
                                loanRequest.RequestNumber,
                                dto.Notes ?? ""
                            );
                        }
                    }
                    else if (level == ApprovalLevel.Level2)
                    {
                        if (dto.Status == ApprovalStatus.Approved)
                        {
                            // Get vehicle and driver details
                            var vehicle = await _vehicleRepository.GetByIdAsync(loanRequest.VehicleId!.Value);
                            var driver = await _driverRepository.GetByIdAsync(loanRequest.DriverId!.Value);
                            
                            // Build full loan request DTO
                            var loanRequestDto = new LoanRequestDto
                            {
                                Id = loanRequest.Id,
                                RequestNumber = loanRequest.RequestNumber,
                                RequesterName = requester.FullName,
                                RequesterEmail = requester.Email,
                                RequesterPhone = requester.PhoneNumber,
                                RequesterDivision = requester.Division,
                                RequesterUnitKerja = requester.UnitKerja,
                                ServiceLetterBasis = loanRequest.ServiceLetterBasis,
                                ServiceLetterFilePath = loanRequest.ServiceLetterFilePath,
                                Purpose = loanRequest.Purpose,
                                Destination = loanRequest.Destination,
                                GuestList = loanRequest.GuestList,
                                HotelAccommodation = loanRequest.HotelAccommodation,
                                StartDatetime = loanRequest.StartDatetime,
                                EndDatetime = loanRequest.EndDatetime,
                                Status = loanRequest.Status.ToString(),
                                CreatedAt = loanRequest.CreatedAt
                            };
                            
                            // Email to requester: L2 approved (final)
                            await _emailService.SendLoanRequestApprovedL2EmailAsync(
                                requester.Email,
                                requester.FullName,
                                loanRequestDto,
                                vehicle?.PlateNumber ?? "-",
                                vehicle?.Type ?? "-",
                                driver?.User?.FullName ?? "-",
                                driver?.User?.PhoneNumber ?? "-"
                            );

                            // Email to driver
                            if (loanRequest.DriverId.HasValue)
                            {
                                if (driver?.User != null && !string.IsNullOrEmpty(driver.User.Email))
                                {
                                    await _emailService.SendDriverAssignmentEmailAsync(
                                        driver.User.Email,
                                        driver.User.FullName,
                                        loanRequest.RequestNumber,
                                        requester.FullName,
                                        requester.PhoneNumber,
                                        loanRequestDto
                                    );
                                }
                            }
                        }
                        else if (dto.Status == ApprovalStatus.Rejected)
                        {
                            // Email to requester: L2 rejected
                            await _emailService.SendLoanRequestRejectedL2EmailAsync(
                                requester.Email,
                                requester.FullName,
                                loanRequest.RequestNumber,
                                dto.Notes ?? ""
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email notification for approval {ApprovalId}", approval.Id);
                }
            });

            return new ApprovalDto
            {
                Id = approval.Id,
                LoanRequestId = approval.LoanRequestId,
                ApproverId = approval.ApproverId,
                ApprovalLevel = approval.ApprovalLevel,
                Status = approval.Status,
                Notes = approval.Notes,
                ApprovedAt = approval.ApprovedAt
            };
        }

        private static string GetNewLoanRequestStatus(int level, string approvalStatus)
        {
            if (approvalStatus == ApprovalStatus.Rejected)
            {
                return level == 1 ? LoanRequestStatus.RejectedL1 : LoanRequestStatus.RejectedL2;
            }

            return level == 1 ? LoanRequestStatus.ApprovedL1 : LoanRequestStatus.ApprovedL2;
        }

        public async Task<IEnumerable<ApprovalDto>> GetApprovalHistoryAsync(int loanRequestId)
        {
            var approvals = await _approvalRepository.GetByLoanRequestIdAsync(loanRequestId);

            return approvals.Select(a => new ApprovalDto
            {
                Id = a.Id,
                LoanRequestId = a.LoanRequestId,
                ApproverId = a.ApproverId,
                ApprovalLevel = a.ApprovalLevel,
                Status = a.Status,
                Notes = a.Notes,
                ApprovedAt = a.ApprovedAt,
                ApproverName = a.Approver?.Name
            });
        }

        public async Task<int> GetPendingCountAsync(int level)
        {
            return await _approvalRepository.GetPendingCountByLevelAsync(level);
        }

        private ApprovalDto MapToApprovalDto(Approval approval)
        {
            return new ApprovalDto
            {
                Id = approval.Id,
                LoanRequestId = approval.LoanRequestId,
                ApproverId = approval.ApproverId,
                ApprovalLevel = approval.ApprovalLevel,
                Status = approval.Status,
                Notes = approval.Notes,
                ApprovedAt = approval.ApprovedAt
            };
        }
    }
}
