using Microsoft.AspNetCore.Mvc;

namespace SeatFlow.Api.Controllers;

[ApiController]
[Route("api/version")]
public sealed class VersionController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(VersionResponse), StatusCodes.Status200OK)]
    public ActionResult<VersionResponse> GetVersion()
    {
        var assemblyName = typeof(Program).Assembly.GetName();

        var response = new VersionResponse(
            assemblyName.Name ?? "SeatFlow.Api",
            assemblyName.Version?.ToString() ?? "Unknown");

        return Ok(response);
    }
}

public sealed record VersionResponse(
    string Application,
    string Version);