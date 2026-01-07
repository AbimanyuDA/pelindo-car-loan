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
                SELECT vehicle_id, license_plate, brand, type, model, year, capacity, status,
                       last_maintenance, next_maintenance, created_at, updated_at
                FROM vehicles
                WHERE vehicle_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
            if (result == null) return null;
            
            return MapToVehicle(result);
        }

        public async Task<IEnumerable<Vehicle>> GetAllAsync(string? status = null)
        {
            var sql = @"
                SELECT vehicle_id, license_plate, brand, type, model, year, capacity, status,
                       last_maintenance, next_maintenance, created_at, updated_at
                FROM vehicles
                WHERE 1=1";

            if (!string.IsNullOrEmpty(status))
                sql += " AND status = :Status";

            sql += " ORDER BY license_plate";

            using var connection = _dbContext.CreateConnection();
            var results = await connection.QueryAsync<dynamic>(sql, new { Status = status });
            var vehicles = new List<Vehicle>();
            foreach (var r in results)
            {
                vehicles.Add(MapToVehicle(r));
            }
            return vehicles;
        }

        public async Task<IEnumerable<Vehicle>> GetAvailableAsync()
        {
            return await GetAllAsync(VehicleStatus.Available);
        }

        public async Task<IEnumerable<Vehicle>> GetAvailableForPeriodAsync(DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT v.vehicle_id, v.license_plate, v.brand, v.type, v.model, v.year, v.capacity, v.status,
                       v.last_maintenance, v.next_maintenance, v.created_at, v.updated_at
                FROM vehicles v
                WHERE v.status = 'AVAILABLE'
                  AND NOT EXISTS (
                      SELECT 1 FROM schedules s
                      INNER JOIN loan_requests lr ON s.loan_request_id = lr.loan_request_id
                      WHERE s.vehicle_id = v.vehicle_id
                        AND s.status NOT IN ('COMPLETED', 'CANCELLED')
                        AND lr.start_datetime < :EndTime
                        AND lr.end_datetime > :StartTime
                  )
                ORDER BY v.license_plate";

            using var connection = _dbContext.CreateConnection();
            var results = await connection.QueryAsync<dynamic>(sql, new { StartTime = start, EndTime = end });
            var vehicles = new List<Vehicle>();
            foreach (var r in results)
            {
                vehicles.Add(MapToVehicle(r));
            }
            return vehicles;
        }

        public async Task<int> CreateAsync(Vehicle vehicle)
        {
            const string sql = @"
                INSERT INTO vehicles (license_plate, brand, type, model, year, capacity, status, last_maintenance, next_maintenance)
                VALUES (:PlateNumber, :Brand, :Type, :Model, :Year, :Capacity, :Status, :LastMaintenance, :NextMaintenance)
                RETURNING vehicle_id INTO :Id";

            using var connection = _dbContext.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("PlateNumber", vehicle.PlateNumber);
            parameters.Add("Brand", vehicle.Brand);
            parameters.Add("Type", vehicle.Type);
            parameters.Add("Model", vehicle.Model);
            parameters.Add("Year", vehicle.Year);
            parameters.Add("Capacity", vehicle.Capacity);
            parameters.Add("Status", vehicle.Status);
            parameters.Add("LastMaintenance", vehicle.LastMaintenance);
            parameters.Add("NextMaintenance", vehicle.NextMaintenance);
            parameters.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

            await connection.ExecuteAsync(sql, parameters);
            return parameters.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(Vehicle vehicle)
        {
            const string sql = @"
                UPDATE vehicles
                SET license_plate = :PlateNumber, brand = :Brand, type = :Type, model = :Model,
                    year = :Year, capacity = :Capacity, status = :Status, 
                    last_maintenance = :LastMaintenance, next_maintenance = :NextMaintenance,
                    updated_at = SYSDATE
                WHERE vehicle_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                vehicle.Id,
                vehicle.PlateNumber,
                vehicle.Brand,
                vehicle.Type,
                vehicle.Model,
                vehicle.Year,
                vehicle.Capacity,
                vehicle.Status,
                vehicle.LastMaintenance,
                vehicle.NextMaintenance
            });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            const string sql = "UPDATE vehicles SET status = :Status, updated_at = SYSDATE WHERE vehicle_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, Status = status });
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM vehicles WHERE vehicle_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<int> GetAvailableCountAsync()
        {
            const string sql = "SELECT COUNT(1) FROM vehicles WHERE status = 'AVAILABLE'";

            using var connection = _dbContext.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql);
        }

        private Vehicle MapToVehicle(dynamic result)
        {
            return new Vehicle
            {
                Id = (int)result.VEHICLE_ID,
                PlateNumber = result.LICENSE_PLATE ?? string.Empty,
                Brand = result.BRAND ?? string.Empty,
                Type = result.TYPE ?? string.Empty,
                Model = result.MODEL,
                Year = result.YEAR != null ? (int?)result.YEAR : null,
                Capacity = result.CAPACITY != null ? (int)result.CAPACITY : 4,
                Status = result.STATUS ?? VehicleStatus.Available,
                LastMaintenance = result.LAST_MAINTENANCE,
                NextMaintenance = result.NEXT_MAINTENANCE,
                CreatedAt = result.CREATED_AT ?? DateTime.Now,
                UpdatedAt = result.UPDATED_AT ?? DateTime.Now
            };
        }
    }
}
