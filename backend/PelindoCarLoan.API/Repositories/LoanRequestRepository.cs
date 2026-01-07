using Dapper;
using PelindoCarLoan.API.Models;

namespace PelindoCarLoan.API.Repositories
{
    /// <summary>
    /// Repository interface for LoanRequest operations
    /// </summary>
    public interface ILoanRequestRepository
    {
        Task<LoanRequest?> GetByIdAsync(int id);
        Task<LoanRequest?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<LoanRequest>> GetAllAsync(int? userId = null, string? status = null);
        Task<IEnumerable<LoanRequest>> GetPendingForApprovalAsync(int approvalLevel);
        Task<IEnumerable<LoanRequest>> GetByStatusAsync(string status);
        Task<int> CreateAsync(LoanRequest loanRequest);
        Task<bool> UpdateAsync(LoanRequest loanRequest);
        Task<bool> UpdateStatusAsync(int id, string status);
        Task<bool> DeleteAsync(int id);
        Task<string> GenerateRequestNumberAsync();
        Task<int> GetCountByStatusAsync(string status);
        Task<int> GetTotalCountAsync(int? userId = null);
    }

    public class LoanRequestRepository : ILoanRequestRepository
    {
        private readonly IDbContext _dbContext;

        public LoanRequestRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<LoanRequest?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT id, user_id AS UserId, request_number AS RequestNumber, purpose, destination,
                       passenger_count AS PassengerCount, start_datetime AS StartDatetime, 
                       end_datetime AS EndDatetime, status, notes, 
                       created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM loan_requests
                WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<LoanRequest>(sql, new { Id = id });
        }

        public async Task<LoanRequest?> GetByIdWithDetailsAsync(int id)
        {
            const string sql = @"
                SELECT lr.id, lr.user_id AS UserId, lr.request_number AS RequestNumber, lr.purpose, 
                       lr.destination, lr.passenger_count AS PassengerCount, 
                       lr.start_datetime AS StartDatetime, lr.end_datetime AS EndDatetime, 
                       lr.status, lr.notes, lr.created_at AS CreatedAt, lr.updated_at AS UpdatedAt,
                       u.id, u.name, u.email, u.role, u.division
                FROM loan_requests lr
                INNER JOIN users u ON lr.user_id = u.id
                WHERE lr.id = :Id";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<LoanRequest, User, LoanRequest>(
                sql,
                (lr, u) =>
                {
                    lr.User = u;
                    return lr;
                },
                new { Id = id },
                splitOn: "id"
            );

            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<LoanRequest>> GetAllAsync(int? userId = null, string? status = null)
        {
            var sql = @"
                SELECT lr.id, lr.user_id AS UserId, lr.request_number AS RequestNumber, lr.purpose, 
                       lr.destination, lr.passenger_count AS PassengerCount, 
                       lr.start_datetime AS StartDatetime, lr.end_datetime AS EndDatetime, 
                       lr.status, lr.notes, lr.created_at AS CreatedAt, lr.updated_at AS UpdatedAt,
                       u.id, u.name, u.email, u.role, u.division
                FROM loan_requests lr
                INNER JOIN users u ON lr.user_id = u.id
                WHERE 1=1";

            if (userId.HasValue)
                sql += " AND lr.user_id = :UserId";
            if (!string.IsNullOrEmpty(status))
                sql += " AND lr.status = :Status";

            sql += " ORDER BY lr.created_at DESC";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<LoanRequest, User, LoanRequest>(
                sql,
                (lr, u) =>
                {
                    lr.User = u;
                    return lr;
                },
                new { UserId = userId, Status = status },
                splitOn: "id"
            );

            return result;
        }

