namespace SeatFlow.Domain.Common;

public abstract class Entity
{
    protected Entity(Guid id)
    {
        Guard.AgainstEmpty(id, nameof(id));
        Id = id;
    }

    protected Entity()
    {
    }

    public Guid Id { get; protected set; }
}