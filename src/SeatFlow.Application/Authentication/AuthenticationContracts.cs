using SeatFlow.Domain.Enums;

namespace SeatFlow.Application.Authentication;

public sealed record AuthenticatedUser(
    Guid Id,
    string Email,
    string FullName,
    UserRole Role);

public sealed record AuthenticationResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    AuthenticatedUser User);

public interface IAuthenticationService
{
    Task<AuthenticationResult> RegisterAsync(
        string email,
        string password,
        string fullName,
        CancellationToken cancellationToken);

    Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<AuthenticatedUser?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken);
}