        public async Task<IEnumerable<LoanRequest>> GetPendingForApprovalAsync(int approvalLevel)
        {
            var status = approvalLevel == 1 ? LoanRequestStatus.Submitted : LoanRequestStatus.ApprovedL1;

            const string sql = @"
                SELECT lr.id, lr.user_id AS UserId, lr.request_number AS RequestNumber, lr.purpose, 
                       lr.destination, lr.passenger_count AS PassengerCount, 
                       lr.start_datetime AS StartDatetime, lr.end_datetime AS EndDatetime, 
                       lr.status, lr.notes, lr.created_at AS CreatedAt, lr.updated_at AS UpdatedAt,
                       u.id, u.name, u.email, u.role, u.division
                FROM loan_requests lr
                INNER JOIN users u ON lr.user_id = u.id
                WHERE lr.status = :Status
                ORDER BY lr.created_at ASC";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<LoanRequest, User, LoanRequest>(
                sql,
                (lr, u) =>
                {
                    lr.User = u;
                    return lr;
                },
                new { Status = status },
                splitOn: "id"
            );

            return result;
        }

        public async Task<IEnumerable<LoanRequest>> GetByStatusAsync(string status)
        {
            return await GetAllAsync(status: status);
        }

        public async Task<int> CreateAsync(LoanRequest loanRequest)
        {
            const string sql = @"
                INSERT INTO loan_requests (user_id, request_number, purpose, destination, 
                                          passenger_count, start_datetime, end_datetime, status, notes)
                VALUES (:UserId, :RequestNumber, :Purpose, :Destination, 
                        :PassengerCount, :StartDatetime, :EndDatetime, :Status, :Notes)
                RETURNING id INTO :Id";

            using var connection = _dbContext.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", loanRequest.UserId);
            parameters.Add("RequestNumber", loanRequest.RequestNumber);
            parameters.Add("Purpose", loanRequest.Purpose);
            parameters.Add("Destination", loanRequest.Destination);
            parameters.Add("PassengerCount", loanRequest.PassengerCount);
            parameters.Add("StartDatetime", loanRequest.StartDatetime);
            parameters.Add("EndDatetime", loanRequest.EndDatetime);
            parameters.Add("Status", loanRequest.Status);
            parameters.Add("Notes", loanRequest.Notes);
            parameters.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

            await connection.ExecuteAsync(sql, parameters);
            return parameters.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(LoanRequest loanRequest)
        {
            const string sql = @"
                UPDATE loan_requests
                SET purpose = :Purpose, destination = :Destination, passenger_count = :PassengerCount,
                    start_datetime = :StartDatetime, end_datetime = :EndDatetime, 
                    status = :Status, notes = :Notes
                WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                loanRequest.Id,
                loanRequest.Purpose,
                loanRequest.Destination,
                loanRequest.PassengerCount,
                loanRequest.StartDatetime,
                loanRequest.EndDatetime,
                loanRequest.Status,
                loanRequest.Notes
            });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            const string sql = "UPDATE loan_requests SET status = :Status WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, Status = status });
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE loan_requests SET status = 'CANCELLED' WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<string> GenerateRequestNumberAsync()
        {
            const string sql = @"
                SELECT 'LR-' || TO_CHAR(SYSDATE, 'YYYYMMDD') || '-' || 
                       LPAD(NVL(MAX(TO_NUMBER(SUBSTR(request_number, -6))), 0) + 1, 6, '0')
                FROM loan_requests
                WHERE request_number LIKE 'LR-' || TO_CHAR(SYSDATE, 'YYYYMMDD') || '%'";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryFirstAsync<string>(sql);
            return result ?? $"LR-{DateTime.Now:yyyyMMdd}-000001";
        }

        public async Task<int> GetCountByStatusAsync(string status)
        {
            const string sql = "SELECT COUNT(1) FROM loan_requests WHERE status = :Status";

            using var connection = _dbContext.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { Status = status });
        }

        public async Task<int> GetTotalCountAsync(int? userId = null)
        {
            var sql = "SELECT COUNT(1) FROM loan_requests WHERE 1=1";
            if (userId.HasValue)
                sql += " AND user_id = :UserId";

            using var connection = _dbContext.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }
    }
}
