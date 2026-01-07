using Dapper;
using PelindoCarLoan.API.Models;

namespace PelindoCarLoan.API.Repositories
{
    /// <summary>
    /// Repository interface for Approval operations
    /// </summary>
    public interface IApprovalRepository
    {
        Task<Approval?> GetByIdAsync(int id);
        Task<IEnumerable<Approval>> GetByLoanRequestIdAsync(int loanRequestId);
        Task<Approval?> GetByLoanRequestAndLevelAsync(int loanRequestId, int level);
        Task<int> CreateAsync(Approval approval);
        Task<bool> UpdateAsync(Approval approval);
        Task<int> GetPendingCountByLevelAsync(int level);
    }

    public class ApprovalRepository : IApprovalRepository
    {
        private readonly IDbContext _dbContext;

        public ApprovalRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Approval?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT a.id, a.loan_request_id AS LoanRequestId, a.approver_id AS ApproverId,
                       a.approval_level AS ApprovalLevel, a.status, a.notes, 
                       a.approved_at AS ApprovedAt,
                       u.id, u.name, u.email, u.role, u.division
                FROM approvals a
                INNER JOIN users u ON a.approver_id = u.id
                WHERE a.id = :Id";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Approval, User, Approval>(
                sql,
                (a, u) =>
                {
                    a.Approver = u;
                    return a;
                },
                new { Id = id },
                splitOn: "id"
            );

            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<Approval>> GetByLoanRequestIdAsync(int loanRequestId)
        {
            const string sql = @"
                SELECT a.id, a.loan_request_id AS LoanRequestId, a.approver_id AS ApproverId,
                       a.approval_level AS ApprovalLevel, a.status, a.notes, 
                       a.approved_at AS ApprovedAt,
                       u.id, u.name, u.email, u.role, u.division
                FROM approvals a
                INNER JOIN users u ON a.approver_id = u.id
                WHERE a.loan_request_id = :LoanRequestId
                ORDER BY a.approval_level";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Approval, User, Approval>(
                sql,
                (a, u) =>
                {
                    a.Approver = u;
                    return a;
                },
                new { LoanRequestId = loanRequestId },
                splitOn: "id"
            );

            return result;
        }

        public async Task<Approval?> GetByLoanRequestAndLevelAsync(int loanRequestId, int level)
        {
            const string sql = @"
                SELECT id, loan_request_id AS LoanRequestId, approver_id AS ApproverId,
                       approval_level AS ApprovalLevel, status, notes, approved_at AS ApprovedAt
                FROM approvals
                WHERE loan_request_id = :LoanRequestId AND approval_level = :Level";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Approval>(sql, 
                new { LoanRequestId = loanRequestId, Level = level });
        }

        public async Task<int> CreateAsync(Approval approval)
        {
            const string sql = @"
                INSERT INTO approvals (loan_request_id, approver_id, approval_level, status, notes)
                VALUES (:LoanRequestId, :ApproverId, :ApprovalLevel, :Status, :Notes)
                RETURNING id INTO :Id";

            using var connection = _dbContext.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("LoanRequestId", approval.LoanRequestId);
            parameters.Add("ApproverId", approval.ApproverId);
            parameters.Add("ApprovalLevel", approval.ApprovalLevel);
            parameters.Add("Status", approval.Status);
            parameters.Add("Notes", approval.Notes);
            parameters.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

            await connection.ExecuteAsync(sql, parameters);
            return parameters.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(Approval approval)
        {
            const string sql = @"
                UPDATE approvals
                SET status = :Status, notes = :Notes
                WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                approval.Id,
                approval.Status,
                approval.Notes
            });
            return rowsAffected > 0;
        }

        public async Task<int> GetPendingCountByLevelAsync(int level)
        {
            var status = level == 1 ? LoanRequestStatus.Submitted : LoanRequestStatus.ApprovedL1;
            const string sql = "SELECT COUNT(1) FROM loan_requests WHERE status = :Status";

            using var connection = _dbContext.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { Status = status });
        }
    }
}
