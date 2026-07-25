using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeatFlow.Domain.Entities;

namespace SeatFlow.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(
        EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .ValueGeneratedNever();

        builder.Property(user => user.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(user => user.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique();
    }
}

internal sealed class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(
        EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
            .ValueGeneratedNever();

        builder.Property(token => token.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(token => token.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(token => token.ExpiresAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(token => token.RevokedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.HasIndex(
            token => new
            {
                token.UserId,
                token.ExpiresAtUtc
            });

        builder.HasIndex(token => token.ReplacedByTokenId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ReservationUserConfiguration
    : IEntityTypeConfiguration<Reservation>
{
    public void Configure(
        EntityTypeBuilder<Reservation> builder)
    {
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(
                reservation => reservation.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}