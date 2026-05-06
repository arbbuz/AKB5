using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public enum KnowledgeBaseObjectTemplateApplicationAction
    {
        Added,
        Skipped,
        Unchanged
    }

    public sealed class KnowledgeBaseObjectTemplateApplicationPreviewItem
    {
        public KnowledgeBaseObjectTemplateApplicationAction Action { get; init; }

        public string Area { get; init; } = string.Empty;

        public string Target { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseObjectTemplateDetailUpdate
    {
        public KbNode TargetNode { get; init; } = null!;

        public string FieldKey { get; init; } = string.Empty;

        public string FieldDisplayName { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseObjectTemplateNodeAddition
    {
        public KbNode ParentNode { get; init; } = null!;

        public KbNode Node { get; init; } = null!;
    }

    public sealed class KnowledgeBaseObjectTemplateApplicationPlan
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public string TemplateDisplayName { get; init; } = string.Empty;

        public string TargetName { get; init; } = string.Empty;

        public List<KnowledgeBaseObjectTemplateApplicationPreviewItem> PreviewItems { get; init; } = new();

        public List<KnowledgeBaseObjectTemplateDetailUpdate> DetailUpdates { get; init; } = new();

        public List<KnowledgeBaseObjectTemplateNodeAddition> NodeAdditions { get; init; } = new();

        public List<KbCompositionEntry> CompositionEntries { get; init; } = new();

        public List<KbDocumentLink> DocumentLinks { get; init; } = new();

        public List<KbSoftwareRecord> SoftwareRecords { get; init; } = new();

        public List<KbNetworkFileReference> NetworkFileReferences { get; init; } = new();

        public List<KbMaintenanceScheduleProfile> MaintenanceScheduleProfiles { get; init; } = new();

        public int AddedCount => PreviewItems.Count(static item =>
            item.Action == KnowledgeBaseObjectTemplateApplicationAction.Added);

        public int SkippedCount => PreviewItems.Count(static item =>
            item.Action == KnowledgeBaseObjectTemplateApplicationAction.Skipped);

        public int UnchangedCount => PreviewItems.Count(static item =>
            item.Action == KnowledgeBaseObjectTemplateApplicationAction.Unchanged);

        public bool HasChanges =>
            DetailUpdates.Count > 0 ||
            NodeAdditions.Count > 0 ||
            CompositionEntries.Count > 0 ||
            DocumentLinks.Count > 0 ||
            SoftwareRecords.Count > 0 ||
            NetworkFileReferences.Count > 0 ||
            MaintenanceScheduleProfiles.Count > 0;
    }
}
