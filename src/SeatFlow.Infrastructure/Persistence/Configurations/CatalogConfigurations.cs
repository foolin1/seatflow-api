using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeatFlow.Domain.Entities;

using DomainEvent = SeatFlow.Domain.Entities.Event;

namespace SeatFlow.Infrastructure.Persistence.Configurations;

internal sealed class VenueConfiguration
    : IEntityTypeConfiguration<Venue>
{
    public void Configure(
        EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");

        builder.HasKey(venue => venue.Id);

        builder.Property(venue => venue.Id)
            .ValueGeneratedNever();

        builder.Property(venue => venue.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(venue => venue.Address)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(venue => venue.Description)
            .HasMaxLength(2000);

        builder.HasIndex(venue => venue.Name);
    }
}

internal sealed class HallConfiguration
    : IEntityTypeConfiguration<Hall>
{
    public void Configure(
        EntityTypeBuilder<Hall> builder)
    {
        builder.ToTable("halls");

        builder.HasKey(hall => hall.Id);

        builder.Property(hall => hall.Id)
            .ValueGeneratedNever();

        builder.Property(hall => hall.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(hall => hall.Capacity)
            .IsRequired();

        builder.HasIndex(
                hall => new
                {
                    hall.VenueId,
                    hall.Name
                })
            .IsUnique();

        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(hall => hall.VenueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SeatConfiguration
    : IEntityTypeConfiguration<Seat>
{
    public void Configure(
        EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("seats");

        builder.HasKey(seat => seat.Id);

        builder.Property(seat => seat.Id)
            .ValueGeneratedNever();

        builder.Property(seat => seat.RowLabel)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(seat => seat.Number)
            .IsRequired();

        builder.Property(seat => seat.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(
                seat => new
                {
                    seat.HallId,
                    seat.RowLabel,
                    seat.Number
                })
            .IsUnique();

        builder.HasOne<Hall>()
            .WithMany()
            .HasForeignKey(seat => seat.HallId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class EventConfiguration
    : IEntityTypeConfiguration<DomainEvent>
{
    public void Configure(
        EntityTypeBuilder<DomainEvent> builder)
    {
        builder.ToTable("events");

        builder.HasKey(eventEntity => eventEntity.Id);

        builder.Property(eventEntity => eventEntity.Id)
            .ValueGeneratedNever();

        builder.Property(eventEntity => eventEntity.Title)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(eventEntity => eventEntity.Description)
            .HasMaxLength(4000);

        builder.Property(eventEntity => eventEntity.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(eventEntity => eventEntity.AgeRestriction)
            .IsRequired();

        builder.HasIndex(eventEntity => eventEntity.Title);
    }
}

internal sealed class EventSessionConfiguration
    : IEntityTypeConfiguration<EventSession>
{
    public void Configure(
        EntityTypeBuilder<EventSession> builder)
    {
        builder.ToTable("event_sessions");

        builder.HasKey(session => session.Id);

        builder.Property(session => session.Id)
            .ValueGeneratedNever();

        builder.Property(session => session.StartsAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(session => session.BookingOpensAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(session => session.BookingClosesAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(session => session.IsCancelled)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(session => session.CancelledAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(
            session => new
            {
                session.EventId,
                session.StartsAtUtc
            });

        builder.HasIndex(
            session => new
            {
                session.HallId,
                session.StartsAtUtc
            });

        builder.HasOne<DomainEvent>()
            .WithMany()
            .HasForeignKey(session => session.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Hall>()
            .WithMany()
            .HasForeignKey(session => session.HallId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}