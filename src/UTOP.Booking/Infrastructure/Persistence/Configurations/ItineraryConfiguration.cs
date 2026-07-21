using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTOP.Booking.Domain.Entities;
using UTOP.Shared.Domain.ValueObjects;

namespace UTOP.Booking.Infrastructure.Persistence.Configurations;

public sealed class ItineraryConfiguration : IEntityTypeConfiguration<Itinerary>
{
    public void Configure(EntityTypeBuilder<Itinerary> builder)
    {
        builder.ToTable("itineraries", "utop_booking");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.DepartureUtc).HasColumnName("departure_utc").IsRequired();
        builder.Property(i => i.ArrivalUtc).HasColumnName("arrival_utc").IsRequired();
        builder.Property(i => i.DepartureCity).HasColumnName("departure_city").HasMaxLength(100).IsRequired();
        builder.Property(i => i.DepartureCountry).HasColumnName("departure_country").HasMaxLength(3).IsRequired();
        builder.Property(i => i.ArrivalCity).HasColumnName("arrival_city").HasMaxLength(100).IsRequired();
        builder.Property(i => i.ArrivalCountry).HasColumnName("arrival_country").HasMaxLength(3).IsRequired();
        builder.Property(i => i.CarrierReference).HasColumnName("carrier_reference").HasMaxLength(20);
        builder.Property(i => i.ServiceClass).HasColumnName("service_class").HasMaxLength(20);

        // FIX (see PENDING-LLD-CORRECTIONS.md): Location's original LLD config tried to
        // OwnsOne() it while ignoring Type/DisplayName — but Location has no parameterless
        // constructor, so EF cannot materialize it with 2 of 3 constructor params unmapped.
        // Converting to a plain string column instead (just Code, matching the schema's
        // single VARCHAR(10) column). Type/DisplayName are NOT round-tripped through the
        // database — reconstructed as a fixed placeholder on load. Confirmed nothing
        // downstream reads .Type/.DisplayName off these two properties, only .Code.
        builder.Property(i => i.DeparturePoint)
            .HasConversion(
                loc => loc.Code,
                code => new Location(code, LocationType.Airport, null))
            .HasColumnName("departure_airport")
            .HasMaxLength(10);

        builder.Property(i => i.ArrivalPoint)
            .HasConversion(
                loc => loc.Code,
                code => new Location(code, LocationType.Airport, null))
            .HasColumnName("arrival_airport")
            .HasMaxLength(10);
        // FIX (see PENDING-LLD-CORRECTIONS.md): this index existed in LLD §9.2 but was
        // never added to the config — same class of miss as the bookings-table indexes.
        builder.HasIndex(i => i.DepartureUtc)
            .HasDatabaseName("ix_itineraries_departure");
    }
}
