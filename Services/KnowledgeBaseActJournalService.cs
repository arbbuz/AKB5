using System.Globalization;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseActJournalRow
    {
        public string ActId { get; init; } = string.Empty;

        public DateTime? ActDate { get; init; }

        public string ActDateText { get; init; } = string.Empty;

        public string ActNumberText { get; init; } = string.Empty;

        public string StatusText { get; init; } = string.Empty;

        public string ActTypeText { get; init; } = string.Empty;

        public string WorkshopName { get; init; } = string.Empty;

        public string ObjectName { get; init; } = string.Empty;

        public string EquipmentName { get; init; } = string.Empty;

        public string OrderNumber { get; init; } = string.Empty;

        public string DocumentPath { get; init; } = string.Empty;

        public string AbsoluteDocumentPath { get; init; } = string.Empty;

        public bool CanDeletePhysically { get; init; }

        public bool CanChangeStatus { get; init; }

        public bool CanGenerateDocument { get; init; }

        public bool CanOpenDocument { get; init; }
    }

    public sealed class KnowledgeBaseActJournalService
    {
        public IReadOnlyList<KnowledgeBaseActJournalRow> BuildRows(
            IEnumerable<KbAct>? acts,
            IEnumerable<KbActDocument>? documents,
            string documentBaseDirectory = "",
            Func<string, bool>? fileExists = null)
        {
            fileExists ??= File.Exists;
            List<KbAct> normalizedActs = KnowledgeBaseDataService.NormalizeActs(acts);
            HashSet<string> knownActIds = normalizedActs
                .Select(static act => act.ActId)
                .ToHashSet(StringComparer.Ordinal);
            Dictionary<string, KbActDocument> latestDocumentsByActId = KnowledgeBaseDataService
                .NormalizeActDocuments(documents, knownActIds)
                .GroupBy(static document => document.ActId, StringComparer.Ordinal)
                .ToDictionary(
                    static group => group.Key,
                    static group => group
                        .OrderByDescending(static document => document.IsLatest)
                        .ThenByDescending(static document => document.VersionNumber)
                        .ThenBy(static document => document.DocumentId, StringComparer.Ordinal)
                        .First(),
                    StringComparer.Ordinal);

            return normalizedActs
                .Select(act =>
                {
                    latestDocumentsByActId.TryGetValue(act.ActId, out KbActDocument? document);
                    string documentPath = document?.Path?.Trim() ?? string.Empty;
                    string absoluteDocumentPath = ResolveDocumentAbsolutePath(documentPath, documentBaseDirectory);
                    return new KnowledgeBaseActJournalRow
                    {
                        ActId = act.ActId,
                        ActDate = act.ActDate,
                        ActDateText = act.ActDate?.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture) ?? string.Empty,
                        ActNumberText = string.IsNullOrWhiteSpace(act.ActNumber)
                            ? "без номера"
                            : act.ActNumber,
                        StatusText = FormatStatus(act.Status),
                        ActTypeText = FormatActType(act.ActType),
                        WorkshopName = act.WorkshopName,
                        ObjectName = act.ObjectNameSnapshot,
                        EquipmentName = act.EquipmentName,
                        OrderNumber = act.EquipmentSnapshot?.OrderNumber ?? string.Empty,
                        DocumentPath = documentPath,
                        AbsoluteDocumentPath = absoluteDocumentPath,
                        CanDeletePhysically = CanDeletePhysically(act, documentPath),
                        CanChangeStatus = CanChangeStatus(act.Status),
                        CanGenerateDocument = CanGenerateDocument(act.Status),
                        CanOpenDocument = CanOpenDocument(absoluteDocumentPath, fileExists)
                    };
                })
                .OrderByDescending(static row => row.ActDate ?? DateTime.MinValue)
                .ThenByDescending(static row => row.ActNumberText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static row => row.WorkshopName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static row => row.ObjectName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static row => row.EquipmentName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static row => row.ActId, StringComparer.Ordinal)
                .ToList();
        }

        public bool CanDeletePhysically(KbAct? act, IEnumerable<KbActDocument>? documents)
        {
            if (act == null)
                return false;

            string actId = act.ActId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(actId))
                return false;

            bool hasDocumentPath = KnowledgeBaseDataService
                .NormalizeActDocuments(documents, new[] { actId })
                .Any(static document => !string.IsNullOrWhiteSpace(document.Path));
            return CanDeletePhysically(act, hasDocumentPath ? "docx" : string.Empty);
        }

        public static string FormatStatus(KbActStatus status) =>
            status switch
            {
                KbActStatus.Draft => "Черновик",
                KbActStatus.Generated => "Сформирован",
                KbActStatus.Signed => "Подписан",
                KbActStatus.Cancelled => "Отменен",
                KbActStatus.Archived => "Архив",
                KbActStatus.Annulled => "Аннулирован",
                _ => "Черновик"
            };

        public static string FormatActType(KbActType actType) =>
            actType switch
            {
                KbActType.EquipmentFailure => "Отказ оборудования",
                KbActType.InspectionWork => "Осмотр / работы",
                _ => "Отказ оборудования"
            };

        public static bool CanChangeStatus(KbActStatus status) =>
            status != KbActStatus.Cancelled &&
            status != KbActStatus.Annulled &&
            status != KbActStatus.Archived;

        public static bool CanGenerateDocument(KbActStatus status) =>
            status != KbActStatus.Cancelled &&
            status != KbActStatus.Annulled &&
            status != KbActStatus.Archived;

        private static bool CanOpenDocument(string absoluteDocumentPath, Func<string, bool> fileExists)
        {
            if (string.IsNullOrWhiteSpace(absoluteDocumentPath))
                return false;

            try
            {
                return fileExists(absoluteDocumentPath);
            }
            catch
            {
                return false;
            }
        }

        private static bool CanDeletePhysically(KbAct act, string documentPath) =>
            act.Status == KbActStatus.Draft &&
            string.IsNullOrWhiteSpace(act.ActNumber) &&
            string.IsNullOrWhiteSpace(documentPath);

        private static string ResolveDocumentAbsolutePath(string documentPath, string documentBaseDirectory)
        {
            if (string.IsNullOrWhiteSpace(documentPath))
                return string.Empty;

            try
            {
                return Path.GetFullPath(Path.IsPathRooted(documentPath)
                    ? documentPath
                    : Path.Combine(
                        string.IsNullOrWhiteSpace(documentBaseDirectory)
                            ? AppContext.BaseDirectory
                            : documentBaseDirectory,
                        documentPath));
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
