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
        public async Task<IActionResult> GetAvailable()
        {
            var vehicles = await _vehicleService.GetAvailableAsync();
            return Ok(ApiResponse<IEnumerable<VehicleDto>>.SuccessResponse(vehicles));
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
        public async Task<IActionResult> GetAvailable()
        {
            var drivers = await _driverService.GetAvailableAsync();
            return Ok(ApiResponse<IEnumerable<DriverDto>>.SuccessResponse(drivers));
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
