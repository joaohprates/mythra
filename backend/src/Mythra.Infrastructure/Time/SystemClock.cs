using Mythra.Application.Abstractions.Time;

namespace Mythra.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
