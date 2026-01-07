using Dapper;
using PelindoCarLoan.API.Models;

namespace PelindoCarLoan.API.Repositories
{
    /// <summary>
    /// Repository interface for Vehicle operations
    /// </summary>
    public interface IVehicleRepository
    {
        Task<Vehicle?> GetByIdAsync(int id);
        Task<IEnumerable<Vehicle>> GetAllAsync(string? status = null);
        Task<IEnumerable<Vehicle>> GetAvailableAsync();
        Task<IEnumerable<Vehicle>> GetAvailableForPeriodAsync(DateTime start, DateTime end);
        Task<int> CreateAsync(Vehicle vehicle);
        Task<bool> UpdateAsync(Vehicle vehicle);
        Task<bool> UpdateStatusAsync(int id, string status);
        Task<bool> DeleteAsync(int id);
        Task<int> GetAvailableCountAsync();
    }

    public class VehicleRepository : IVehicleRepository
    {
        private readonly IDbContext _dbContext;

        public VehicleRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Vehicle?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT id, plate_number AS PlateNumber, brand, type, capacity, status,
                       notes, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM vehicles
                WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Vehicle>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Vehicle>> GetAllAsync(string? status = null)
        {
            var sql = @"
                SELECT id, plate_number AS PlateNumber, brand, type, capacity, status,
                       notes, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM vehicles
                WHERE is_active = 1";

            if (!string.IsNullOrEmpty(status))
                sql += " AND status = :Status";

            sql += " ORDER BY plate_number";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryAsync<Vehicle>(sql, new { Status = status });
        }

        public async Task<IEnumerable<Vehicle>> GetAvailableAsync()
        {
            return await GetAllAsync(VehicleStatus.Available);
        }

        public async Task<IEnumerable<Vehicle>> GetAvailableForPeriodAsync(DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT v.id, v.plate_number AS PlateNumber, v.brand, v.type, v.capacity, v.status,
                       v.notes, v.is_active AS IsActive, v.created_at AS CreatedAt, v.updated_at AS UpdatedAt
                FROM vehicles v
                WHERE v.is_active = 1 
                  AND v.status = 'AVAILABLE'
                  AND NOT EXISTS (
                      SELECT 1 FROM schedules s
                      INNER JOIN loan_requests lr ON s.loan_request_id = lr.id
                      WHERE s.vehicle_id = v.id
                        AND s.status NOT IN ('COMPLETED', 'CANCELLED')
                        AND lr.start_datetime < :EndTime
                        AND lr.end_datetime > :StartTime
                  )
                ORDER BY v.plate_number";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryAsync<Vehicle>(sql, new { StartTime = start, EndTime = end });
        }

        public async Task<int> CreateAsync(Vehicle vehicle)
        {
            const string sql = @"
                INSERT INTO vehicles (plate_number, brand, type, capacity, status, notes, is_active)
                VALUES (:PlateNumber, :Brand, :Type, :Capacity, :Status, :Notes, 1)
                RETURNING id INTO :Id";

            using var connection = _dbContext.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("PlateNumber", vehicle.PlateNumber);
            parameters.Add("Brand", vehicle.Brand);
            parameters.Add("Type", vehicle.Type);
            parameters.Add("Capacity", vehicle.Capacity);
            parameters.Add("Status", vehicle.Status);
            parameters.Add("Notes", vehicle.Notes);
            parameters.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

            await connection.ExecuteAsync(sql, parameters);
            return parameters.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(Vehicle vehicle)
        {
            const string sql = @"
                UPDATE vehicles
                SET plate_number = :PlateNumber, brand = :Brand, type = :Type,
                    capacity = :Capacity, status = :Status, notes = :Notes
                WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                vehicle.Id,
                vehicle.PlateNumber,
                vehicle.Brand,
                vehicle.Type,
                vehicle.Capacity,
                vehicle.Status,
                vehicle.Notes
            });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            const string sql = "UPDATE vehicles SET status = :Status WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, Status = status });
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE vehicles SET is_active = 0 WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<int> GetAvailableCountAsync()
        {
            const string sql = "SELECT COUNT(1) FROM vehicles WHERE status = 'AVAILABLE' AND is_active = 1";

            using var connection = _dbContext.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql);
        }
    }
}
