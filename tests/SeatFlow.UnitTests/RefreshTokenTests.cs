using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.UnitTests;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(
            2026,
            8,
            1,
            12,
            0,
            0,
            TimeSpan.Zero);

    private static readonly DateTimeOffset ExpiresAt =
        CreatedAt.AddDays(7);

    [Fact]
    public void IsActiveAt_BeforeExpiration_ReturnsTrue()
    {
        var token = CreateToken();

        var result = token.IsActiveAt(
            CreatedAt.AddDays(1));

        Assert.True(result);
    }

    [Fact]
    public void IsActiveAt_WhenExpirationReached_ReturnsFalse()
    {
        var token = CreateToken();

        var result = token.IsActiveAt(ExpiresAt);

        Assert.False(result);
    }

    [Fact]
    public void Revoke_MarksTokenAsRevoked()
    {
        var token = CreateToken();
        var replacementTokenId = Guid.NewGuid();
        var revokedAt = CreatedAt.AddHours(1);

        token.Revoke(
            revokedAt,
            replacementTokenId);

        Assert.Equal(
            revokedAt,
            token.RevokedAtUtc);

        Assert.Equal(
            replacementTokenId,
            token.ReplacedByTokenId);

        Assert.False(
            token.IsActiveAt(
                CreatedAt.AddHours(2)));
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsConflict()
    {
        var token = CreateToken();

        token.Revoke(
            CreatedAt.AddHours(1));

        Assert.Throws<DomainConflictException>(
            () => token.Revoke(
                CreatedAt.AddHours(2)));
    }

    [Fact]
    public void Constructor_WithInvalidExpiration_ThrowsValidation()
    {
        Assert.Throws<DomainValidationException>(
            () => new RefreshToken(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "token-hash",
                CreatedAt,
                CreatedAt));
    }

    private static RefreshToken CreateToken()
    {
        return new RefreshToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "refresh-token-hash",
            CreatedAt,
            ExpiresAt);
    }
}