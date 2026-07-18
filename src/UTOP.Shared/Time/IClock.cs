namespace UTOP.Shared.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
