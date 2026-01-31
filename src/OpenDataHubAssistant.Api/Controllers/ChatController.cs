using Microsoft.AspNetCore.Mvc;
using OpenDataHubAssistant.Core.DTOs;
using OpenDataHubAssistant.Core.Interfaces;
using OpenDataHubAssistant.Core.Models;

namespace OpenDataHubAssistant.Api.Controllers;

/// <summary>
/// Controller for chatbot-style interactions
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILlmService _llmService;
    private readonly IRecommendationLogRepository _logRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        ILlmService llmService,
        IRecommendationLogRepository logRepository,
        ILocationRepository locationRepository,
        ILogger<ChatController> logger)
    {
        _llmService = llmService;
        _logRepository = logRepository;
        _locationRepository = locationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat message and get AI-powered recommendations
    /// </summary>
    /// <param name="request">Chat request with user message</param>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ChatResponseDto>> Chat([FromBody] ChatRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message is required" });
        }

        _logger.LogInformation("Processing chat message: {Message}", request.Message);

        try
        {
            var response = await _llmService.ProcessChatMessageAsync(request.Message);

            // Log the interaction
            if (response.Data?.Weather != null)
            {
                var location = await _locationRepository.GetByNameAsync(response.Data.Weather.LocationName);
                await _logRepository.AddAsync(new RecommendationLog
                {
                    LocationId = location?.Id,
                    Timestamp = DateTime.UtcNow,
                    QueryText = request.Message,
                    RecommendationText = response.Response,
                    SourceDataSummary = response.Data != null
                        ? $"Weather: {response.Data.Weather?.ConditionText}, Recommendations: {response.Data.Recommendations.Count}"
                        : null
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return Problem(
                detail: "An error occurred while processing your message",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Check if the LLM service is available
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetStatus()
    {
        var isAvailable = await _llmService.IsAvailableAsync();
        return Ok(new
        {
            llmAvailable = isAvailable,
            message = isAvailable
                ? "AI-powered responses are enabled"
                : "AI-powered responses are unavailable. Using rule-based responses."
        });
    }
}
