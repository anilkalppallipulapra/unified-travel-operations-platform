namespace UTOP.Shared.Time;

/// <summary>
/// Deterministic clock for unit and integration tests (ARCH-009 §3.4).
/// Starts at a fixed point; can be advanced explicitly by tests.
/// Never use SystemClock in tests — time-dependent tests are not repeatable.
/// Thread-safety: NOT thread-safe. Use one FakeClock per test scope.
///   Do not share a FakeClock instance across parallel test threads.
/// </summary>
public sealed class FakeClock : IClock
{
    private DateTimeOffset _current;

    public FakeClock(DateTimeOffset startTime)
    {
        _current = startTime;
    }

    /// <summary>Convenience constructor — defaults to 2024-01-01T00:00:00Z.</summary>
    public FakeClock() : this(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)) { }

    public DateTimeOffset UtcNow => _current;

    public void AdvanceBy(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        _current += duration;
    }

    public void SetTo(DateTimeOffset instant)
    {
        _current = instant;
    }
}
