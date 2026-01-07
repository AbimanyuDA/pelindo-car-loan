using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Models;
using PelindoCarLoan.API.Repositories;

namespace PelindoCarLoan.API.Services
{
    /// <summary>
    /// Service interface for loan request operations
    /// </summary>
    public interface ILoanRequestService
    {
        Task<LoanRequestDto?> GetByIdAsync(int id);
        Task<IEnumerable<LoanRequestListDto>> GetAllAsync(int? userId = null, string? status = null);
        Task<IEnumerable<LoanRequestListDto>> GetMyRequestsAsync(int userId);
        Task<LoanRequestDto> CreateAsync(int userId, CreateLoanRequestDto dto);
        Task<LoanRequestDto?> UpdateAsync(int id, int userId, UpdateLoanRequestDto dto);
        Task<bool> CancelAsync(int id, int userId);
        Task<int> GetCountByStatusAsync(string status);
        Task<int> GetTotalCountAsync(int? userId = null);
    }

    public class LoanRequestService : ILoanRequestService
    {
        private readonly ILoanRequestRepository _loanRequestRepository;
        private readonly IApprovalRepository _approvalRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly ILogger<LoanRequestService> _logger;

        public LoanRequestService(
            ILoanRequestRepository loanRequestRepository,
            IApprovalRepository approvalRepository,
            IScheduleRepository scheduleRepository,
            ILogger<LoanRequestService> logger)
        {
            _loanRequestRepository = loanRequestRepository;
            _approvalRepository = approvalRepository;
            _scheduleRepository = scheduleRepository;
            _logger = logger;
        }

        public async Task<LoanRequestDto?> GetByIdAsync(int id)
        {
            var loanRequest = await _loanRequestRepository.GetByIdWithDetailsAsync(id);
            if (loanRequest == null) return null;

            var approvals = await _approvalRepository.GetByLoanRequestIdAsync(id);
            var schedule = await _scheduleRepository.GetByLoanRequestIdAsync(id);

            return MapToDto(loanRequest, approvals.ToList(), schedule);
        }

        public async Task<IEnumerable<LoanRequestListDto>> GetAllAsync(int? userId = null, string? status = null)
        {
            var loanRequests = await _loanRequestRepository.GetAllAsync(userId, status);
            return loanRequests.Select(MapToListDto);
        }

        public async Task<IEnumerable<LoanRequestListDto>> GetMyRequestsAsync(int userId)
        {
            var loanRequests = await _loanRequestRepository.GetAllAsync(userId: userId);
            return loanRequests.Select(MapToListDto);
        }

        public async Task<LoanRequestDto> CreateAsync(int userId, CreateLoanRequestDto dto)
        {
            // Validate dates
            if (dto.StartDatetime >= dto.EndDatetime)
            {
                throw new ArgumentException("End datetime must be after start datetime");
            }

            if (dto.StartDatetime < DateTime.Now.AddHours(1))
            {
                throw new ArgumentException("Start datetime must be at least 1 hour from now");
            }

            var requestNumber = await _loanRequestRepository.GenerateRequestNumberAsync();

            var loanRequest = new LoanRequest
            {
                UserId = userId,
                RequestNumber = requestNumber,
                Purpose = dto.Purpose,
                Destination = dto.Destination,
                PassengerCount = dto.PassengerCount,
                StartDatetime = dto.StartDatetime,
                EndDatetime = dto.EndDatetime,
                Status = LoanRequestStatus.Submitted,
                Notes = dto.Notes
            };

            var id = await _loanRequestRepository.CreateAsync(loanRequest);
            loanRequest.Id = id;

            _logger.LogInformation("Loan request created: {RequestNumber} by user {UserId}", requestNumber, userId);

            return (await GetByIdAsync(id))!;
        }

