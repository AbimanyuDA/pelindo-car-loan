using Dapper;
using PelindoCarLoan.API.Models;

namespace PelindoCarLoan.API.Repositories
{
    /// <summary>
    /// Repository interface for Schedule operations
    /// </summary>
    public interface IScheduleRepository
    {
        Task<Schedule?> GetByIdAsync(int id);
        Task<Schedule?> GetByLoanRequestIdAsync(int loanRequestId);
        Task<IEnumerable<Schedule>> GetAllAsync(string? status = null);
        Task<IEnumerable<Schedule>> GetByDriverIdAsync(int driverId);
        Task<IEnumerable<Schedule>> GetByDriverUserIdAsync(int userId);
        Task<IEnumerable<Schedule>> GetUpcomingByDriverIdAsync(int driverId);
        Task<IEnumerable<Schedule>> GetByVehicleIdAsync(int vehicleId);
        Task<IEnumerable<Schedule>> GetByDateRangeAsync(DateTime start, DateTime end);
        Task<int> CreateAsync(Schedule schedule);
        Task<bool> UpdateAsync(Schedule schedule);
        Task<bool> UpdateStatusAsync(int id, string status);
        Task<bool> DeleteAsync(int id);
        Task<int> GetScheduledCountAsync();
    }

    public class ScheduleRepository : IScheduleRepository
    {
        private readonly IDbContext _dbContext;

        public ScheduleRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Schedule?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT s.id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_by AS AssignedBy, s.assigned_at AS AssignedAt,
                       s.actual_start_time AS ActualStartTime, s.actual_end_time AS ActualEndTime,
                       s.status, s.notes
                FROM schedules s
                WHERE s.id = :Id";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Schedule>(sql, new { Id = id });
        }

        public async Task<Schedule?> GetByLoanRequestIdAsync(int loanRequestId)
        {
            const string sql = @"
                SELECT s.id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_by AS AssignedBy, s.assigned_at AS AssignedAt,
                       s.actual_start_time AS ActualStartTime, s.actual_end_time AS ActualEndTime,
                       s.status, s.notes
                FROM schedules s
                WHERE s.loan_request_id = :LoanRequestId";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Schedule>(sql, new { LoanRequestId = loanRequestId });
        }

        public async Task<IEnumerable<Schedule>> GetAllAsync(string? status = null)
        {
            var sql = @"
                SELECT s.id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_by AS AssignedBy, s.assigned_at AS AssignedAt,
                       s.actual_start_time AS ActualStartTime, s.actual_end_time AS ActualEndTime,
                       s.status, s.notes,
                       lr.id, lr.request_number AS RequestNumber, lr.purpose, lr.destination,
                       lr.passenger_count AS PassengerCount, lr.start_datetime AS StartDatetime, 
                       lr.end_datetime AS EndDatetime, lr.status AS LRStatus,
                       d.id, d.license_number AS LicenseNumber, d.phone_number AS PhoneNumber,
                       v.id, v.plate_number AS PlateNumber, v.brand, v.type, v.capacity,
                       u.id, u.name, u.email
                FROM schedules s
                INNER JOIN loan_requests lr ON s.loan_request_id = lr.id
                INNER JOIN drivers d ON s.driver_id = d.id
                INNER JOIN vehicles v ON s.vehicle_id = v.id
                LEFT JOIN users u ON lr.user_id = u.id
                WHERE 1=1";

            if (!string.IsNullOrEmpty(status))
                sql += " AND s.status = :Status";

            sql += " ORDER BY lr.start_datetime DESC";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Schedule, LoanRequest, Driver, Vehicle, User, Schedule>(
                sql,
                (s, lr, d, v, u) =>
                {
                    s.LoanRequest = lr;
                    s.LoanRequest.User = u;
                    s.Driver = d;
                    s.Vehicle = v;
                    return s;
                },
                new { Status = status },
                splitOn: "id,id,id,id"
            );

            return result;
        }

        public async Task<IEnumerable<Schedule>> GetByDriverIdAsync(int driverId)
        {
            const string sql = @"
                SELECT s.id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_by AS AssignedBy, s.assigned_at AS AssignedAt,
                       s.actual_start_time AS ActualStartTime, s.actual_end_time AS ActualEndTime,
                       s.status, s.notes,
                       lr.id, lr.request_number AS RequestNumber, lr.purpose, lr.destination,
                       lr.passenger_count AS PassengerCount, lr.start_datetime AS StartDatetime, 
                       lr.end_datetime AS EndDatetime,
                       v.id, v.plate_number AS PlateNumber, v.brand, v.type, v.capacity,
                       u.id, u.name, u.email
                FROM schedules s
                INNER JOIN loan_requests lr ON s.loan_request_id = lr.id
                INNER JOIN vehicles v ON s.vehicle_id = v.id
                LEFT JOIN users u ON lr.user_id = u.id
                WHERE s.driver_id = :DriverId
                ORDER BY lr.start_datetime DESC";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Schedule, LoanRequest, Vehicle, User, Schedule>(
                sql,
                (s, lr, v, u) =>
                {
                    s.LoanRequest = lr;
                    s.LoanRequest.User = u;
                    s.Vehicle = v;
                    return s;
                },
                new { DriverId = driverId },
                splitOn: "id,id,id"
            );

