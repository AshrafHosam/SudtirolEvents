using Microsoft.AspNetCore.Mvc;
using OpenDataHubAssistant.Core.DTOs;
using OpenDataHubAssistant.Core.Interfaces;

namespace OpenDataHubAssistant.Api.Controllers;

/// <summary>
/// Controller for location-related operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly ILocationRepository _locationRepository;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        ILocationRepository locationRepository,
        ILogger<LocationsController> logger)
    {
        _locationRepository = locationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all available locations
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LocationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LocationDto>>> GetLocations()
    {
        _logger.LogInformation("Getting all locations");

        var locations = await _locationRepository.GetAllAsync();

        var result = locations.Select(l => new LocationDto
        {
            Id = l.Id,
            Name = l.Name,
            Latitude = l.Latitude,
            Longitude = l.Longitude
        });

        return Ok(result);
    }

    /// <summary>
    /// Get a specific location by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationDto>> GetLocation(int id)
    {
        var location = await _locationRepository.GetByIdAsync(id);

        if (location == null)
        {
            return NotFound(new { message = $"Location with ID {id} not found" });
        }

        return Ok(new LocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Latitude = location.Latitude,
            Longitude = location.Longitude
        });
    }

    /// <summary>
    /// Get a location by name
    /// </summary>
    [HttpGet("byname/{name}")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationDto>> GetLocationByName(string name)
    {
        var location = await _locationRepository.GetByNameAsync(name);

        if (location == null)
        {
            return NotFound(new { message = $"Location '{name}' not found" });
        }

        return Ok(new LocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Latitude = location.Latitude,
            Longitude = location.Longitude
        });
    }
}
