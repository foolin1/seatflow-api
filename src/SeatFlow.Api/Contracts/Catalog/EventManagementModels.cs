using System.ComponentModel.DataAnnotations;
using SeatFlow.Domain.Enums;

namespace SeatFlow.Api.Contracts.Catalog;

public sealed class CreateVenueRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Address { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }
}

public sealed class UpdateVenueRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Address { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }
}

public sealed class CreateHallRequest
{
    public Guid VenueId { get; init; }

    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }
}

public sealed class UpdateHallRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }
}

public sealed class CreateSeatRequest
{
    public Guid HallId { get; init; }

    [Required]
    [MaxLength(20)]
    public string RowLabel { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Number { get; init; }

    public SeatCategory Category { get; init; }
}

public sealed class UpdateSeatRequest
{
    [Required]
    [MaxLength(20)]
    public string RowLabel { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Number { get; init; }

    public SeatCategory Category { get; init; }
}

public sealed class CreateEventRequest
{
    [Required]
    [MaxLength(250)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }

    public EventCategory Category { get; init; }

    [Range(0, 21)]
    public int AgeRestriction { get; init; }
}

public sealed class UpdateEventRequest
{
    [Required]
    [MaxLength(250)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }

    public EventCategory Category { get; init; }

    [Range(0, 21)]
    public int AgeRestriction { get; init; }
}

public sealed class CreateEventSessionRequest
{
    public Guid EventId { get; init; }

    public Guid HallId { get; init; }

    public DateTimeOffset StartsAtUtc { get; init; }

    public DateTimeOffset BookingOpensAtUtc { get; init; }

    public DateTimeOffset BookingClosesAtUtc { get; init; }

    [Range(
        typeof(decimal),
        "0.01",
        "99999999.99",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal DefaultPrice { get; init; }
}

public sealed class UpdateEventSessionRequest
{
    public DateTimeOffset StartsAtUtc { get; init; }

    public DateTimeOffset BookingOpensAtUtc { get; init; }

    public DateTimeOffset BookingClosesAtUtc { get; init; }
}