using SeatFlow.Domain.Exceptions;

namespace SeatFlow.Domain.Common;

internal static class Guard
{
    public static void AgainstEmpty(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException(
                $"{fieldName} cannot be empty.");
        }
    }

    public static string RequiredText(
        string? value,
        string fieldName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(
                $"{fieldName} is required.");
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new DomainValidationException(
                $"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalizedValue;
    }

    public static string? OptionalText(
        string? value,
        string fieldName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new DomainValidationException(
                $"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalizedValue;
    }

    public static int PositiveNumber(
        int value,
        string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainValidationException(
                $"{fieldName} must be greater than zero.");
        }

        return value;
    }

    public static int NumberInRange(
        int value,
        string fieldName,
        int minimum,
        int maximum)
    {
        if (value < minimum || value > maximum)
        {
            throw new DomainValidationException(
                $"{fieldName} must be between {minimum} and {maximum}.");
        }

        return value;
    }

    public static decimal PositiveMoney(
        decimal value,
        string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainValidationException(
                $"{fieldName} must be greater than zero.");
        }

        return decimal.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    public static TEnum DefinedEnum<TEnum>(
        TEnum value,
        string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainValidationException(
                $"{fieldName} contains an unsupported value.");
        }

        return value;
    }
}