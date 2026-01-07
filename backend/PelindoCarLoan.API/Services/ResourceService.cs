using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Models;
using PelindoCarLoan.API.Repositories;

namespace PelindoCarLoan.API.Services
{
    /// <summary>
    /// Service interface for vehicle operations
    /// </summary>
    public interface IVehicleService
    {
        Task<VehicleDto?> GetByIdAsync(int id);
        Task<IEnumerable<VehicleDto>> GetAllAsync(string? status = null);
        Task<IEnumerable<VehicleDto>> GetAvailableAsync();
        Task<VehicleDto> CreateAsync(CreateVehicleDto dto);
        Task<VehicleDto?> UpdateAsync(int id, CreateVehicleDto dto);
        Task<bool> UpdateStatusAsync(int id, UpdateVehicleStatusDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ILogger<VehicleService> _logger;

        public VehicleService(IVehicleRepository vehicleRepository, ILogger<VehicleService> logger)
        {
            _vehicleRepository = vehicleRepository;
            _logger = logger;
        }

        public async Task<VehicleDto?> GetByIdAsync(int id)
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id);
            return vehicle != null ? MapToDto(vehicle) : null;
        }

        public async Task<IEnumerable<VehicleDto>> GetAllAsync(string? status = null)
        {
            var vehicles = await _vehicleRepository.GetAllAsync(status);
            return vehicles.Select(MapToDto);
        }

        public async Task<IEnumerable<VehicleDto>> GetAvailableAsync()
        {
            var vehicles = await _vehicleRepository.GetAvailableAsync();
            return vehicles.Select(MapToDto);
        }

        public async Task<VehicleDto> CreateAsync(CreateVehicleDto dto)
        {
            var vehicle = new Vehicle
            {
                PlateNumber = dto.PlateNumber,
                Brand = dto.Brand,
                Type = dto.Type,
                Capacity = dto.Capacity,
                Status = VehicleStatus.Available,
                Notes = dto.Notes,
                IsActive = true
            };

            var id = await _vehicleRepository.CreateAsync(vehicle);
            vehicle.Id = id;

            _logger.LogInformation("Vehicle created: {PlateNumber}", dto.PlateNumber);

            return MapToDto(vehicle);
        }

        public async Task<VehicleDto?> UpdateAsync(int id, CreateVehicleDto dto)
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id);
            if (vehicle == null) return null;

            vehicle.PlateNumber = dto.PlateNumber;
            vehicle.Brand = dto.Brand;
            vehicle.Type = dto.Type;
            vehicle.Capacity = dto.Capacity;
            vehicle.Notes = dto.Notes;

            await _vehicleRepository.UpdateAsync(vehicle);

            _logger.LogInformation("Vehicle updated: {PlateNumber}", dto.PlateNumber);

            return MapToDto(vehicle);
        }

        public async Task<bool> UpdateStatusAsync(int id, UpdateVehicleStatusDto dto)
        {
            if (!VehicleStatus.AllStatuses.Contains(dto.Status))
            {
                throw new ArgumentException($"Invalid status. Must be one of: {string.Join(", ", VehicleStatus.AllStatuses)}");
            }

            var result = await _vehicleRepository.UpdateStatusAsync(id, dto.Status);
            
            if (result)
            {
                _logger.LogInformation("Vehicle {VehicleId} status updated to {Status}", id, dto.Status);
            }

            return result;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _vehicleRepository.DeleteAsync(id);
            
            if (result)
            {
                _logger.LogInformation("Vehicle {VehicleId} deleted (soft delete)", id);
            }

            return result;
        }

        private static VehicleDto MapToDto(Vehicle vehicle)
        {
            return new VehicleDto
            {
                Id = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                Brand = vehicle.Brand,
                Type = vehicle.Type,
                Capacity = vehicle.Capacity,
                Status = vehicle.Status,
                Notes = vehicle.Notes,
                IsActive = vehicle.IsActive
            };
        }
    }

    /// <summary>
    /// Service interface for driver operations
    /// </summary>
    public interface IDriverService
    {
        Task<DriverDto?> GetByIdAsync(int id);
        Task<IEnumerable<DriverDto>> GetAllAsync(string? status = null);
        Task<IEnumerable<DriverDto>> GetAvailableAsync();
        Task<DriverDto> CreateAsync(CreateDriverDto dto);
        Task<DriverDto?> UpdateAsync(int id, CreateDriverDto dto);
        Task<bool> UpdateStatusAsync(int id, UpdateDriverStatusDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class DriverService : IDriverService
    {
        private readonly IDriverRepository _driverRepository;
        private readonly ILogger<DriverService> _logger;

        public DriverService(IDriverRepository driverRepository, ILogger<DriverService> logger)
        {
            _driverRepository = driverRepository;
            _logger = logger;
        }

        public async Task<DriverDto?> GetByIdAsync(int id)
        {
            var driver = await _driverRepository.GetByIdAsync(id);
            return driver != null ? MapToDto(driver) : null;
        }

        public async Task<IEnumerable<DriverDto>> GetAllAsync(string? status = null)
        {
            var drivers = await _driverRepository.GetAllAsync(status);
            return drivers.Select(MapToDto);
        }

        public async Task<IEnumerable<DriverDto>> GetAvailableAsync()
        {
            var drivers = await _driverRepository.GetAvailableAsync();
            return drivers.Select(MapToDto);
        }

        public async Task<DriverDto> CreateAsync(CreateDriverDto dto)
        {
            var driver = new Driver
            {
                UserId = dto.UserId,
                LicenseNumber = dto.LicenseNumber,
                LicenseExpiry = dto.LicenseExpiry,
                PhoneNumber = dto.PhoneNumber,
                Status = DriverStatus.Available,
                IsActive = true
            };

            var id = await _driverRepository.CreateAsync(driver);
            driver.Id = id;

            _logger.LogInformation("Driver created: {LicenseNumber}", dto.LicenseNumber);

            return MapToDto(driver);
        }

        public async Task<DriverDto?> UpdateAsync(int id, CreateDriverDto dto)
        {
            var driver = await _driverRepository.GetByIdAsync(id);
            if (driver == null) return null;

            driver.LicenseNumber = dto.LicenseNumber;
            driver.LicenseExpiry = dto.LicenseExpiry;
            driver.PhoneNumber = dto.PhoneNumber;

            await _driverRepository.UpdateAsync(driver);

            _logger.LogInformation("Driver updated: {LicenseNumber}", dto.LicenseNumber);

            return MapToDto(driver);
        }

        public async Task<bool> UpdateStatusAsync(int id, UpdateDriverStatusDto dto)
        {
            if (!DriverStatus.AllStatuses.Contains(dto.Status))
            {
                throw new ArgumentException($"Invalid status. Must be one of: {string.Join(", ", DriverStatus.AllStatuses)}");
            }

            var result = await _driverRepository.UpdateStatusAsync(id, dto.Status);
            
            if (result)
            {
                _logger.LogInformation("Driver {DriverId} status updated to {Status}", id, dto.Status);
            }

            return result;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _driverRepository.DeleteAsync(id);
            
            if (result)
            {
                _logger.LogInformation("Driver {DriverId} deleted (soft delete)", id);
            }

            return result;
        }

        private static DriverDto MapToDto(Driver driver)
        {
            return new DriverDto
            {
                Id = driver.Id,
                UserId = driver.UserId,
                DriverName = driver.User?.Name,
                LicenseNumber = driver.LicenseNumber,
                LicenseExpiry = driver.LicenseExpiry,
                PhoneNumber = driver.PhoneNumber,
                Status = driver.Status,
                IsActive = driver.IsActive
            };
        }
    }
}
