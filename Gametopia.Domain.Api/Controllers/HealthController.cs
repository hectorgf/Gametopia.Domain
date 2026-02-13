using Microsoft.AspNetCore.Mvc;

namespace Gametopia.Domain.Api.Controllers;

/// <summary>
/// Health check endpoint for Kubernetes liveness and readiness probes
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Liveness probe - indicates if the application is running
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        _logger.LogDebug("Health check requested");
        
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = GetApplicationVersion()
        });
    }

    /// <summary>
    /// Readiness probe - indicates if the application is ready to serve requests
    /// </summary>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> IsReadyAsync()
    {
        _logger.LogDebug("Readiness check requested");
        
        try
        {
            // Add any readiness checks here (database connectivity, etc.)
            // await _dbContext.Database.ExecuteScalarAsync("SELECT 1");
            
            return Ok(new
            {
                status = "ready",
                timestamp = DateTime.UtcNow,
                version = GetApplicationVersion()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "not_ready",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get application version and environment info
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetInfo([FromServices] IWebHostEnvironment env)
    {
        return Ok(new
        {
            environment = env.EnvironmentName,
            version = GetApplicationVersion(),
            timestamp = DateTime.UtcNow
        });
    }

    private static string GetApplicationVersion()
    {
        var version = typeof(Program).Assembly.GetName().Version;
        return version?.ToString() ?? "0.0.0";
    }
}
