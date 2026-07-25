using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeatFlow.Domain.Entities;

namespace SeatFlow.Infrastructure.Persistence.Configurations;

internal sealed class SessionSeatConfiguration
    : IEntityTypeConfiguration<SessionSeat>
{
    public void Configure(
        EntityTypeBuilder<SessionSeat> builder)
    {
        builder.ToTable("session_seats");

        builder.HasKey(sessionSeat => sessionSeat.Id);

        builder.Property(sessionSeat => sessionSeat.Id)
            .ValueGeneratedNever();

        builder.Property(sessionSeat => sessionSeat.Price)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(sessionSeat => sessionSeat.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(sessionSeat => sessionSeat.ReservedUntilUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(sessionSeat => sessionSeat.Version)
            .IsRowVersion();

        builder.HasIndex(
                sessionSeat => new
                {
                    sessionSeat.EventSessionId,
                    sessionSeat.SeatId
                })
            .IsUnique();

        builder.HasIndex(
            sessionSeat => sessionSeat.Status);

        builder.HasIndex(
            sessionSeat => sessionSeat.ReservationId);

        builder.HasOne<EventSession>()
            .WithMany()
            .HasForeignKey(
                sessionSeat => sessionSeat.EventSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Seat>()
            .WithMany()
            .HasForeignKey(sessionSeat => sessionSeat.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Reservation>()
            .WithMany()
            .HasForeignKey(
                sessionSeat => sessionSeat.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ReservationConfiguration
    : IEntityTypeConfiguration<Reservation>
{
    public void Configure(
        EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");

        builder.HasKey(reservation => reservation.Id);

        builder.Property(reservation => reservation.Id)
            .ValueGeneratedNever();

        builder.Property(reservation => reservation.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(reservation => reservation.TotalAmount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(reservation => reservation.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(reservation => reservation.ExpiresAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(reservation => reservation.ConfirmedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(reservation => reservation.CancelledAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(reservation => reservation.ExpiredAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(
            reservation => new
            {
                reservation.UserId,
                reservation.CreatedAtUtc
            });

        builder.HasIndex(
            reservation => new
            {
                reservation.Status,
                reservation.ExpiresAtUtc
            });

        builder.HasOne<EventSession>()
            .WithMany()
            .HasForeignKey(
                reservation => reservation.EventSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(reservation => reservation.Seats)
            .WithOne()
            .HasForeignKey(
                reservationSeat =>
                    reservationSeat.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(reservation => reservation.Seats)
            .HasField("_seats")
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}

internal sealed class ReservationSeatConfiguration
    : IEntityTypeConfiguration<ReservationSeat>
{
    public void Configure(
        EntityTypeBuilder<ReservationSeat> builder)
    {
        builder.ToTable("reservation_seats");

        builder.HasKey(reservationSeat => reservationSeat.Id);

        builder.Property(reservationSeat => reservationSeat.Id)
            .ValueGeneratedNever();

        builder.Property(reservationSeat => reservationSeat.Price)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.HasIndex(
                reservationSeat => new
                {
                    reservationSeat.ReservationId,
                    reservationSeat.SessionSeatId
                })
            .IsUnique();

        builder.HasOne<SessionSeat>()
            .WithMany()
            .HasForeignKey(
                reservationSeat =>
                    reservationSeat.SessionSeatId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PaymentConfiguration
    : IEntityTypeConfiguration<Payment>
{
    public void Configure(
        EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Id)
            .ValueGeneratedNever();

        builder.Property(payment => payment.Amount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(payment => payment.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(payment => payment.ExternalReference)
            .HasMaxLength(200);

        builder.Property(payment => payment.FailureReason)
            .HasMaxLength(1000);

        builder.Property(payment => payment.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(payment => payment.CompletedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(payment => payment.ReservationId);

        builder.HasIndex(payment => payment.ExternalReference)
            .IsUnique();

        builder.HasOne<Reservation>()
            .WithMany()
            .HasForeignKey(payment => payment.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}