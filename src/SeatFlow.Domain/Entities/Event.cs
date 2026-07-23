using SeatFlow.Domain.Common;
using SeatFlow.Domain.Enums;

namespace SeatFlow.Domain.Entities;

public sealed class Event : Entity
{
    private Event()
    {
    }

    public Event(
        Guid id,
        string title,
        string? description,
        EventCategory category,
        int ageRestriction)
        : base(id)
    {
        Update(
            title,
            description,
            category,
            ageRestriction);
    }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public EventCategory Category { get; private set; }

    public int AgeRestriction { get; private set; }

    public void Update(
        string title,
        string? description,
        EventCategory category,
        int ageRestriction)
    {
        Title = Guard.RequiredText(
            title,
            nameof(Title),
            maxLength: 250);

        Description = Guard.OptionalText(
            description,
            nameof(Description),
            maxLength: 4000);

        Category = Guard.DefinedEnum(
            category,
            nameof(Category));

        AgeRestriction = Guard.NumberInRange(
            ageRestriction,
            nameof(AgeRestriction),
            minimum: 0,
            maximum: 21);
    }
}