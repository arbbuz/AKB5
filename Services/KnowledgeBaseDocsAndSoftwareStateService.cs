using System.Globalization;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseDocumentLinkState
    {
        public string DocumentId { get; init; } = string.Empty;

        public KbDocumentKind Kind { get; init; }

        public string KindText { get; init; } = string.Empty;

        public string TitleText { get; init; } = string.Empty;

        public string PathText { get; init; } = string.Empty;

        public string UpdatedAtText { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseSoftwareRecordState
    {
        public string SoftwareId { get; init; } = string.Empty;

        public string TitleText { get; init; } = string.Empty;

        public string PathText { get; init; } = string.Empty;

        public string AddedAtText { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseDocsAndSoftwareState
    {
        public bool SupportsEditing { get; init; }

        public string SourceText { get; init; } = string.Empty;

        public string EmptyStateText { get; init; } = string.Empty;

        public int SchemeLinksCount { get; init; }

        public int ManualsAndInstructionsCount { get; init; }

        public int SoftwareRecordsCount { get; init; }

        public IReadOnlyList<KnowledgeBaseDocumentLinkState> SchemeLinkStates { get; init; } =
            Array.Empty<KnowledgeBaseDocumentLinkState>();

        public IReadOnlyList<KnowledgeBaseDocumentLinkState> ManualAndInstructionStates { get; init; } =
            Array.Empty<KnowledgeBaseDocumentLinkState>();

        public IReadOnlyList<KnowledgeBaseSoftwareRecordState> SoftwareRecordStates { get; init; } =
            Array.Empty<KnowledgeBaseSoftwareRecordState>();

        public bool HasEntries =>
            SchemeLinksCount > 0 ||
            ManualsAndInstructionsCount > 0 ||
            SoftwareRecordsCount > 0;
    }

    public class KnowledgeBaseDocsAndSoftwareStateService
    {
        public KnowledgeBaseDocsAndSoftwareState Build(
            KbNode? selectedNode,
            IReadOnlyList<KbDocumentLink>? documentLinks,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords,
            int visibleLevel = 0)
        {
            if (selectedNode == null || !SupportsRecords(selectedNode.NodeType, visibleLevel))
            {
                return new KnowledgeBaseDocsAndSoftwareState
                {
                    EmptyStateText = "Вкладка \"Документация и ПО\" недоступна для выбранного узла."
                };
            }

            string ownerNodeId = selectedNode.NodeId?.Trim() ?? string.Empty;
            var nodeDocumentLinks = GetOwnedDocumentLinks(ownerNodeId, documentLinks);
            var nodeSoftwareRecords = GetOwnedSoftwareRecords(ownerNodeId, softwareRecords);
            var schemeStates = BuildDocumentStates(
                nodeDocumentLinks.Where(static link => link.Kind == KbDocumentKind.SchemeLink));
            var instructionStates = BuildDocumentStates(
                nodeDocumentLinks.Where(link => link.Kind is KbDocumentKind.Manual or KbDocumentKind.Instruction));
            var softwareStates = BuildSoftwareStates(nodeSoftwareRecords);

            return new KnowledgeBaseDocsAndSoftwareState
            {
                SupportsEditing = true,
                SourceText = "Показаны ссылки на схемы, инструкции и папки с актуальными версиями ПО.",
                EmptyStateText = "Для этого узла пока нет ссылок на схемы, инструкции и ПО.",
                SchemeLinksCount = schemeStates.Count,
                ManualsAndInstructionsCount = instructionStates.Count,
                SoftwareRecordsCount = softwareStates.Count,
                SchemeLinkStates = schemeStates,
                ManualAndInstructionStates = instructionStates,
                SoftwareRecordStates = softwareStates
            };
        }

        public static bool SupportsRecords(KbNodeType nodeType, int visibleLevel = 0) =>
            KnowledgeBaseEngineeringNodeSupportService.SupportsEngineeringWorkspace(nodeType, visibleLevel);

        private static List<KbDocumentLink> GetOwnedDocumentLinks(
            string ownerNodeId,
            IReadOnlyList<KbDocumentLink>? documentLinks)
        {
            if (string.IsNullOrWhiteSpace(ownerNodeId) || documentLinks == null)
                return new List<KbDocumentLink>();

            return documentLinks
                .Where(link => string.Equals(link.OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
                .OrderBy(link => link.Title, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(link => link.Path, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(link => link.DocumentId, StringComparer.Ordinal)
                .ToList();
        }

        private static List<KbSoftwareRecord> GetOwnedSoftwareRecords(
            string ownerNodeId,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords)
        {
            if (string.IsNullOrWhiteSpace(ownerNodeId) || softwareRecords == null)
                return new List<KbSoftwareRecord>();

            return softwareRecords
                .Where(record => string.Equals(record.OwnerNodeId, ownerNodeId, StringComparison.Ordinal))
                .OrderBy(record => record.Title, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(record => record.Path, KnowledgeBaseNaturalStringComparer.Instance)
                .ThenBy(record => record.SoftwareId, StringComparer.Ordinal)
                .ToList();
        }

        private static List<KnowledgeBaseDocumentLinkState> BuildDocumentStates(IEnumerable<KbDocumentLink> links) =>
            links.Select(link => new KnowledgeBaseDocumentLinkState
            {
                DocumentId = link.DocumentId,
                Kind = link.Kind,
                KindText = GetDocumentKindText(link.Kind),
                TitleText = GetDisplayTitle(link.Title, link.Path),
                PathText = GetDisplayText(link.Path),
                UpdatedAtText = FormatDate(link.UpdatedAt)
            })
                .ToList();

        private static List<KnowledgeBaseSoftwareRecordState> BuildSoftwareStates(
            IEnumerable<KbSoftwareRecord> records) =>
            records.Select(record => new KnowledgeBaseSoftwareRecordState
            {
                SoftwareId = record.SoftwareId,
                TitleText = GetDisplayTitle(record.Title, record.Path),
                PathText = GetDisplayText(record.Path),
                AddedAtText = FormatDate(record.AddedAt)
            })
                .ToList();

        private static string GetDocumentKindText(KbDocumentKind kind) => kind switch
        {
            KbDocumentKind.SchemeLink => "Схема",
            KbDocumentKind.Manual => "Инструкция",
            KbDocumentKind.Instruction => "Инструкция",
            _ => "Документ"
        };

        private static string GetDisplayTitle(string? title, string? path)
        {
            string normalizedTitle = title?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedTitle))
                return normalizedTitle;

            string normalizedPath = path?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(normalizedPath)
                ? "(без названия)"
                : normalizedPath;
        }

        private static string GetDisplayText(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? "-"
                : value.Trim();

        private static string FormatDate(DateTime? value) =>
            value.HasValue
                ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : "-";
    }
}
