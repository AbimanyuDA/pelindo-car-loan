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
        Task<IEnumerable<LoanRequest>> GetEmergencyForApprovalAsync(int approvalLevel);
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
                SELECT loan_request_id AS Id, user_id AS UserId, NULL AS RequestNumber, 
                       service_letter_basis AS ServiceLetterBasis, service_letter_file_path AS ServiceLetterFilePath,
                       purpose, destination,
                       guest_list AS GuestList, hotel_accommodation AS HotelAccommodation, 
                       vehicle_id AS VehicleId, driver_id AS DriverId, 
                       start_datetime AS StartDatetime, end_datetime AS EndDatetime, 
                       status, NULL AS Notes, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM loan_requests
                WHERE loan_request_id = :Id";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<LoanRequest>(sql, new { Id = id });
        }

        public async Task<LoanRequest?> GetByIdWithDetailsAsync(int id)
        {
            const string sql = @"
                SELECT lr.loan_request_id AS Id, lr.user_id AS UserId, NULL AS RequestNumber, 
                       lr.service_letter_basis AS ServiceLetterBasis, lr.service_letter_file_path AS ServiceLetterFilePath,
                       lr.purpose, lr.destination, lr.guest_list AS GuestList,
                       lr.hotel_accommodation AS HotelAccommodation, 
                       lr.vehicle_id AS VehicleId, lr.driver_id AS DriverId,
                       lr.start_datetime AS StartDatetime, lr.end_datetime AS EndDatetime, 
                       lr.status, NULL AS Notes, lr.created_at AS CreatedAt, lr.updated_at AS UpdatedAt,
                       u.user_id AS Id, u.full_name AS Name, u.email, u.phone_number AS PhoneNumber, u.role, u.division, u.unit_kerja AS UnitKerja,
                       d.driver_id AS Id, d.user_id AS UserId,
                       du.user_id AS Id, du.full_name AS Name, du.email, du.phone_number AS PhoneNumber
                FROM loan_requests lr
                INNER JOIN users u ON lr.user_id = u.user_id
                LEFT JOIN drivers d ON lr.driver_id = d.driver_id
                LEFT JOIN users du ON d.user_id = du.user_id
                WHERE lr.loan_request_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<LoanRequest, User, Driver, User, LoanRequest>(
                sql,
                (lr, u, d, du) =>
                {
                    lr.User = u;
                    if (d != null && du != null)
                    {
                        d.User = du;
                        lr.Driver = d;
                    }
                    return lr;
                },
                new { Id = id },
                splitOn: "Id,Id,Id"
            );

            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<LoanRequest>> GetAllAsync(int? userId = null, string? status = null)
        {
            var sql = @"
                SELECT lr.loan_request_id AS Id, lr.user_id AS UserId, NULL AS RequestNumber, 
                       lr.service_letter_basis AS ServiceLetterBasis, lr.service_letter_file_path AS ServiceLetterFilePath,
                       lr.purpose, lr.destination, lr.guest_list AS GuestList,
                       lr.hotel_accommodation AS HotelAccommodation,
                       lr.vehicle_id AS VehicleId, lr.driver_id AS DriverId,
                       lr.start_datetime AS StartDatetime, lr.end_datetime AS EndDatetime, 
                       lr.status, NULL AS Notes, lr.created_at AS CreatedAt, lr.updated_at AS UpdatedAt,
                       u.user_id AS Id, u.full_name AS Name, u.email, u.phone_number AS PhoneNumber, u.role, u.division, u.unit_kerja AS UnitKerja
                FROM loan_requests lr
                INNER JOIN users u ON lr.user_id = u.user_id
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
                splitOn: "Id"
            );

            return result;
        }

        public async Task<IEnumerable<LoanRequest>> GetPendingForApprovalAsync(int approvalLevel)
        {
            var status = approvalLevel == 1 ? LoanRequestStatus.Submitted : LoanRequestStatus.ApprovedL1;

            const string sql = @"
                SELECT lr.loan_request_id AS Id, lr.user_id AS UserId, NULL AS RequestNumber, 
                       lr.service_letter_basis AS ServiceLetterBasis, lr.service_letter_file_path AS ServiceLetterFilePath,
                       lr.purpose, lr.destination, lr.guest_list AS GuestList,
                       lr.hotel_accommodation AS HotelAccommodation,
                       lr.vehicle_id AS VehicleId, lr.driver_id AS DriverId,
                       lr.start_datetime AS StartDatetime, lr.end_datetime AS EndDatetime, 
                       lr.status, lr.notes AS Notes, lr.created_at AS CreatedAt, lr.updated_at AS UpdatedAt,
                       u.user_id AS Id, u.full_name AS Name, u.email, u.phone_number AS PhoneNumber, u.role, u.division, u.unit_kerja AS UnitKerja,
                       d.driver_id AS Id, d.user_id AS UserId,
                       du.user_id AS Id, du.full_name AS Name, du.email, du.phone_number AS PhoneNumber
                FROM loan_requests lr
                INNER JOIN users u ON lr.user_id = u.user_id
                LEFT JOIN drivers d ON lr.driver_id = d.driver_id
                LEFT JOIN users du ON d.user_id = du.user_id
                WHERE lr.status = :Status
                  AND NOT EXISTS (
                    SELECT 1 FROM schedules s 
                    WHERE s.loan_request_id = lr.loan_request_id 
                                        AND s.schedule_id = (
                                                SELECT MAX(s2.schedule_id) 
                                                FROM schedules s2 
                                                WHERE s2.loan_request_id = lr.loan_request_id
                                        )
                                                                                AND s.status IN ('WAITING', 'WAITING_L2')
                                        AND s.emergency_reason IS NOT NULL
                  )
                ORDER BY lr.created_at ASC";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<LoanRequest, User, Driver, User, LoanRequest>(
                sql,
                (lr, u, d, du) =>
                {
                    lr.User = u;
                    if (d != null && du != null)
                    {
                        d.User = du;
                        lr.Driver = d;
                    }
                    return lr;
                },
                new { Status = status },
                splitOn: "Id,Id,Id"
            );

            return result;
        }

        public async Task<IEnumerable<LoanRequest>> GetEmergencyForApprovalAsync(int approvalLevel)
        {
            var scheduleStatus = approvalLevel == 1 ? ScheduleStatus.Waiting : ScheduleStatus.WaitingL2;

            const string sql = @"
                SELECT lr.loan_request_id AS Id, lr.user_id AS UserId, NULL AS RequestNumber, 
                       lr.service_letter_basis AS ServiceLetterBasis, lr.service_letter_file_path AS ServiceLetterFilePath,
                       lr.purpose, lr.destination, lr.guest_list AS GuestList,
                       lr.hotel_accommodation AS HotelAccommodation,
                       lr.vehicle_id AS VehicleId, lr.driver_id AS DriverId,
                       lr.start_datetime AS StartDatetime, lr.end_datetime AS EndDatetime, 
                       lr.status, lr.notes AS Notes, lr.created_at AS CreatedAt, lr.updated_at AS UpdatedAt,
                       u.user_id AS Id, u.full_name AS Name, u.email, u.phone_number AS PhoneNumber, u.role, u.division, u.unit_kerja AS UnitKerja,
                       d.driver_id AS Id, d.user_id AS UserId,
                       du.user_id AS Id, du.full_name AS Name, du.email, du.phone_number AS PhoneNumber,
                       s.schedule_id AS Id, s.emergency_reason AS EmergencyReason, s.emergency_type AS EmergencyType
                FROM loan_requests lr
                INNER JOIN users u ON lr.user_id = u.user_id
                LEFT JOIN drivers d ON lr.driver_id = d.driver_id
                LEFT JOIN users du ON d.user_id = du.user_id
                INNER JOIN schedules s ON lr.loan_request_id = s.loan_request_id
                WHERE s.schedule_id = (
                        SELECT MAX(s2.schedule_id) 
                        FROM schedules s2 
                        WHERE s2.loan_request_id = lr.loan_request_id
                    )
                    AND s.status = :ScheduleStatus
                    AND s.emergency_reason IS NOT NULL
                ORDER BY s.assigned_at DESC";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<LoanRequest, User, Driver, User, Schedule, LoanRequest>(
                sql,
                (lr, u, d, du, schedule) =>
                {
                    lr.User = u;
                    if (d != null && du != null)
                    {
                        d.User = du;
                        lr.Driver = d;
                    }
                    lr.Schedule = schedule;
                    return lr;
                },
                new { ScheduleStatus = scheduleStatus },
                splitOn: "Id,Id,Id,Id"
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
                INSERT INTO loan_requests (loan_request_id, user_id, service_letter_basis, service_letter_file_path, 
                                          purpose, destination, guest_list, hotel_accommodation, vehicle_id, driver_id, 
                                          start_datetime, end_datetime, status, notes)
                VALUES (seq_loan_requests.NEXTVAL, :UserId, :ServiceLetterBasis, :ServiceLetterFilePath,
                        :Purpose, :Destination, :GuestList, :HotelAccommodation, :VehicleId, :DriverId, 
                        :StartDatetime, :EndDatetime, :Status, :Notes)
                RETURNING loan_request_id INTO :Id";

            using var connection = _dbContext.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", loanRequest.UserId);
            parameters.Add("ServiceLetterBasis", loanRequest.ServiceLetterBasis);
            parameters.Add("ServiceLetterFilePath", loanRequest.ServiceLetterFilePath);
            parameters.Add("Purpose", loanRequest.Purpose);
            parameters.Add("Destination", loanRequest.Destination);
            parameters.Add("GuestList", loanRequest.GuestList);
            parameters.Add("HotelAccommodation", loanRequest.HotelAccommodation);
            parameters.Add("VehicleId", loanRequest.VehicleId);
            parameters.Add("DriverId", loanRequest.DriverId);
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
                SET service_letter_basis = :ServiceLetterBasis, service_letter_file_path = :ServiceLetterFilePath,
                    purpose = :Purpose, destination = :Destination, 
                    guest_list = :GuestList, hotel_accommodation = :HotelAccommodation, 
                    vehicle_id = :VehicleId, driver_id = :DriverId,
                    start_datetime = :StartDatetime, end_datetime = :EndDatetime, 
                    status = :Status, updated_at = SYSTIMESTAMP
                WHERE loan_request_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                loanRequest.Id,
                loanRequest.ServiceLetterBasis,
                loanRequest.ServiceLetterFilePath,
                loanRequest.Purpose,
                loanRequest.Destination,
                loanRequest.GuestList,
                loanRequest.HotelAccommodation,
                loanRequest.VehicleId,
                loanRequest.DriverId,
                loanRequest.StartDatetime,
                loanRequest.EndDatetime,
                loanRequest.Status
            });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            const string sql = "UPDATE loan_requests SET status = :Status, updated_at = SYSTIMESTAMP WHERE loan_request_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, Status = status });
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE loan_requests SET status = 'CANCELLED', updated_at = SYSTIMESTAMP WHERE loan_request_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<string> GenerateRequestNumberAsync()
        {
            // Generate request number based on sequence
            const string sql = @"SELECT seq_loan_requests.NEXTVAL FROM DUAL";

            using var connection = _dbContext.CreateConnection();
            var seqVal = await connection.QueryFirstAsync<int>(sql);
            return $"LR-{DateTime.Now:yyyyMMdd}-{seqVal:D6}";
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
