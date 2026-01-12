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
        Task<IEnumerable<VehicleDto>> GetAvailableForPeriodAsync(DateTime start, DateTime end);
        Task<VehicleDto> CreateAsync(CreateVehicleDto dto);
        Task<VehicleDto?> UpdateAsync(int id, CreateVehicleDto dto);
        Task<bool> UpdateStatusAsync(int id, UpdateVehicleStatusDto dto);
        Task<bool> DeleteAsync(int id);
        Task<BulkImportResultDto> ImportVehiclesFromExcelAsync(Stream fileStream);
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

        public async Task<IEnumerable<VehicleDto>> GetAvailableForPeriodAsync(DateTime start, DateTime end)
        {
            var vehicles = await _vehicleRepository.GetAvailableForPeriodAsync(start, end);
            return vehicles.Select(MapToDto);
        }

        public async Task<VehicleDto> CreateAsync(CreateVehicleDto dto)
        {
            var vehicle = new Vehicle
            {
                PlateNumber = dto.PlateNumber,
                Brand = dto.Brand,
                Type = dto.Type,
                Model = dto.Model,
                Year = dto.Year,
                Capacity = dto.Capacity,
                Status = VehicleStatus.Available
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
            vehicle.Model = dto.Model;
            vehicle.Year = dto.Year;
            vehicle.Capacity = dto.Capacity;

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
                Model = vehicle.Model,
                Year = vehicle.Year,
                Capacity = vehicle.Capacity,
                Status = vehicle.Status,
                LastMaintenance = vehicle.LastMaintenance,
                NextMaintenance = vehicle.NextMaintenance
            };
        }

        public async Task<BulkImportResultDto> ImportVehiclesFromExcelAsync(Stream fileStream)
        {
            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            var result = new BulkImportResultDto
            {
                SuccessCount = 0,
                FailedCount = 0,
                Errors = new List<BulkImportErrorDto>()
            };

            using var package = new OfficeOpenXml.ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount < 2)
            {
                throw new InvalidOperationException("File Excel kosong atau tidak memiliki data");
            }

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var plateNumber = worksheet.Cells[row, 1].Text?.Trim();
                    var brand = worksheet.Cells[row, 2].Text?.Trim();
                    var type = worksheet.Cells[row, 3].Text?.Trim();
                    var model = worksheet.Cells[row, 4].Text?.Trim();
                    var capacityText = worksheet.Cells[row, 5].Text?.Trim();
                    var status = worksheet.Cells[row, 6].Text?.Trim();

                    // Validation
                    if (string.IsNullOrWhiteSpace(plateNumber))
                    {
                        result.Errors.Add(new BulkImportErrorDto
                        {
                            RowNumber = row,
                            Email = plateNumber ?? "",
                            ErrorMessage = "Nomor Plat tidak boleh kosong"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(brand))
                    {
                        result.Errors.Add(new BulkImportErrorDto
                        {
                            RowNumber = row,
                            Email = plateNumber,
                            ErrorMessage = "Merek tidak boleh kosong"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(type))
                    {
                        result.Errors.Add(new BulkImportErrorDto
                        {
                            RowNumber = row,
                            Email = plateNumber,
                            ErrorMessage = "Tipe tidak boleh kosong"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(model))
                    {
                        result.Errors.Add(new BulkImportErrorDto
                        {
                            RowNumber = row,
                            Email = plateNumber,
                            ErrorMessage = "Model tidak boleh kosong"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    if (!int.TryParse(capacityText, out int capacity) || capacity < 1)
                    {
                        result.Errors.Add(new BulkImportErrorDto
                        {
                            RowNumber = row,
                            Email = plateNumber,
                            ErrorMessage = "Kapasitas harus berupa angka minimal 1"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    // Check if plate number already exists
                    var existingVehicles = await _vehicleRepository.GetAllAsync();
                    if (existingVehicles.Any(v => v.PlateNumber.Equals(plateNumber, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.Errors.Add(new BulkImportErrorDto
                        {
                            RowNumber = row,
                            Email = plateNumber,
                            ErrorMessage = $"Nomor Plat '{plateNumber}' sudah terdaftar"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    // Set default status if empty
                    if (string.IsNullOrWhiteSpace(status))
                    {
                        status = VehicleStatus.Available;
                    }

                    // Validate status
                    if (!VehicleStatus.AllStatuses.Contains(status))
                    {
                        result.Errors.Add(new BulkImportErrorDto
                        {
                            RowNumber = row,
                            Email = plateNumber,
                            ErrorMessage = $"Status '{status}' tidak valid. Gunakan: {string.Join(", ", VehicleStatus.AllStatuses)}"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    // Create vehicle
                    var vehicle = new Vehicle
                    {
                        PlateNumber = plateNumber,
                        Brand = brand,
                        Type = type,
                        Model = model,
                        Year = DateTime.Now.Year, // Default to current year
                        Capacity = capacity,
                        Status = status
                    };

                    await _vehicleRepository.CreateAsync(vehicle);
                    result.SuccessCount++;

                    _logger.LogInformation("Vehicle imported: {PlateNumber}", plateNumber);
                }
                catch (Exception ex)
                {
                    var plateNumber = worksheet.Cells[row, 1].Text?.Trim() ?? "";
                    result.Errors.Add(new BulkImportErrorDto
                    {
                        RowNumber = row,
                        Email = plateNumber,
                        ErrorMessage = ex.Message
                    });
                    result.FailedCount++;
                    _logger.LogError(ex, "Error importing vehicle at row {Row}", row);
                }
            }

            return result;
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
        Task<IEnumerable<DriverDto>> GetAvailableForPeriodAsync(DateTime start, DateTime end);
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

        public async Task<IEnumerable<DriverDto>> GetAvailableForPeriodAsync(DateTime start, DateTime end)
        {
            var drivers = await _driverRepository.GetAvailableForPeriodAsync(start, end);
            return drivers.Select(MapToDto);
        }

        public async Task<DriverDto> CreateAsync(CreateDriverDto dto)
        {
            var driver = new Driver
            {
                UserId = dto.UserId,
                LicenseNumber = dto.LicenseNumber,
                LicenseExpiry = dto.LicenseExpiry,
                ExperienceYears = dto.ExperienceYears,
                Status = DriverStatus.Available
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
            driver.ExperienceYears = dto.ExperienceYears;

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
                PhoneNumber = driver.User?.PhoneNumber,
                LicenseNumber = driver.LicenseNumber,
                LicenseExpiry = driver.LicenseExpiry,
                Status = driver.Status,
                ExperienceYears = driver.ExperienceYears,
                Rating = driver.Rating
            };
        }
    }
}
