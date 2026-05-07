namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseChangeLogEntry
    {
        public string ChangeId { get; init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public string ActionKind { get; init; } = string.Empty;

        public string Summary { get; init; } = string.Empty;

        public string Details { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseChangeLogAppendResult
    {
        public bool IsSuccess { get; init; }

        public bool IsSupported { get; init; } = true;

        public string? ErrorMessage { get; init; }
    }

    public sealed class KnowledgeBaseChangeLogListResult
    {
        public bool IsSuccess { get; init; }

        public bool IsSupported { get; init; } = true;

        public IReadOnlyList<KnowledgeBaseChangeLogEntry> Entries { get; init; } =
            Array.Empty<KnowledgeBaseChangeLogEntry>();

        public string? ErrorMessage { get; init; }
    }
}
