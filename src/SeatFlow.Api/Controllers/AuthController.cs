using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeatFlow.Api.Contracts.Authentication;
using SeatFlow.Application.Authentication;

namespace SeatFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController
    : ControllerBase
{
    private const string SubjectClaim = "sub";

    private readonly IAuthenticationService
        _authenticationService;

    public AuthController(
        IAuthenticationService authenticationService)
    {
        _authenticationService =
            authenticationService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResponse>>
        Register(
            RegisterRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _authenticationService.RegisterAsync(
                request.Email,
                request.Password,
                request.FullName,
                cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            AuthenticationResponse.From(result));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponse>>
        Login(
            LoginRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _authenticationService.LoginAsync(
                request.Email,
                request.Password,
                cancellationToken);

        return Ok(
            AuthenticationResponse.From(result));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthenticationResponse>>
        Refresh(
            RefreshTokenRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _authenticationService.RefreshAsync(
                request.RefreshToken,
                cancellationToken);

        return Ok(
            AuthenticationResponse.From(result));
    }

    [AllowAnonymous]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(
        RevokeTokenRequest request,
        CancellationToken cancellationToken)
    {
        await _authenticationService.RevokeAsync(
            request.RefreshToken,
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthenticatedUserResponse>>
        Me(CancellationToken cancellationToken)
    {
        var subject =
            User.FindFirstValue(SubjectClaim);

        if (!Guid.TryParse(
                subject,
                out var userId))
        {
            return Unauthorized();
        }

        var user =
            await _authenticationService.GetUserAsync(
                userId,
                cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(
            AuthenticatedUserResponse.From(user));
    }
}