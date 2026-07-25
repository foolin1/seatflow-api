using Microsoft.EntityFrameworkCore;
using SeatFlow.Domain.Entities;

using DomainEvent = SeatFlow.Domain.Entities.Event;

namespace SeatFlow.Infrastructure.Persistence;

public sealed class SeatFlowDbContext
    : DbContext
{
    public SeatFlowDbContext(
        DbContextOptions<SeatFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Venue> Venues => Set<Venue>();

    public DbSet<Hall> Halls => Set<Hall>();

    public DbSet<Seat> Seats => Set<Seat>();

    public DbSet<DomainEvent> Events => Set<DomainEvent>();

    public DbSet<EventSession> EventSessions =>
        Set<EventSession>();

    public DbSet<SessionSeat> SessionSeats =>
        Set<SessionSeat>();

    public DbSet<Reservation> Reservations =>
        Set<Reservation>();

    public DbSet<ReservationSeat> ReservationSeats =>
        Set<ReservationSeat>();

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SeatFlowDbContext).Assembly);

        SeatFlowSeedData.Apply(modelBuilder);
    }
}