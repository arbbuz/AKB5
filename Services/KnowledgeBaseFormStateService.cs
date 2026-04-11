using System;
using System.IO;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public class KnowledgeBaseFormState
    {
        public bool CanSave { get; init; }

        public string SaveToolTip { get; init; } = string.Empty;

        public string WindowTitle { get; init; } = string.Empty;

        public string StatusText { get; init; } = string.Empty;
    }

    /// <summary>
    /// Содержит чистые правила вычисления состояния главной формы:
    /// доступность сохранения, заголовок окна, статусную строку и close/save-решения.
    /// </summary>
    public class KnowledgeBaseFormStateService
    {
        public KnowledgeBaseFormState Build(
            bool isDirty,
            bool requiresSave,
            string currentDataPath,
            string currentWorkshop,
            string lastSavedWorkshop,
            int totalNodes,
            KbConfig config,
            KbNode? selectedNode)
        {
            string currentDataFileName = Path.GetFileName(currentDataPath);

            return new KnowledgeBaseFormState
            {
                CanSave = ShouldEnableSave(
                    isDirty,
                    requiresSave,
                    File.Exists(currentDataPath),
                    currentWorkshop,
                    lastSavedWorkshop),
                SaveToolTip = $"Сохранить базу данных ({currentDataPath})",
                WindowTitle = BuildWindowTitle(isDirty, currentDataFileName),
                StatusText = BuildStatusText(currentWorkshop, totalNodes, config, selectedNode)
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

        public string BuildStatusText(
            string currentWorkshop,
            int totalNodes,
            KbConfig config,
            KbNode? selectedNode)
        {
            if (selectedNode != null)
            {
                string levelName = config.LevelNames.Count > selectedNode.LevelIndex
                    ? config.LevelNames[selectedNode.LevelIndex]
                    : $"Ур. {selectedNode.LevelIndex + 1}";

                return $"Цех: {currentWorkshop} | Всего: {totalNodes} | Выбрано: {selectedNode.Name} ({levelName})";
            }

            return $"Цех: {currentWorkshop} | Всего узлов: {totalNodes} | Уровней: {config.MaxLevels}";
        }

        public bool RequiresSavePromptBeforeContinue(bool isDirty) => isDirty;

        public bool RequiresSavePromptOnClose(bool isDirty) => isDirty;

        public bool ShouldSaveSilentlyOnClose(string currentWorkshop, string lastSavedWorkshop) =>
            !string.Equals(currentWorkshop, lastSavedWorkshop, StringComparison.Ordinal);
    }
}
