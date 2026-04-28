using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public class KnowledgeBaseSelectedNodeState
    {
        public bool HasSelection { get; init; }

        public string EmptyStateText { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string FullPath { get; init; } = string.Empty;

        public string ChildrenCountText { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string Location { get; init; } = string.Empty;

        public string PhotoPath { get; init; } = string.Empty;

        public string IpAddress { get; init; } = string.Empty;

        public string SchemaLink { get; init; } = string.Empty;

        public bool ShowTechnicalFields { get; init; }

        public KnowledgeBaseNodeWorkspaceState Workspace { get; init; } = new();

        public KnowledgeBaseCompositionState Composition { get; init; } = new();

        public KnowledgeBaseDocsAndSoftwareState DocsAndSoftware { get; init; } = new();
    }

    public class KnowledgeBaseFormState
    {
        public bool CanSave { get; init; }

        public string SaveToolTip { get; init; } = string.Empty;

        public string WindowTitle { get; init; } = string.Empty;

        public string SessionStatusText { get; init; } = string.Empty;

        public string SelectionStatusText { get; init; } = string.Empty;

        public string FileNameText { get; init; } = string.Empty;

        public string FilePathText { get; init; } = string.Empty;

        public string SaveStateText { get; init; } = string.Empty;

        public string WorkshopText { get; init; } = string.Empty;

        public KnowledgeBaseSelectedNodeState SelectedNode { get; init; } = new();
    }

    /// <summary>
    /// Содержит чистые правила вычисления состояния главной формы:
    /// доступность сохранения, заголовок окна и persistent session/selection контекст.
    /// </summary>
    public class KnowledgeBaseFormStateService
    {
        private readonly KnowledgeBaseNodePresentationService _nodePresentationService = new();
        private readonly KnowledgeBaseNodeWorkspaceResolverService _nodeWorkspaceResolverService = new();
        private readonly KnowledgeBaseCompositionStateService _compositionStateService = new();
        private readonly KnowledgeBaseDocsAndSoftwareStateService _docsAndSoftwareStateService = new();

        public KnowledgeBaseFormState Build(
            bool isDirty,
            bool requiresSave,
            string currentDataPath,
            string currentWorkshop,
            string lastSavedWorkshop,
            int totalNodes,
            IReadOnlyList<KbNode> currentRoots,
            KbNode? selectedNode,
            IReadOnlyList<KbCompositionEntry>? compositionEntries = null,
            IReadOnlyList<KbDocumentLink>? documentLinks = null,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords = null)
        {
            bool fileExists = File.Exists(currentDataPath);
            string currentDataFileName = Path.GetFileName(currentDataPath);
            if (string.IsNullOrWhiteSpace(currentDataFileName))
                currentDataFileName = "(без файла)";

            string saveStateText = BuildSaveStateText(isDirty, requiresSave, fileExists);
            var selectedNodeState = BuildSelectedNodeState(
                currentRoots,
                selectedNode,
                compositionEntries,
                documentLinks,
                softwareRecords);

            return new KnowledgeBaseFormState
            {
                CanSave = ShouldEnableSave(
                    isDirty,
                    requiresSave,
                    fileExists,
                    currentWorkshop,
                    lastSavedWorkshop),
                SaveToolTip = $"Сохранить базу данных ({currentDataPath})",
                WindowTitle = BuildWindowTitle(isDirty, currentDataFileName),
                SessionStatusText = BuildSessionStatusText(currentDataFileName, saveStateText, currentWorkshop, totalNodes),
                SelectionStatusText = BuildSelectionStatusText(selectedNodeState),
                FileNameText = currentDataFileName,
                FilePathText = currentDataPath,
                SaveStateText = saveStateText,
                WorkshopText = currentWorkshop,
                SelectedNode = selectedNodeState
            };
        }

        public bool ShouldEnableSave(
            bool isDirty,
            bool requiresSave,
            bool fileExists,
            string currentWorkshop,
            string lastSavedWorkshop) =>
            isDirty ||
            requiresSave ||
            !fileExists ||
            !string.Equals(currentWorkshop, lastSavedWorkshop, StringComparison.Ordinal);

        public string BuildWindowTitle(bool isDirty, string currentDataFileName) =>
            isDirty
                ? $"* База знаний АСУТП [{currentDataFileName}]"
                : $"База знаний АСУТП [{currentDataFileName}]";

        public string BuildSessionStatusText(
            string currentDataFileName,
            string saveStateText,
            string currentWorkshop,
            int totalNodes)
        {
            if (string.Equals(saveStateText, "Есть несохраненные изменения", StringComparison.Ordinal))
                return saveStateText;

            if (string.IsNullOrWhiteSpace(saveStateText))
                return string.Empty;

            return $"Файл: {currentDataFileName} | {saveStateText} | Цех: {currentWorkshop} | Узлов: {totalNodes}";
        }

        public string BuildSelectionStatusText(KnowledgeBaseSelectedNodeState selectedNodeState) => string.Empty;

        public bool RequiresSavePromptBeforeContinue(bool isDirty, bool requiresSave) =>
            isDirty || requiresSave;

        public bool RequiresSavePromptOnClose(bool isDirty, bool requiresSave) =>
            isDirty || requiresSave;

        public bool ShouldSaveSilentlyOnClose(string currentWorkshop, string lastSavedWorkshop) =>
            !string.Equals(currentWorkshop, lastSavedWorkshop, StringComparison.Ordinal);

        private static string BuildSaveStateText(bool isDirty, bool requiresSave, bool fileExists)
        {
            if (isDirty)
                return "Есть несохраненные изменения";

            if (requiresSave)
                return string.Empty;

            if (!fileExists)
                return "Файл отсутствует на диске";

            return "Сохранено";
        }

        private KnowledgeBaseSelectedNodeState BuildSelectedNodeState(
            IReadOnlyList<KbNode> currentRoots,
            KbNode? selectedNode,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            IReadOnlyList<KbDocumentLink>? documentLinks,
            IReadOnlyList<KbSoftwareRecord>? softwareRecords)
        {
            if (selectedNode == null)
            {
                return new KnowledgeBaseSelectedNodeState
                {
                    HasSelection = false,
                    EmptyStateText = "Ничего не выбрано. Выберите узел в дереве слева.",
                    Name = "—",
                    FullPath = "—",
                    ChildrenCountText = "—"
                };
            }

            bool supportsTechnicalFields = KnowledgeBaseNodeMetadataService.SupportsTechnicalFields(selectedNode.NodeType);

            return new KnowledgeBaseSelectedNodeState
            {
                HasSelection = true,
                Name = selectedNode.Name,
                FullPath = _nodePresentationService.BuildNodePath(currentRoots, selectedNode),
                ChildrenCountText = selectedNode.Children.Count.ToString(),
                Description = selectedNode.Details?.Description ?? string.Empty,
                Location = selectedNode.Details?.Location ?? string.Empty,
                PhotoPath = selectedNode.Details?.PhotoPath ?? string.Empty,
                IpAddress = supportsTechnicalFields ? selectedNode.Details?.IpAddress ?? string.Empty : string.Empty,
                SchemaLink = supportsTechnicalFields ? selectedNode.Details?.SchemaLink ?? string.Empty : string.Empty,
                ShowTechnicalFields = supportsTechnicalFields,
                Workspace = _nodeWorkspaceResolverService.Resolve(selectedNode.NodeType),
                Composition = _compositionStateService.Build(selectedNode, compositionEntries),
                DocsAndSoftware = _docsAndSoftwareStateService.Build(
                    selectedNode,
                    documentLinks,
                    softwareRecords)
            };
        }
    }
}