        public async Task<LoanRequestDto?> UpdateAsync(int id, int userId, UpdateLoanRequestDto dto)
        {
            var loanRequest = await _loanRequestRepository.GetByIdAsync(id);
            if (loanRequest == null) return null;

            // Only allow update if status is SUBMITTED and user owns the request
            if (loanRequest.Status != LoanRequestStatus.Submitted)
            {
                throw new InvalidOperationException("Can only update requests with SUBMITTED status");
            }

            if (loanRequest.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only update your own requests");
            }

            // Validate dates
            if (dto.StartDatetime >= dto.EndDatetime)
            {
                throw new ArgumentException("End datetime must be after start datetime");
            }

            loanRequest.Purpose = dto.Purpose;
            loanRequest.Destination = dto.Destination;
            loanRequest.PassengerCount = dto.PassengerCount;
            loanRequest.StartDatetime = dto.StartDatetime;
            loanRequest.EndDatetime = dto.EndDatetime;
            loanRequest.Notes = dto.Notes;

            await _loanRequestRepository.UpdateAsync(loanRequest);

            _logger.LogInformation("Loan request updated: {RequestNumber}", loanRequest.RequestNumber);

            return await GetByIdAsync(id);
        }

        public async Task<bool> CancelAsync(int id, int userId)
        {
            var loanRequest = await _loanRequestRepository.GetByIdAsync(id);
            if (loanRequest == null) return false;

            // Only allow cancellation if status allows and user owns the request
            if (loanRequest.Status != LoanRequestStatus.Submitted && 
                loanRequest.Status != LoanRequestStatus.ApprovedL1)
            {
                throw new InvalidOperationException("Can only cancel requests with SUBMITTED or APPROVED_L1 status");
            }

            if (loanRequest.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only cancel your own requests");
            }

            await _loanRequestRepository.UpdateStatusAsync(id, LoanRequestStatus.Cancelled);

            _logger.LogInformation("Loan request cancelled: {RequestNumber} by user {UserId}", 
                loanRequest.RequestNumber, userId);

            return true;
        }

        public async Task<int> GetCountByStatusAsync(string status)
        {
            return await _loanRequestRepository.GetCountByStatusAsync(status);
        }

        public async Task<int> GetTotalCountAsync(int? userId = null)
        {
            return await _loanRequestRepository.GetTotalCountAsync(userId);
        }

        private static LoanRequestDto MapToDto(LoanRequest lr, List<Approval>? approvals, Schedule? schedule)
        {
            return new LoanRequestDto
            {
                Id = lr.Id,
                UserId = lr.UserId,
                RequestNumber = lr.RequestNumber,
                Purpose = lr.Purpose,
                Destination = lr.Destination,
                PassengerCount = lr.PassengerCount,
                StartDatetime = lr.StartDatetime,
                EndDatetime = lr.EndDatetime,
                Status = lr.Status,
                Notes = lr.Notes,
                CreatedAt = lr.CreatedAt,
                UpdatedAt = lr.UpdatedAt,
                User = lr.User != null ? new UserDto
                {
                    Id = lr.User.Id,
                    Name = lr.User.Name,
                    Email = lr.User.Email,
                    Role = lr.User.Role,
                    Division = lr.User.Division
                } : null,
                Approvals = approvals?.Select(a => new ApprovalDto
                {
                    Id = a.Id,
                    LoanRequestId = a.LoanRequestId,
                    ApproverId = a.ApproverId,
                    ApprovalLevel = a.ApprovalLevel,
                    Status = a.Status,
                    Notes = a.Notes,
                    ApprovedAt = a.ApprovedAt,
                    ApproverName = a.Approver?.Name
                }).ToList(),
                Schedule = schedule != null ? new ScheduleDto
                {
                    Id = schedule.Id,
                    LoanRequestId = schedule.LoanRequestId,
                    DriverId = schedule.DriverId,
                    VehicleId = schedule.VehicleId,
                    AssignedBy = schedule.AssignedBy,
                    AssignedAt = schedule.AssignedAt,
                    Status = schedule.Status,
                    Notes = schedule.Notes
                } : null
            };
        }

        private static LoanRequestListDto MapToListDto(LoanRequest lr)
        {
            return new LoanRequestListDto
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
            };
        }
    }
}
