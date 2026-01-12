using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PelindoCarLoan.API.DTOs;
using PelindoCarLoan.API.Services;

namespace PelindoCarLoan.API.Controllers
{
    /// <summary>
    /// Controller for managing vehicles
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VehiclesController : BaseController
    {
        private readonly IVehicleService _vehicleService;
        private readonly ILogger<VehiclesController> _logger;

        public VehiclesController(IVehicleService vehicleService, ILogger<VehiclesController> logger)
        {
            _vehicleService = vehicleService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all vehicles
        /// </summary>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of vehicles</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VehicleDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var vehicles = await _vehicleService.GetAllAsync(status);
            return Ok(ApiResponse<IEnumerable<VehicleDto>>.SuccessResponse(vehicles));
        }

        /// <summary>
        /// Gets available vehicles
        /// </summary>
        /// <returns>List of available vehicles</returns>
        [HttpGet("available")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VehicleDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailable([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            // If dates provided, check for schedule conflicts
            if (startDate.HasValue && endDate.HasValue)
            {
                var vehicles = await _vehicleService.GetAvailableForPeriodAsync(startDate.Value, endDate.Value);
                return Ok(ApiResponse<IEnumerable<VehicleDto>>.SuccessResponse(vehicles));
            }
            
            // Otherwise just check status
            var availableVehicles = await _vehicleService.GetAvailableAsync();
            return Ok(ApiResponse<IEnumerable<VehicleDto>>.SuccessResponse(availableVehicles));
        }

        /// <summary>
        /// Gets a specific vehicle by ID
        /// </summary>
        /// <param name="id">Vehicle ID</param>
        /// <returns>Vehicle details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var vehicle = await _vehicleService.GetByIdAsync(id);
            
            if (vehicle == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Vehicle not found"));
            }

            return Ok(ApiResponse<VehicleDto>.SuccessResponse(vehicle));
        }

        /// <summary>
        /// Creates a new vehicle
        /// </summary>
        /// <param name="dto">Vehicle details</param>
        /// <returns>Created vehicle</returns>
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
        {
            var vehicle = await _vehicleService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = vehicle.Id },
                ApiResponse<VehicleDto>.SuccessResponse(vehicle, "Vehicle created successfully"));
        }

        /// <summary>
        /// Updates a vehicle
        /// </summary>
        /// <param name="id">Vehicle ID</param>
        /// <param name="dto">Updated vehicle details</param>
        /// <returns>Updated vehicle</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] CreateVehicleDto dto)
        {
            var vehicle = await _vehicleService.UpdateAsync(id, dto);
            
            if (vehicle == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Vehicle not found"));
            }

            return Ok(ApiResponse<VehicleDto>.SuccessResponse(vehicle, "Vehicle updated successfully"));
        }

