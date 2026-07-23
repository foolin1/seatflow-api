using SeatFlow.Domain.Common;
using SeatFlow.Domain.Enums;

namespace SeatFlow.Domain.Entities;

public sealed class Seat : Entity
{
    private Seat()
    {
    }

    public Seat(
        Guid id,
        Guid hallId,
        string rowLabel,
        int number,
        SeatCategory category)
        : base(id)
    {
        Guard.AgainstEmpty(hallId, nameof(hallId));

        HallId = hallId;

        RowLabel = Guard.RequiredText(
            rowLabel,
            nameof(RowLabel),
            maxLength: 20);

        Number = Guard.PositiveNumber(
            number,
            nameof(Number));

        Category = Guard.DefinedEnum(
            category,
            nameof(Category));
    }

    public Guid HallId { get; private set; }

    public string RowLabel { get; private set; } = string.Empty;

    public int Number { get; private set; }

    public SeatCategory Category { get; private set; }

    public void ChangeCategory(SeatCategory category)
    {
        Category = Guard.DefinedEnum(
            category,
            nameof(category));
    }
}