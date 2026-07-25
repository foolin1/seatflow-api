using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.UnitTests;

public sealed class UserTests
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

    [Fact]
    public void Constructor_NormalizesEmail()
    {
        var user = CreateUser(
            email: "  USER@Example.COM  ");

        Assert.Equal(
            "user@example.com",
            user.Email);

        Assert.True(user.IsActive);
        Assert.Equal(UserRole.User, user.Role);
    }

    [Fact]
    public void Constructor_WithInvalidEmail_ThrowsValidation()
    {
        Assert.Throws<DomainValidationException>(
            () => CreateUser(
                email: "invalid-email"));
    }

    [Fact]
    public void ChangePasswordHash_ReplacesStoredHash()
    {
        var user = CreateUser();

        user.ChangePasswordHash(
            "new-password-hash");

        Assert.Equal(
            "new-password-hash",
            user.PasswordHash);
    }

    [Fact]
    public void ChangeRole_ToAdmin_UpdatesRole()
    {
        var user = CreateUser();

        user.ChangeRole(UserRole.Admin);

        Assert.Equal(
            UserRole.Admin,
            user.Role);
    }

    [Fact]
    public void Deactivate_MarksUserAsInactive()
    {
        var user = CreateUser();

        user.Deactivate();

        Assert.False(user.IsActive);
    }

    private static User CreateUser(
        string email = "user@example.com")
    {
        return new User(
            Guid.NewGuid(),
            email,
            "password-hash",
            "Test User",
            UserRole.User,
            CreatedAt);
    }
}