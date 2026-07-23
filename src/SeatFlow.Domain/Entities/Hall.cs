using SeatFlow.Domain.Common;

namespace SeatFlow.Domain.Entities;

public sealed class Hall : Entity
{
    private Hall()
    {
    }

    public Hall(
        Guid id,
        Guid venueId,
        string name,
        int capacity)
        : base(id)
    {
        Guard.AgainstEmpty(venueId, nameof(venueId));

        VenueId = venueId;

        Update(name, capacity);
    }

    public Guid VenueId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int Capacity { get; private set; }

    public void Update(
        string name,
        int capacity)
    {
        Name = Guard.RequiredText(
            name,
            nameof(Name),
            maxLength: 150);

        Capacity = Guard.PositiveNumber(
            capacity,
            nameof(Capacity));
    }
}