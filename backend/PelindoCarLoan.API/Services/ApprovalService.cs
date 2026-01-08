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
        Task<ApprovalDto> ProcessApprovalL1Async(int approverId, ProcessApprovalDto dto);
        Task<ApprovalDto> ProcessApprovalL2Async(int approverId, ProcessApprovalDto dto);
        Task<IEnumerable<ApprovalDto>> GetApprovalHistoryAsync(int loanRequestId);
        Task<int> GetPendingCountAsync(int level);
    }

    public class ApprovalService : IApprovalService
    {
        private readonly IApprovalRepository _approvalRepository;
        private readonly ILoanRequestRepository _loanRequestRepository;
        private readonly ISchedulingService _schedulingService;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalService(
            IApprovalRepository approvalRepository,
            ILoanRequestRepository loanRequestRepository,
            ISchedulingService schedulingService,
            ILogger<ApprovalService> logger)
        {
            _approvalRepository = approvalRepository;
            _loanRequestRepository = loanRequestRepository;
            _schedulingService = schedulingService;
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

            // Validate loan request is in correct status for this approval level
            var expectedStatus = level == 1 ? LoanRequestStatus.Submitted : LoanRequestStatus.ApprovedL1;
            if (loanRequest.Status != expectedStatus)
            {
                throw new InvalidOperationException($"Loan request is not in {expectedStatus} status");
            }

            // Validate status value
            if (dto.Status != ApprovalStatus.Approved && dto.Status != ApprovalStatus.Rejected)
            {
                throw new ArgumentException("Invalid approval status. Must be APPROVED or REJECTED");
            }

            // Check if approval already exists for this level
            var existingApproval = await _approvalRepository.GetByLoanRequestAndLevelAsync(dto.LoanRequestId, level);
            if (existingApproval != null)
            {
                throw new InvalidOperationException($"Approval for level {level} already exists");
            }

            // L1 dan L2 bisa assign/reassign vehicle dan driver
            // Jika approver memberikan vehicle/driver baru, update loan request
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

            // Create approval record
            var approval = new Approval
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

            // Update loan request status
            var newStatus = GetNewLoanRequestStatus(level, dto.Status);
            await _loanRequestRepository.UpdateStatusAsync(dto.LoanRequestId, newStatus);

            _logger.LogInformation(
                "Approval processed: LoanRequest={LoanRequestId}, Level={Level}, Status={Status}, Approver={ApproverId}",
                dto.LoanRequestId, level, dto.Status, approverId);

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
    }
}