        /// <summary>
        /// Updates vehicle status
        /// </summary>
        /// <param name="id">Vehicle ID</param>
        /// <param name="dto">Status update</param>
        /// <returns>Success status</returns>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateVehicleStatusDto dto)
        {
            var result = await _vehicleService.UpdateStatusAsync(id, dto);
            
            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Vehicle not found"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Vehicle status updated successfully"));
        }

        /// <summary>
        /// Deletes a vehicle (soft delete)
        /// </summary>
        /// <param name="id">Vehicle ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _vehicleService.DeleteAsync(id);
            
            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Vehicle not found"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Vehicle deleted successfully"));
        }

        /// <summary>
        /// Import vehicles from Excel file
        /// Format: PlateNumber | Brand | Type | Model | Capacity | Status
        /// </summary>
        [HttpPost("import")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ApiResponse<BulkImportResultDto>>> ImportFromExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("File Excel tidak boleh kosong"));
                }

                if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("File harus berformat .xlsx"));
                }

                using var stream = file.OpenReadStream();
                var result = await _vehicleService.ImportVehiclesFromExcelAsync(stream);

                return Ok(ApiResponse<BulkImportResultDto>.SuccessResponse(result,
                    $"Import selesai: {result.SuccessCount} berhasil, {result.FailedCount} gagal"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing vehicles from Excel");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Terjadi kesalahan saat mengimport vehicles dari Excel"));
            }
        }

        /// <summary>
        /// Download Excel template for bulk import
        /// </summary>
        [HttpGet("template")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                
                using var package = new OfficeOpenXml.ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Vehicles");

                // Header
                worksheet.Cells[1, 1].Value = "PlateNumber";
                worksheet.Cells[1, 2].Value = "Brand";
                worksheet.Cells[1, 3].Value = "Type";
                worksheet.Cells[1, 4].Value = "Model";
                worksheet.Cells[1, 5].Value = "Capacity";
                worksheet.Cells[1, 6].Value = "Status";

                // Sample data
                worksheet.Cells[2, 1].Value = "B 1234 XYZ";
                worksheet.Cells[2, 2].Value = "Toyota";
                worksheet.Cells[2, 3].Value = "Sedan";
                worksheet.Cells[2, 4].Value = "Camry 2023";
                worksheet.Cells[2, 5].Value = 5;
                worksheet.Cells[2, 6].Value = "AVAILABLE";

                worksheet.Cells[3, 1].Value = "B 5678 ABC";
                worksheet.Cells[3, 2].Value = "Mitsubishi";
                worksheet.Cells[3, 3].Value = "MPV";
                worksheet.Cells[3, 4].Value = "Xpander 2024";
                worksheet.Cells[3, 5].Value = 7;
                worksheet.Cells[3, 6].Value = "AVAILABLE";

                // Style header
                using (var range = worksheet.Cells[1, 1, 1, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                worksheet.Cells.AutoFitColumns();

                var bytes = package.GetAsByteArray();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "VehicleTemplate.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel template");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Terjadi kesalahan saat membuat template"));
            }
        }
    }

    /// <summary>
    /// Controller for managing drivers
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DriversController : BaseController
    {
        private readonly IDriverService _driverService;
        private readonly ILogger<DriversController> _logger;

        public DriversController(IDriverService driverService, ILogger<DriversController> logger)
        {
            _driverService = driverService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all drivers
        /// </summary>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of drivers</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DriverDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var drivers = await _driverService.GetAllAsync(status);
            return Ok(ApiResponse<IEnumerable<DriverDto>>.SuccessResponse(drivers));
        }

        /// <summary>
        /// Gets available drivers
        /// </summary>
        /// <returns>List of available drivers</returns>
        [HttpGet("available")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DriverDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailable([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            // If dates provided, check for schedule conflicts
            if (startDate.HasValue && endDate.HasValue)
            {
                var drivers = await _driverService.GetAvailableForPeriodAsync(startDate.Value, endDate.Value);
                return Ok(ApiResponse<IEnumerable<DriverDto>>.SuccessResponse(drivers));
            }
            
            // Otherwise just check status
            var availableDrivers = await _driverService.GetAvailableAsync();
            return Ok(ApiResponse<IEnumerable<DriverDto>>.SuccessResponse(availableDrivers));
        }

        /// <summary>
        /// Gets a specific driver by ID
        /// </summary>
        /// <param name="id">Driver ID</param>
        /// <returns>Driver details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<DriverDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var driver = await _driverService.GetByIdAsync(id);
            
            if (driver == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Driver not found"));
            }

            return Ok(ApiResponse<DriverDto>.SuccessResponse(driver));
        }

        /// <summary>
        /// Creates a new driver
        /// </summary>
        /// <param name="dto">Driver details</param>
        /// <returns>Created driver</returns>
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<DriverDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateDriverDto dto)
        {
            var driver = await _driverService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = driver.Id },
                ApiResponse<DriverDto>.SuccessResponse(driver, "Driver created successfully"));
        }

        /// <summary>
        /// Updates a driver
        /// </summary>
        /// <param name="id">Driver ID</param>
        /// <param name="dto">Updated driver details</param>
        /// <returns>Updated driver</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<DriverDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] CreateDriverDto dto)
        {
            var driver = await _driverService.UpdateAsync(id, dto);
            
            if (driver == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Driver not found"));
            }

            return Ok(ApiResponse<DriverDto>.SuccessResponse(driver, "Driver updated successfully"));
        }

        /// <summary>
        /// Updates driver status
        /// </summary>
        /// <param name="id">Driver ID</param>
        /// <param name="dto">Status update</param>
        /// <returns>Success status</returns>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDriverStatusDto dto)
        {
            var result = await _driverService.UpdateStatusAsync(id, dto);
            
            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Driver not found"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Driver status updated successfully"));
        }

        /// <summary>
        /// Deletes a driver (soft delete)
        /// </summary>
        /// <param name="id">Driver ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _driverService.DeleteAsync(id);
            
            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Driver not found"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Driver deleted successfully"));
        }
    }
}
