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
        Task<Schedule?> GetByIdWithDetailsAsync(int id);
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
        Task<bool> CancelScheduleAsync(int scheduleId, string cancellationReason);
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
                SELECT schedule_id AS Id, loan_request_id AS LoanRequestId, driver_id AS DriverId,
                       vehicle_id AS VehicleId, assigned_at AS AssignedAt, status, notes
                FROM schedules
                WHERE schedule_id = :Id";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Schedule>(sql, new { Id = id });
        }

        public async Task<Schedule?> GetByIdWithDetailsAsync(int id)
        {
            const string sql = @"
                SELECT s.schedule_id AS Id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_at AS AssignedAt, s.status, s.notes,
                       lr.loan_request_id AS Id, lr.user_id AS UserId, lr.purpose, lr.destination
                FROM schedules s
                INNER JOIN loan_requests lr ON s.loan_request_id = lr.loan_request_id
                WHERE s.schedule_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Schedule, LoanRequest, Schedule>(
                sql,
                (schedule, loanRequest) =>
                {
                    schedule.LoanRequest = loanRequest;
                    return schedule;
                },
                new { Id = id },
                splitOn: "Id"
            );

            return result.FirstOrDefault();
        }

        public async Task<Schedule?> GetByLoanRequestIdAsync(int loanRequestId)
        {
            const string sql = @"
                SELECT s.schedule_id AS Id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_at AS AssignedAt, s.status, s.notes,
                       d.driver_id AS Id, d.user_id AS UserId, d.license_number AS LicenseNumber,
                       d.license_expiry AS LicenseExpiry, d.status AS Status,
                       du.user_id AS Id, du.full_name AS Name, du.email AS Email, du.phone_number AS PhoneNumber,
                       v.vehicle_id AS Id, v.license_plate AS PlateNumber, v.brand, v.type, 
                       v.year, v.capacity, v.status AS Status
                FROM schedules s
                LEFT JOIN drivers d ON s.driver_id = d.driver_id
                LEFT JOIN users du ON d.user_id = du.user_id
                LEFT JOIN vehicles v ON s.vehicle_id = v.vehicle_id
                WHERE s.loan_request_id = :LoanRequestId";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Schedule, Driver, User, Vehicle, Schedule>(
                sql,
                (schedule, driver, driverUser, vehicle) =>
                {
                    if (driver != null && driverUser != null)
                    {
                        driver.User = driverUser;
                        schedule.Driver = driver;
                    }
                    if (vehicle != null)
                    {
                        schedule.Vehicle = vehicle;
                    }
                    return schedule;
                },
                new { LoanRequestId = loanRequestId },
                splitOn: "Id,Id,Id"
            );

            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<Schedule>> GetAllAsync(string? status = null)
        {
            var sql = @"
                SELECT s.schedule_id AS Id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_at AS AssignedAt, s.status, s.notes,
                       lr.loan_request_id AS Id, lr.purpose, lr.destination, lr.guest_list AS GuestList,
                       lr.hotel_accommodation AS HotelAccommodation, lr.start_datetime AS StartDatetime, 
                       lr.end_datetime AS EndDatetime, lr.status AS LRStatus,
                       d.driver_id AS Id, d.license_number AS LicenseNumber,
                       v.vehicle_id AS Id, v.license_plate AS PlateNumber, v.brand, v.type, v.capacity,
                       u.user_id AS Id, u.full_name AS Name, u.email
                FROM schedules s
                INNER JOIN loan_requests lr ON s.loan_request_id = lr.loan_request_id
                LEFT JOIN drivers d ON s.driver_id = d.driver_id
                LEFT JOIN vehicles v ON s.vehicle_id = v.vehicle_id
                LEFT JOIN users u ON lr.user_id = u.user_id
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
                    if (s.LoanRequest != null) s.LoanRequest.User = u;
                    s.Driver = d;
                    s.Vehicle = v;
                    return s;
                },
                new { Status = status },
                splitOn: "Id,Id,Id,Id"
            );

            return result;
        }

        public async Task<IEnumerable<Schedule>> GetByDriverIdAsync(int driverId)
        {
            const string sql = @"
                SELECT s.schedule_id AS Id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_at AS AssignedAt, s.status, s.notes,
                       lr.loan_request_id AS Id, lr.purpose, lr.destination, lr.guest_list AS GuestList,
                       lr.hotel_accommodation AS HotelAccommodation, lr.start_datetime AS StartDatetime, 
                       lr.end_datetime AS EndDatetime,
                       v.vehicle_id AS Id, v.license_plate AS PlateNumber, v.brand, v.type, v.capacity,
                       u.user_id AS Id, u.full_name AS Name, u.email
                FROM schedules s
                INNER JOIN loan_requests lr ON s.loan_request_id = lr.loan_request_id
                LEFT JOIN vehicles v ON s.vehicle_id = v.vehicle_id
                LEFT JOIN users u ON lr.user_id = u.user_id
                WHERE s.driver_id = :DriverId
                ORDER BY lr.start_datetime DESC";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Schedule, LoanRequest, Vehicle, User, Schedule>(
                sql,
                (s, lr, v, u) =>
                {
                    s.LoanRequest = lr;
                    if (s.LoanRequest != null) s.LoanRequest.User = u;
                    s.Vehicle = v;
                    return s;
                },
                new { DriverId = driverId },
                splitOn: "Id,Id,Id"
            );

            return result;
        }

        public async Task<IEnumerable<Schedule>> GetByDriverUserIdAsync(int userId)
        {
            const string sql = @"
                SELECT s.schedule_id AS Id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_at AS AssignedAt, s.status, s.notes,
                       lr.loan_request_id AS Id, lr.purpose, lr.destination, lr.guest_list AS GuestList,
                       lr.hotel_accommodation AS HotelAccommodation, lr.start_datetime AS StartDatetime, 
                       lr.end_datetime AS EndDatetime,
                       v.vehicle_id AS Id, v.license_plate AS PlateNumber, v.brand, v.type, v.capacity,
                       u.user_id AS Id, u.full_name AS Name, u.email, u.phone_number AS PhoneNumber
                FROM schedules s
                INNER JOIN loan_requests lr ON s.loan_request_id = lr.loan_request_id
                LEFT JOIN vehicles v ON s.vehicle_id = v.vehicle_id
                INNER JOIN drivers d ON s.driver_id = d.driver_id
                LEFT JOIN users u ON lr.user_id = u.user_id
                WHERE d.user_id = :UserId
                ORDER BY lr.start_datetime DESC";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Schedule, LoanRequest, Vehicle, User, Schedule>(
                sql,
                (s, lr, v, u) =>
                {
                    s.LoanRequest = lr;
                    if (s.LoanRequest != null) s.LoanRequest.User = u;
                    s.Vehicle = v;
                    return s;
                },
                new { UserId = userId },
                splitOn: "Id,Id,Id"
            );

            return result;
        }

        public async Task<IEnumerable<Schedule>> GetUpcomingByDriverIdAsync(int driverId)
        {
            const string sql = @"
                SELECT s.schedule_id AS Id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_at AS AssignedAt, s.status, s.notes,
                       lr.loan_request_id AS Id, lr.purpose, lr.destination, lr.guest_list AS GuestList,
                       lr.hotel_accommodation AS HotelAccommodation, lr.start_datetime AS StartDatetime, 
                       lr.end_datetime AS EndDatetime,
                       v.vehicle_id AS Id, v.license_plate AS PlateNumber, v.brand, v.type, v.capacity,
                       u.user_id AS Id, u.full_name AS Name, u.email
                FROM schedules s
                INNER JOIN loan_requests lr ON s.loan_request_id = lr.loan_request_id
                LEFT JOIN vehicles v ON s.vehicle_id = v.vehicle_id
                LEFT JOIN users u ON lr.user_id = u.user_id
                WHERE s.driver_id = :DriverId
                  AND s.status IN ('CONFIRMED', 'IN_PROGRESS')
                  AND lr.end_datetime >= SYSDATE
                ORDER BY lr.start_datetime ASC";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Schedule, LoanRequest, Vehicle, User, Schedule>(
                sql,
                (s, lr, v, u) =>
                {
                    s.LoanRequest = lr;
                    if (s.LoanRequest != null) s.LoanRequest.User = u;
                    s.Vehicle = v;
                    return s;
                },
                new { DriverId = driverId },
                splitOn: "Id,Id,Id"
            );

            return result;
        }

        public async Task<IEnumerable<Schedule>> GetByVehicleIdAsync(int vehicleId)
        {
            const string sql = @"
                SELECT schedule_id AS Id, loan_request_id AS LoanRequestId, driver_id AS DriverId,
                       vehicle_id AS VehicleId, assigned_at AS AssignedAt, status, notes
                FROM schedules
                WHERE vehicle_id = :VehicleId
                ORDER BY assigned_at DESC";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryAsync<Schedule>(sql, new { VehicleId = vehicleId });
        }

        public async Task<IEnumerable<Schedule>> GetByDateRangeAsync(DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT s.schedule_id AS Id, s.loan_request_id AS LoanRequestId, s.driver_id AS DriverId,
                       s.vehicle_id AS VehicleId, s.assigned_at AS AssignedAt, s.status, s.notes,
                       lr.loan_request_id AS Id, lr.purpose, lr.destination, lr.guest_list AS GuestList,
                       lr.hotel_accommodation AS HotelAccommodation, lr.start_datetime AS StartDatetime, 
                       lr.end_datetime AS EndDatetime
                FROM schedules s
                INNER JOIN loan_requests lr ON s.loan_request_id = lr.loan_request_id
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
                splitOn: "Id"
            );

            return result;
        }

        public async Task<int> CreateAsync(Schedule schedule)
        {
            const string sql = @"
                INSERT INTO schedules (schedule_id, loan_request_id, driver_id, vehicle_id, status, notes)
                VALUES (seq_schedules.NEXTVAL, :LoanRequestId, :DriverId, :VehicleId, :Status, :Notes)
                RETURNING schedule_id INTO :Id";

            using var connection = _dbContext.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("LoanRequestId", schedule.LoanRequestId);
            parameters.Add("DriverId", schedule.DriverId);
            parameters.Add("VehicleId", schedule.VehicleId);
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
                SET driver_id = :DriverId, vehicle_id = :VehicleId, status = :Status, notes = :Notes
                WHERE schedule_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                schedule.Id,
                schedule.DriverId,
                schedule.VehicleId,
                schedule.Status,
                schedule.Notes
            });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            const string sql = "UPDATE schedules SET status = :Status WHERE schedule_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, Status = status });
            return rowsAffected > 0;
        }

        public async Task<bool> CancelScheduleAsync(int scheduleId, string cancellationReason)
        {
            const string sql = @"
                UPDATE schedules 
                SET status = 'CANCELLED', 
                    notes = :CancellationReason 
                WHERE schedule_id = :ScheduleId";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { ScheduleId = scheduleId, CancellationReason = cancellationReason });
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE schedules SET status = 'CANCELLED' WHERE schedule_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<int> GetScheduledCountAsync()
        {
            const string sql = "SELECT COUNT(1) FROM schedules WHERE status IN ('CONFIRMED', 'IN_PROGRESS')";

            using var connection = _dbContext.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql);
        }
    }
}