            return result;
        }

        public async Task<IEnumerable<Schedule>> GetByDriverUserIdAsync(int userId)
        {
            const string sql = @"
                SELECT s.id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_by AS AssignedBy, s.assigned_at AS AssignedAt,
                       s.actual_start_time AS ActualStartTime, s.actual_end_time AS ActualEndTime,
                       s.status, s.notes,
                       lr.id, lr.request_number AS RequestNumber, lr.purpose, lr.destination,
                       lr.passenger_count AS PassengerCount, lr.start_datetime AS StartDatetime, 
                       lr.end_datetime AS EndDatetime,
                       v.id, v.plate_number AS PlateNumber, v.brand, v.type, v.capacity,
                       u.id, u.name, u.email
                FROM schedules s
                INNER JOIN loan_requests lr ON s.loan_request_id = lr.id
                INNER JOIN vehicles v ON s.vehicle_id = v.id
                INNER JOIN drivers d ON s.driver_id = d.id
                LEFT JOIN users u ON lr.user_id = u.id
                WHERE d.user_id = :UserId
                ORDER BY lr.start_datetime DESC";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Schedule, LoanRequest, Vehicle, User, Schedule>(
                sql,
                (s, lr, v, u) =>
                {
                    s.LoanRequest = lr;
                    s.LoanRequest.User = u;
                    s.Vehicle = v;
                    return s;
                },
                new { UserId = userId },
                splitOn: "id,id,id"
            );

            return result;
        }

        public async Task<IEnumerable<Schedule>> GetUpcomingByDriverIdAsync(int driverId)
        {
            const string sql = @"
                SELECT s.id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_by AS AssignedBy, s.assigned_at AS AssignedAt,
                       s.actual_start_time AS ActualStartTime, s.actual_end_time AS ActualEndTime,
                       s.status, s.notes,
                       lr.id, lr.request_number AS RequestNumber, lr.purpose, lr.destination,
                       lr.passenger_count AS PassengerCount, lr.start_datetime AS StartDatetime, 
                       lr.end_datetime AS EndDatetime,
                       v.id, v.plate_number AS PlateNumber, v.brand, v.type, v.capacity,
                       u.id, u.name, u.email
                FROM schedules s
                INNER JOIN loan_requests lr ON s.loan_request_id = lr.id
                INNER JOIN vehicles v ON s.vehicle_id = v.id
                LEFT JOIN users u ON lr.user_id = u.id
                WHERE s.driver_id = :DriverId
                  AND s.status IN ('ASSIGNED', 'IN_PROGRESS')
                  AND lr.end_datetime >= SYSDATE
                ORDER BY lr.start_datetime ASC";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Schedule, LoanRequest, Vehicle, User, Schedule>(
                sql,
                (s, lr, v, u) =>
                {
                    s.LoanRequest = lr;
                    s.LoanRequest.User = u;
                    s.Vehicle = v;
                    return s;
                },
                new { DriverId = driverId },
                splitOn: "id,id,id"
            );

            return result;
        }

        public async Task<IEnumerable<Schedule>> GetByVehicleIdAsync(int vehicleId)
        {
            const string sql = @"
                SELECT s.id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_by AS AssignedBy, s.assigned_at AS AssignedAt,
                       s.actual_start_time AS ActualStartTime, s.actual_end_time AS ActualEndTime,
                       s.status, s.notes
                FROM schedules s
                WHERE s.vehicle_id = :VehicleId
                ORDER BY s.assigned_at DESC";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryAsync<Schedule>(sql, new { VehicleId = vehicleId });
        }

        public async Task<IEnumerable<Schedule>> GetByDateRangeAsync(DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT s.id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_by AS AssignedBy, s.assigned_at AS AssignedAt,
                       s.actual_start_time AS ActualStartTime, s.actual_end_time AS ActualEndTime,
                       s.status, s.notes,
                       lr.id, lr.request_number AS RequestNumber, lr.purpose, lr.destination,
                       lr.passenger_count AS PassengerCount, lr.start_datetime AS StartDatetime, 
                       lr.end_datetime AS EndDatetime
                FROM schedules s
                INNER JOIN loan_requests lr ON s.loan_request_id = lr.id
                WHERE lr.start_datetime >= :StartTime AND lr.start_datetime <= :EndTime
                ORDER BY lr.start_datetime";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Schedule, LoanRequest, Schedule>(
                sql,
                (s, lr) =>
                {
                    s.LoanRequest = lr;
                    return s;
                },
                new { StartTime = start, EndTime = end },
                splitOn: "id"
            );

            return result;
        }

        public async Task<int> CreateAsync(Schedule schedule)
        {
            const string sql = @"
                INSERT INTO schedules (loan_request_id, driver_id, vehicle_id, assigned_by, status, notes)
                VALUES (:LoanRequestId, :DriverId, :VehicleId, :AssignedBy, :Status, :Notes)
                RETURNING id INTO :Id";

            using var connection = _dbContext.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("LoanRequestId", schedule.LoanRequestId);
            parameters.Add("DriverId", schedule.DriverId);
            parameters.Add("VehicleId", schedule.VehicleId);
            parameters.Add("AssignedBy", schedule.AssignedBy);
            parameters.Add("Status", schedule.Status);
            parameters.Add("Notes", schedule.Notes);
            parameters.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

            await connection.ExecuteAsync(sql, parameters);
            return parameters.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(Schedule schedule)
        {
            const string sql = @"
                UPDATE schedules
                SET driver_id = :DriverId, vehicle_id = :VehicleId,
                    actual_start_time = :ActualStartTime, actual_end_time = :ActualEndTime,
                    status = :Status, notes = :Notes
                WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                schedule.Id,
                schedule.DriverId,
                schedule.VehicleId,
                schedule.ActualStartTime,
                schedule.ActualEndTime,
                schedule.Status,
                schedule.Notes
            });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            const string sql = "UPDATE schedules SET status = :Status WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, Status = status });
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE schedules SET status = 'CANCELLED' WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<int> GetScheduledCountAsync()
        {
            const string sql = "SELECT COUNT(1) FROM schedules WHERE status IN ('ASSIGNED', 'IN_PROGRESS')";

            using var connection = _dbContext.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql);
        }
    }
}
