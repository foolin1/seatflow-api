using System.Text;

namespace SeatFlow.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException(
                "JWT issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException(
                "JWT audience is not configured.");
        }

        if (string.IsNullOrWhiteSpace(SigningKey))
        {
            throw new InvalidOperationException(
                "JWT signing key is not configured.");
        }

        if (Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 bytes.");
        }

        if (AccessTokenMinutes is < 1 or > 1440)
        {
            throw new InvalidOperationException(
                "JWT access token lifetime must be between " +
                "1 and 1440 minutes.");
        }

        if (RefreshTokenDays is < 1 or > 90)
        {
            throw new InvalidOperationException(
                "Refresh token lifetime must be between " +
                "1 and 90 days.");
        }
    }
}