using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

internal sealed class InMemoryAppLogger : IAppLogger
{
    public List<InMemoryAppLogEntry> Entries { get; } = new();

    public void Log(
        string eventName,
        AppLogLevel level,
        string message,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        Entries.Add(new InMemoryAppLogEntry
        {
            EventName = eventName,
            Level = level,
            Message = message,
            Exception = exception,
            Properties = properties == null
                ? new Dictionary<string, object?>(StringComparer.Ordinal)
                : new Dictionary<string, object?>(properties, StringComparer.Ordinal)
        });
    }
}

internal sealed class InMemoryAppLogEntry
{
    public string EventName { get; init; } = string.Empty;

    public AppLogLevel Level { get; init; }

    public string Message { get; init; } = string.Empty;

    public Exception? Exception { get; init; }

    public IReadOnlyDictionary<string, object?> Properties { get; init; } =
        new Dictionary<string, object?>(StringComparer.Ordinal);
}
