using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTOP.Booking.Domain.Entities;
using UTOP.Booking.Domain.ValueObjects;
using UTOP.Shared.Domain.ValueObjects;
using BookingAggregate = UTOP.Booking.Domain.Aggregates.Booking;

namespace UTOP.Booking.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<BookingAggregate>
{
    public void Configure(EntityTypeBuilder<BookingAggregate> builder)
    {
        builder.ToTable("bookings", "utop_booking");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingId)
            .HasConversion(id => id.Value, v => new BookingId(v))
            .HasColumnName("booking_id")
            .HasMaxLength(20)
            .IsRequired();

        // FIX (see PENDING-LLD-CORRECTIONS.md): Mode/Status/Category were missing
        // HasMaxLength — inherited gap from the original LLD code, not something newly
        // introduced. Schema specifies VARCHAR(20)/VARCHAR(30)/VARCHAR(20) respectively.
        builder.Property(b => b.Mode)
            .HasConversion<string>()
            .HasColumnName("mode")
            .HasMaxLength(20);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(30);

        builder.Property(b => b.Category)
            .HasConversion<string>()
            .HasColumnName("category")
            .HasMaxLength(20);

        // Money uses OwnsOne — splits into two columns.
        // HasConversion with anonymous type does not work in EF Core.
        builder.OwnsOne(b => b.TotalPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("total_amount")
                .HasColumnType("NUMERIC(18,4)")
                .IsRequired();

            // FIX: Currency was missing HasMaxLength — schema specifies VARCHAR(10).
            money.Property(m => m.Currency)
                .HasConversion<string>()
                .HasColumnName("currency")
                .HasMaxLength(10)
                .IsRequired();
        });

        builder.Property(b => b.OperatorId).HasColumnName("operator_id").HasMaxLength(100);
        builder.Property(b => b.GroupId).HasColumnName("group_id").HasMaxLength(100);
        builder.Property(b => b.PilgrimageId).HasColumnName("pilgrimage_id").HasMaxLength(100);
        builder.Property(b => b.AmendmentVersion).HasColumnName("amendment_version");
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");

        // Optimistic concurrency (BK-CONC-001)
        builder.Property<int>("row_version")
            .HasColumnName("row_version")
            .IsConcurrencyToken();

        builder.HasOne(b => b.Itinerary)
            .WithOne()
            .HasForeignKey<Itinerary>("booking_id")
            .IsRequired();

        // FIX: schema explicitly says passengers.booking_id is NOT NULL with
        // ON DELETE CASCADE. Original config never called .IsRequired() here,
        // so EF made the FK nullable and dropped the cascade-delete behavior —
        // a real behavioral bug, not cosmetic (a Passenger can't exist without
        // its parent Booking per the aggregate boundary).
        builder.HasMany(b => b.PassengerList)
            .WithOne()
            .HasForeignKey("booking_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // FIX v2 (see PENDING-LLD-CORRECTIONS.md): the nested-OwnsOne approach below
        // (Origin/Destination as owned types within Route) does NOT work — EF's rule is
        // that navigations to owned types can never satisfy a constructor parameter, only
        // scalar/complex properties can. Since JourneyRoute's constructor needs Origin and
        // Destination bound, and those can only exist as navigations when nested this way,
        // materialization fails the same way the original Location bug did, one level up.
        // Fixed instead with a single JSON-converted column — sidesteps EF's owned-type
        // materialization machinery entirely, since System.Text.Json builds the object
        // graph itself and has no issue with nested records. Still full fidelity
        // (Code/Type/DisplayName for both Origin and Destination, plus RouteType) —
        // just one JSONB blob instead of seven flat columns. Queryable via Postgres's
        // JSONB path operators if another context ever needs to reach into it.
        builder.Property(b => b.Route)
            .HasConversion(
                route => JsonSerializer.Serialize(route, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<JourneyRoute>(json, (JsonSerializerOptions?)null)!)
            .HasColumnName("route")
            .HasColumnType("jsonb")
            .IsRequired();

        // FIX v2 (see PENDING-LLD-CORRECTIONS.md): OwnsOne requires the owned type to be
        // a class — PassengerCount is a readonly record struct (a value type), so that
        // constraint isn't satisfied and the compiler silently picked a different, wrong
        // overload instead (hence the confusing "Booking does not contain Property" error).
        // ComplexProperty is EF Core 8+'s correct API for struct-based value objects.
        builder.ComplexProperty(b => b.Passengers, passengers =>
        {
            passengers.Property(p => p.Adults).HasColumnName("adults").IsRequired();
            passengers.Property(p => p.Children).HasColumnName("children").IsRequired();
            passengers.Property(p => p.Infants).HasColumnName("infants").IsRequired();
        });

        builder.Ignore(b => b.DomainEvents);

        // FIX: none of these six indexes/constraint existed anywhere in the config —
        // a complete miss on the first pass, not just this migration. Matches LLD §9.2 exactly.
        builder.HasIndex(b => b.BookingId)
            .IsUnique()
            .HasDatabaseName("uq_bookings_booking_id");

        builder.HasIndex(b => b.OperatorId)
            .HasDatabaseName("ix_bookings_operator");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("ix_bookings_status");

        builder.HasIndex(b => b.GroupId)
            .HasFilter("group_id IS NOT NULL")
            .HasDatabaseName("ix_bookings_group_id");

        builder.HasIndex(b => b.PilgrimageId)
            .HasFilter("pilgrimage_id IS NOT NULL")
            .HasDatabaseName("ix_bookings_pilgrimage_id");
    }
}
