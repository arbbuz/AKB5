using System.Globalization;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseCompositionEntryState
    {
        public string EntryId { get; init; } = string.Empty;

        public bool IsSlotted { get; init; }

        public string PositionText { get; init; } = string.Empty;

        public string SlotText { get; init; } = string.Empty;

        public string ComponentTypeText { get; init; } = string.Empty;

        public string ComponentText { get; init; } = string.Empty;

        public string IpAddressText { get; init; } = string.Empty;

        public string LastCalibrationText { get; init; } = string.Empty;

        public string NextCalibrationText { get; init; } = string.Empty;

        public string NotesText { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseCompositionState
    {
        public bool SupportsEditing { get; init; }

        public bool CanApplyTemplates { get; init; }

        public string SourceText { get; init; } = string.Empty;

        public string EmptyStateText { get; init; } = string.Empty;

        public int TotalEntries { get; init; }

        public int SlottedEntries { get; init; }

        public int AuxiliaryEntries { get; init; }

        public IReadOnlyList<KnowledgeBaseCompositionEntryState> SlottedEntryStates { get; init; } =
            Array.Empty<KnowledgeBaseCompositionEntryState>();

        public IReadOnlyList<KnowledgeBaseCompositionEntryState> AuxiliaryEntryStates { get; init; } =
            Array.Empty<KnowledgeBaseCompositionEntryState>();

        public IReadOnlyList<KnowledgeBaseCompositionEntryState> Entries { get; init; } =
            Array.Empty<KnowledgeBaseCompositionEntryState>();

        public bool HasEntries => Entries.Count > 0;
    }

    public class KnowledgeBaseCompositionStateService
    {
        public KnowledgeBaseCompositionState Build(
            KbNode? selectedNode,
            IReadOnlyList<KbCompositionEntry>? compositionEntries)
        {
            if (selectedNode == null || !SupportsComposition(selectedNode.NodeType))
            {
                return new KnowledgeBaseCompositionState
                {
                    EmptyStateText = "Вкладка \"Состав\" недоступна для выбранного узла."
                };
            }

            var typedEntries = GetOrderedTypedEntries(selectedNode.NodeId, compositionEntries);
            if (typedEntries.Count > 0)
                return BuildTypedState(selectedNode.NodeType, typedEntries);

            if (selectedNode.Children.Count > 0)
                return BuildLegacyFallbackState(selectedNode);

            return new KnowledgeBaseCompositionState
            {
                CanApplyTemplates = SupportsTemplates(selectedNode.NodeType),
                SupportsEditing = true,
                SourceText = "Записи состава еще не заполнены.",
                EmptyStateText = "Для этого узла еще нет записей состава."
            };
        }

        public static bool SupportsComposition(KbNodeType nodeType) => nodeType switch
        {
            KbNodeType.Cabinet => true,
            KbNodeType.Device => true,
            KbNodeType.Controller => true,
            KbNodeType.Module => true,
            _ => false
        };

        public static bool SupportsTemplates(KbNodeType nodeType) => nodeType switch
        {
            KbNodeType.Cabinet => true,
            KbNodeType.Controller => true,
            _ => false
        };

        private static List<KbCompositionEntry> GetOrderedTypedEntries(
            string parentNodeId,
            IReadOnlyList<KbCompositionEntry>? compositionEntries)
        {
            if (string.IsNullOrWhiteSpace(parentNodeId) || compositionEntries == null)
                return new List<KbCompositionEntry>();

            return compositionEntries
                .Where(entry => string.Equals(entry.ParentNodeId, parentNodeId, StringComparison.Ordinal))
                .OrderBy(static entry => entry.SlotNumber.HasValue ? 0 : 1)
                .ThenBy(static entry => entry.SlotNumber ?? int.MaxValue)
                .ThenBy(static entry => entry.PositionOrder)
                .ThenBy(static entry => entry.EntryId, StringComparer.Ordinal)
                .ToList();
        }

        private static KnowledgeBaseCompositionState BuildTypedState(
            KbNodeType nodeType,
            IReadOnlyList<KbCompositionEntry> typedEntries)
        {
            var states = new List<KnowledgeBaseCompositionEntryState>(typedEntries.Count);
            var slottedStates = new List<KnowledgeBaseCompositionEntryState>();
            var auxiliaryStates = new List<KnowledgeBaseCompositionEntryState>();
            int auxiliaryIndex = 0;

            foreach (var entry in typedEntries)
            {
                if (!entry.SlotNumber.HasValue)
                    auxiliaryIndex++;

                var state = new KnowledgeBaseCompositionEntryState
                {
                    EntryId = entry.EntryId,
                    IsSlotted = entry.SlotNumber.HasValue,
                    PositionText = entry.SlotNumber.HasValue
                        ? $"Слот {entry.SlotNumber.Value}"
                        : $"Позиция {auxiliaryIndex}",
                    SlotText = entry.SlotNumber?.ToString(CultureInfo.InvariantCulture) ?? "-",
                    ComponentTypeText = GetDisplayText(entry.ComponentType),
                    ComponentText = GetDisplayText(entry.Model),
                    IpAddressText = GetDisplayText(entry.IpAddress),
                    LastCalibrationText = FormatDate(entry.LastCalibrationAt),
                    NextCalibrationText = FormatDate(entry.NextCalibrationAt),
                    NotesText = GetDisplayText(entry.Notes)
                };

                states.Add(state);
                if (state.IsSlotted)
                    slottedStates.Add(state);
                else
                    auxiliaryStates.Add(state);
            }

            return new KnowledgeBaseCompositionState
            {
                CanApplyTemplates = SupportsTemplates(nodeType),
                SupportsEditing = true,
                SourceText = "Показаны записи состава из JSON.",
                TotalEntries = states.Count,
                SlottedEntries = typedEntries.Count(static entry => entry.SlotNumber.HasValue),
                AuxiliaryEntries = typedEntries.Count(static entry => !entry.SlotNumber.HasValue),
                SlottedEntryStates = slottedStates,
                AuxiliaryEntryStates = auxiliaryStates,
                Entries = states
            };
        }

        private static KnowledgeBaseCompositionState BuildLegacyFallbackState(KbNode selectedNode)
        {
            var states = new List<KnowledgeBaseCompositionEntryState>(selectedNode.Children.Count);
            var auxiliaryStates = new List<KnowledgeBaseCompositionEntryState>(selectedNode.Children.Count);
            int auxiliaryIndex = 0;

            foreach (var child in selectedNode.Children)
            {
                auxiliaryIndex++;
                var state = new KnowledgeBaseCompositionEntryState
                {
                    EntryId = string.Empty,
                    IsSlotted = false,
                    PositionText = $"Позиция {auxiliaryIndex}",
                    SlotText = "-",
                    ComponentTypeText = child.NodeType.ToString(),
                    ComponentText = GetDisplayText(child.Name),
                    IpAddressText = GetDisplayText(child.Details?.IpAddress),
                    LastCalibrationText = "-",
                    NextCalibrationText = "-",
                    NotesText = GetDisplayText(child.Details?.Description)
                };

                states.Add(state);
                auxiliaryStates.Add(state);
            }

            return new KnowledgeBaseCompositionState
            {
                CanApplyTemplates = SupportsTemplates(selectedNode.NodeType),
                SupportsEditing = true,
                SourceText = "Записи состава еще не заполнены. Пока показаны дочерние узлы дерева.",
                TotalEntries = states.Count,
                SlottedEntries = 0,
                AuxiliaryEntries = states.Count,
                AuxiliaryEntryStates = auxiliaryStates,
                Entries = states
            };
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
