using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;

namespace SeatFlow.UnitTests;

public sealed class PaymentTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(
            2026,
            8,
            1,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void Succeed_WhenPaymentIsPending_MarksPaymentAsSucceeded()
    {
        var payment = CreatePayment();
        var completionTime = CreatedAt.AddSeconds(5);

        payment.Succeed(
            "test-payment-001",
            completionTime);

        Assert.Equal(
            PaymentStatus.Succeeded,
            payment.Status);

        Assert.Equal(
            "test-payment-001",
            payment.ExternalReference);

        Assert.Equal(
            completionTime,
            payment.CompletedAtUtc);
    }

    [Fact]
    public void Fail_WhenPaymentIsPending_MarksPaymentAsFailed()
    {
        var payment = CreatePayment();

        payment.Fail(
            "Test payment was declined.",
            CreatedAt.AddSeconds(5));

        Assert.Equal(
            PaymentStatus.Failed,
            payment.Status);

        Assert.Equal(
            "Test payment was declined.",
            payment.FailureReason);
    }

    [Fact]
    public void Succeed_WhenPaymentWasAlreadyCompleted_ThrowsConflict()
    {
        var payment = CreatePayment();

        payment.Succeed(
            "test-payment-001",
            CreatedAt.AddSeconds(5));

        Assert.Throws<DomainConflictException>(
            () => payment.Succeed(
                "test-payment-002",
                CreatedAt.AddSeconds(10)));
    }

    private static Payment CreatePayment()
    {
        return new Payment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            125.50m,
            CreatedAt);
    }
}