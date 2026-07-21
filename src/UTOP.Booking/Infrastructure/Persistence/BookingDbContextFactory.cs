using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UTOP.Booking.Infrastructure.Persistence;

/// <summary>
/// Enables `dotnet ef migrations add` to run against this project standalone,
/// before UTOP.API has full DI wiring for every context's DbContext.
/// Connection string here is design-time only — never used at runtime;
/// runtime configuration comes from UTOP.API's actual DI registration.
/// </summary>
public sealed class BookingDbContextFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BookingDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=utop_design_time;Username=postgres;Password=postgres");
        return new BookingDbContext(optionsBuilder.Options);
    }
}
