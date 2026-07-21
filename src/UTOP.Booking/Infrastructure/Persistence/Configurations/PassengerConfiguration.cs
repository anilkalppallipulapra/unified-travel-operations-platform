using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTOP.Booking.Domain.Entities;

namespace UTOP.Booking.Infrastructure.Persistence.Configurations;

public sealed class PassengerConfiguration : IEntityTypeConfiguration<Passenger>
{
    public void Configure(EntityTypeBuilder<Passenger> builder)
    {
        builder.ToTable("passengers", "utop_booking");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName)
            .HasColumnName("first_name")
            .IsRequired();

        builder.Property(p => p.LastName)
            .HasColumnName("last_name")
            .IsRequired();

        // FIX (see PENDING-LLD-CORRECTIONS.md): was missing HasMaxLength — schema
        // specifies VARCHAR(10). Inherited gap from the original LLD code.
        builder.Property(p => p.Type)
            .HasConversion<string>()
            .HasColumnName("passenger_type")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.DateOfBirth)
            .HasColumnName("date_of_birth")
            .IsRequired();

        builder.Property(p => p.DocumentNumber)
            .HasColumnName("document_number");

        builder.Property(p => p.Nationality)
            .HasColumnName("nationality")
            .HasMaxLength(3);
    }
}
