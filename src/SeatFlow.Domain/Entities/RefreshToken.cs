using SeatFlow.Domain.Common;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.Domain.Entities;

public sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    public RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
        : base(id)
    {
        Guard.AgainstEmpty(
            userId,
            nameof(userId));

        var normalizedCreatedAt =
            createdAtUtc.ToUniversalTime();

        var normalizedExpiresAt =
            expiresAtUtc.ToUniversalTime();

        if (normalizedExpiresAt <= normalizedCreatedAt)
        {
            throw new DomainValidationException(
                "Refresh token expiration time must be " +
                "after creation time.");
        }

        UserId = userId;

        TokenHash = Guard.RequiredText(
            tokenHash,
            nameof(TokenHash),
            maxLength: 128);

        CreatedAtUtc = normalizedCreatedAt;
        ExpiresAtUtc = normalizedExpiresAt;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActiveAt(DateTimeOffset currentTimeUtc)
    {
        var normalizedCurrentTime =
            currentTimeUtc.ToUniversalTime();

        return RevokedAtUtc is null &&
               normalizedCurrentTime < ExpiresAtUtc;
    }

    public void Revoke(
        DateTimeOffset revokedAtUtc,
        Guid? replacedByTokenId = null)
    {
        if (RevokedAtUtc is not null)
        {
            throw new DomainConflictException(
                $"Refresh token '{Id}' has already been revoked.");
        }

        var normalizedRevokedAt =
            revokedAtUtc.ToUniversalTime();

        if (normalizedRevokedAt < CreatedAtUtc)
        {
            throw new DomainValidationException(
                "Refresh token revocation time cannot be " +
                "before creation time.");
        }

        if (replacedByTokenId.HasValue)
        {
            Guard.AgainstEmpty(
                replacedByTokenId.Value,
                nameof(replacedByTokenId));

            if (replacedByTokenId.Value == Id)
            {
                throw new DomainValidationException(
                    "A refresh token cannot replace itself.");
            }
        }

        RevokedAtUtc = normalizedRevokedAt;
        ReplacedByTokenId = replacedByTokenId;
    }
}