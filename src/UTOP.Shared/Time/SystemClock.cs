namespace UTOP.Shared.Time;

/// <summary>
/// Production clock. Delegates to the system wall clock (ARCH-009 §3.3).
/// Registered as a singleton in the DI container: services.AddSingleton&lt;IClock&gt;(SystemClock.Instance);
/// Thread-safe: DateTimeOffset.UtcNow returns an immutable value.
/// </summary>
public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();

    private SystemClock() { }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
