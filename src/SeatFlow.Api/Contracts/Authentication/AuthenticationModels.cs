using System.ComponentModel.DataAnnotations;
using SeatFlow.Application.Authentication;

namespace SeatFlow.Api.Contracts.Authentication;

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class RevokeTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed record AuthenticatedUserResponse(
    Guid Id,
    string Email,
    string FullName,
    string Role)
{
    public static AuthenticatedUserResponse From(
        AuthenticatedUser user)
    {
        return new AuthenticatedUserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString());
    }
}

public sealed record AuthenticationResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    AuthenticatedUserResponse User)
{
    public static AuthenticationResponse From(
        AuthenticationResult result)
    {
        return new AuthenticationResponse(
            result.AccessToken,
            result.AccessTokenExpiresAtUtc,
            result.RefreshToken,
            result.RefreshTokenExpiresAtUtc,
            AuthenticatedUserResponse.From(
                result.User));
    }
}