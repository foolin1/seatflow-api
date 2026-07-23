using SeatFlow.Domain.Common;

namespace SeatFlow.Domain.Entities;

public sealed class Venue : Entity
{
    private Venue()
    {
    }

    public Venue(
        Guid id,
        string name,
        string address,
        string? description = null)
        : base(id)
    {
        Update(name, address, description);
    }

    public string Name { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public void Update(
        string name,
        string address,
        string? description)
    {
        Name = Guard.RequiredText(
            name,
            nameof(Name),
            maxLength: 200);

        Address = Guard.RequiredText(
            address,
            nameof(Address),
            maxLength: 300);

        Description = Guard.OptionalText(
            description,
            nameof(Description),
            maxLength: 2000);
    }
}