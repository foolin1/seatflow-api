using SeatFlow.Domain.Exceptions;

namespace SeatFlow.Infrastructure.Authentication;

internal static class PasswordPolicy
{
    private const int MinimumLength = 8;
    private const int MaximumLength = 128;

    public static void Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new DomainValidationException(
                "Password is required.");
        }

        if (password.Length < MinimumLength)
        {
            throw new DomainValidationException(
                $"Password must contain at least " +
                $"{MinimumLength} characters.");
        }

        if (password.Length > MaximumLength)
        {
            throw new DomainValidationException(
                $"Password cannot exceed " +
                $"{MaximumLength} characters.");
        }

        if (password.Any(char.IsWhiteSpace))
        {
            throw new DomainValidationException(
                "Password cannot contain whitespace.");
        }

        if (!password.Any(char.IsUpper))
        {
            throw new DomainValidationException(
                "Password must contain an uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            throw new DomainValidationException(
                "Password must contain a lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            throw new DomainValidationException(
                "Password must contain a digit.");
        }

        if (!password.Any(
                character => !char.IsLetterOrDigit(character)))
        {
            throw new DomainValidationException(
                "Password must contain a special character.");
        }
    }
}