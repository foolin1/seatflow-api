using SeatFlow.Domain.Common;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.Domain.Entities;

public sealed class Payment : Entity
{
    private Payment()
    {
    }

    public Payment(
        Guid id,
        Guid reservationId,
        decimal amount,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Guard.AgainstEmpty(
            reservationId,
            nameof(reservationId));

        ReservationId = reservationId;

        Amount = Guard.PositiveMoney(
            amount,
            nameof(Amount));

        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        Status = PaymentStatus.Pending;
    }

    public Guid ReservationId { get; private set; }

    public decimal Amount { get; private set; }

    public PaymentStatus Status { get; private set; }

    public string? ExternalReference { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public void Succeed(
        string externalReference,
        DateTimeOffset completedAtUtc)
    {
        EnsurePending();

        var normalizedCompletionTime =
            ValidateCompletionTime(completedAtUtc);

        ExternalReference = Guard.RequiredText(
            externalReference,
            nameof(externalReference),
            maxLength: 200);

        Status = PaymentStatus.Succeeded;
        CompletedAtUtc = normalizedCompletionTime;
    }

    public void Fail(
        string failureReason,
        DateTimeOffset completedAtUtc)
    {
        EnsurePending();

        var normalizedCompletionTime =
            ValidateCompletionTime(completedAtUtc);

        FailureReason = Guard.RequiredText(
            failureReason,
            nameof(failureReason),
            maxLength: 1000);

        Status = PaymentStatus.Failed;
        CompletedAtUtc = normalizedCompletionTime;
    }

    private DateTimeOffset ValidateCompletionTime(
        DateTimeOffset completedAtUtc)
    {
        var normalizedCompletionTime =
            completedAtUtc.ToUniversalTime();

        if (normalizedCompletionTime < CreatedAtUtc)
        {
            throw new DomainValidationException(
                "Payment completion time cannot be before creation time.");
        }

        return normalizedCompletionTime;
    }

    private void EnsurePending()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainConflictException(
                $"Payment '{Id}' has already been completed " +
                $"with status {Status}.");
        }
    }
}