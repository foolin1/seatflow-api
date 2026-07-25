using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatFlow.Domain.Enums;

namespace SeatFlow.Api.Controllers;

[ApiController]
[Route("api/access")]
public sealed class AccessController
    : ControllerBase
{
    private const string SubjectClaim = "sub";

    [Authorize]
    [HttpGet("user")]
    public IActionResult UserAccess()
    {
        return Ok(
            new
            {
                message =
                    "Authenticated user access granted.",
                userId =
                    User.FindFirstValue(SubjectClaim),
                name =
                    User.Identity?.Name
            });
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("admin")]
    public IActionResult AdminAccess()
    {
        return Ok(
            new
            {
                message =
                    "Administrator access granted.",
                userId =
                    User.FindFirstValue(SubjectClaim),
                name =
                    User.Identity?.Name,
                role =
                    User.FindFirstValue("role")
            });
    }
}