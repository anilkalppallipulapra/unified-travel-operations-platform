using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UTOP.Booking.Infrastructure.Persistence;

/// <summary>
/// Persistence-only model for the outbox pattern (ARCH-006). Not a domain concept —
/// lives here, not in Domain/Entities. Written by BookingRepository.SaveAsync alongside
/// the aggregate, in the same transaction. Publishing unpublished rows to RabbitMQ is
/// the deferred outbox processor's job (UTOP-LLD-BK-04) — out of scope for Booking itself.
/// </summary>
public sealed class OutboxEventEntity
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public string EventType { get; set; } = null!;   // routing key, e.g. "booking.created"
    public string Payload { get; set; } = null!;      // JSON-serialized integration event
    public Guid CorrelationId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }  // null = unpublished
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEventEntity>
{
    public void Configure(EntityTypeBuilder<OutboxEventEntity> builder)
    {
        builder.ToTable("outbox_events", "utop_booking");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.BookingId).HasColumnName("booking_id").IsRequired();
        builder.Property(o => o.EventType).HasColumnName("event_type").HasMaxLength(150).IsRequired();
        builder.Property(o => o.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(o => o.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(o => o.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(o => o.PublishedAt).HasColumnName("published_at");
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");

        // FIX (see PENDING-LLD-CORRECTIONS.md): existed in LLD §9.2 but was never
        // added — same class of miss as the bookings/itineraries indexes.
        builder.HasIndex(o => o.CreatedAt)
            .HasFilter("published_at IS NULL")
            .HasDatabaseName("ix_outbox_unpublished");
    }
}

/// <summary>
/// Persistence-only model for CreateBooking idempotency (ARCH-005 §1.4).
/// KeyHash is SHA-256 hex of (operatorId + mode + route.Origin.Code + route.Destination.Code + departureUtc ISO8601),
/// matching IBookingRepository.FindByIdempotencyKeyAsync's documented key derivation exactly.
/// </summary>
public sealed class IdempotencyKeyEntity
{
    public string KeyHash { get; set; } = null!;   // CHAR(64) hex
    public string BookingId { get; set; } = null!; // human-readable BookingId.Value
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKeyEntity>
{
    public void Configure(EntityTypeBuilder<IdempotencyKeyEntity> builder)
    {
        builder.ToTable("idempotency_keys", "utop_booking");
        builder.HasKey(k => k.KeyHash);

        builder.Property(k => k.KeyHash).HasColumnName("key_hash").HasColumnType("character(64)").IsRequired();
        builder.Property(k => k.BookingId).HasColumnName("booking_id").HasMaxLength(20).IsRequired();
        builder.Property(k => k.CreatedAt).HasColumnName("created_at");
    }
}
