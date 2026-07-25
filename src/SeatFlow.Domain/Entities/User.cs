using System.Net.Mail;
using SeatFlow.Domain.Common;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.Domain.Entities;

public sealed class User : Entity
{
    private User()
    {
    }

    public User(
        Guid id,
        string email,
        string passwordHash,
        string fullName,
        UserRole role,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Email = NormalizeEmail(email);

        PasswordHash = Guard.RequiredText(
            passwordHash,
            nameof(PasswordHash),
            maxLength: 1000);

        FullName = Guard.RequiredText(
            fullName,
            nameof(FullName),
            maxLength: 200);

        Role = Guard.DefinedEnum(
            role,
            nameof(Role));

        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        IsActive = true;
    }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsActive { get; private set; }

    public static string NormalizeEmail(string email)
    {
        var normalizedEmail = Guard.RequiredText(
                email,
                nameof(Email),
                maxLength: 320)
            .ToLowerInvariant();

        if (!MailAddress.TryCreate(
                normalizedEmail,
                out var parsedAddress) ||
            !string.Equals(
                parsedAddress.Address,
                normalizedEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException(
                "Email has an invalid format.");
        }

        return normalizedEmail;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        PasswordHash = Guard.RequiredText(
            passwordHash,
            nameof(passwordHash),
            maxLength: 1000);
    }

    public void ChangeFullName(string fullName)
    {
        FullName = Guard.RequiredText(
            fullName,
            nameof(fullName),
            maxLength: 200);
    }

    public void ChangeRole(UserRole role)
    {
        Role = Guard.DefinedEnum(
            role,
            nameof(role));
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}