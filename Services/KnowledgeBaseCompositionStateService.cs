using System.Globalization;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseCompositionEntryState
    {
        public string EntryId { get; init; } = string.Empty;

        public bool IsSlotted { get; init; }

        public bool IsPlaceholder { get; init; }

        public int RackNumber { get; init; }

        public int? SlotNumberValue { get; init; }

        public int PositionOrder { get; init; }

        public string RackText { get; init; } = string.Empty;

        public string PositionText { get; init; } = string.Empty;

        public string SlotText { get; init; } = string.Empty;

        public string SlotRoleText { get; init; } = string.Empty;

        public string SlotAdvisoryText { get; init; } = string.Empty;

        public bool HasSlotWarning { get; init; }

        public bool HasSlotHint { get; init; }

        public string ComponentTypeText { get; init; } = string.Empty;

        public string ComponentText { get; init; } = string.Empty;

        public string OrderNumberText { get; init; } = string.Empty;

        public string FirmwareText { get; init; } = string.Empty;

        public string MpiDpPnAddressText { get; init; } = string.Empty;

        public string InputAddressText { get; init; } = string.Empty;

        public string OutputAddressText { get; init; } = string.Empty;

        public string CommentText { get; init; } = string.Empty;

        public string InterfaceRowsText { get; init; } = string.Empty;

        public string IpAddressText { get; init; } = string.Empty;

        public string LastCalibrationText { get; init; } = string.Empty;

        public string NextCalibrationText { get; init; } = string.Empty;

        public string NotesText { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseCompositionRackState
    {
        public string RackId { get; init; } = string.Empty;

        public int RackNumber { get; init; }

        public string Title { get; init; } = string.Empty;

        public string RackTypeText { get; init; } = "UR";

        public string LabelText { get; init; } = string.Empty;

        public string NetworkLinkText { get; init; } = string.Empty;

        public string NotesText { get; init; } = string.Empty;

        public bool IsExplicit { get; init; }

        public int FilledSlots { get; init; }

        public int TotalSlots { get; init; }

        public int WarningCount { get; init; }

        public int HintCount { get; init; }

        public bool CanDelete => IsExplicit && Entries.Count == 0;

        public IReadOnlyList<KnowledgeBaseCompositionEntryState> SlotRows { get; init; } =
            Array.Empty<KnowledgeBaseCompositionEntryState>();

        public IReadOnlyList<KnowledgeBaseCompositionEntryState> Entries { get; init; } =
            Array.Empty<KnowledgeBaseCompositionEntryState>();
    }

    public sealed class KnowledgeBaseCompositionState
    {
        public bool SupportsEditing { get; init; }

        public string SourceText { get; init; } = string.Empty;

        public string EmptyStateText { get; init; } = string.Empty;

        public int TotalEntries { get; init; }

        public int RackCount { get; init; }

        public int SlottedEntries { get; init; }

        public int AuxiliaryEntries { get; init; }

        public string ProfileText { get; init; } =
            KnowledgeBaseCompositionRackSlotRulesService.DefaultProfileDisplayName;

        public int WarningCount { get; init; }

        public int HintCount { get; init; }

        public IReadOnlyList<KnowledgeBaseCompositionRackState> RackStates { get; init; } =
            Array.Empty<KnowledgeBaseCompositionRackState>();

        public IReadOnlyList<KnowledgeBaseCompositionEntryState> SlottedEntryStates { get; init; } =
            Array.Empty<KnowledgeBaseCompositionEntryState>();

        public IReadOnlyList<KnowledgeBaseCompositionEntryState> AuxiliaryEntryStates { get; init; } =
            Array.Empty<KnowledgeBaseCompositionEntryState>();

        public IReadOnlyList<KnowledgeBaseCompositionEntryState> Entries { get; init; } =
            Array.Empty<KnowledgeBaseCompositionEntryState>();

        public bool HasRackModel { get; init; }

        public bool HasEntries => Entries.Count > 0;
    }

    public class KnowledgeBaseCompositionStateService
    {
        public KnowledgeBaseCompositionState Build(
            KbNode? selectedNode,
            IReadOnlyList<KbCompositionRack>? compositionRacks,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            int visibleLevel = 0)
        {
            if (selectedNode == null || !SupportsComposition(selectedNode.NodeType, visibleLevel))
            {
                return new KnowledgeBaseCompositionState
                {
                    EmptyStateText = "Вкладка \"Состав\" недоступна для выбранного узла."
                };
            }

            var typedRacks = GetOrderedTypedRacks(selectedNode.NodeId, compositionRacks);
            var typedEntries = GetOrderedTypedEntries(selectedNode.NodeId, compositionEntries);
            if (typedEntries.Count > 0)
                return BuildTypedState(typedRacks, typedEntries);

            if (typedRacks.Count > 0)
                return BuildTypedState(typedRacks, typedEntries);

            if (selectedNode.Children.Count > 0)
                return BuildLegacyFallbackState(selectedNode);

            return new KnowledgeBaseCompositionState
            {
                SupportsEditing = true,
                SourceText = "Записи состава еще не заполнены. Доступен Rack0 для первичной компоновки.",
                EmptyStateText = "Для этого узла еще нет записей состава.",
                RackCount = 1,
                RackStates = CreateRackStates(Array.Empty<KbCompositionRack>(), Array.Empty<KnowledgeBaseCompositionEntryState>())
            };
        }

        public KnowledgeBaseCompositionState Build(
            KbNode? selectedNode,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            int visibleLevel = 0) =>
            Build(selectedNode, compositionRacks: null, compositionEntries, visibleLevel);

        public static bool SupportsComposition(KbNodeType nodeType, int visibleLevel = 0) =>
            KnowledgeBaseEngineeringNodeSupportService.SupportsComposition(nodeType, visibleLevel);

        private static List<KbCompositionRack> GetOrderedTypedRacks(
            string parentNodeId,
            IReadOnlyList<KbCompositionRack>? compositionRacks)
        {
            if (string.IsNullOrWhiteSpace(parentNodeId) || compositionRacks == null)
                return new List<KbCompositionRack>();

            return KnowledgeBaseDataService.NormalizeCompositionRacks(compositionRacks)
                .Where(rack => string.Equals(rack.ParentNodeId, parentNodeId, StringComparison.Ordinal))
                .OrderBy(static rack => rack.SortOrder)
                .ThenBy(static rack => rack.RackNumber)
                .ThenBy(static rack => rack.RackId, StringComparer.Ordinal)
                .ToList();
        }

        private static List<KbCompositionEntry> GetOrderedTypedEntries(
            string parentNodeId,
            IReadOnlyList<KbCompositionEntry>? compositionEntries)
        {
            if (string.IsNullOrWhiteSpace(parentNodeId) || compositionEntries == null)
                return new List<KbCompositionEntry>();

            return compositionEntries
                .Where(entry => string.Equals(entry.ParentNodeId, parentNodeId, StringComparison.Ordinal))
                .OrderBy(static entry => entry.SlotNumber.HasValue ? 0 : 1)
                .ThenBy(static entry => entry.RackNumber)
                .ThenBy(static entry => entry.SlotNumber ?? int.MaxValue)
                .ThenBy(static entry => entry.PositionOrder)
                .ThenBy(static entry => entry.EntryId, StringComparer.Ordinal)
                .ToList();
        }

        private static KnowledgeBaseCompositionState BuildTypedState(
            IReadOnlyList<KbCompositionRack> typedRacks,
            IReadOnlyList<KbCompositionEntry> typedEntries)
        {
            bool hasExpansionRacks = typedEntries
                .Where(static entry => entry.SlotNumber.HasValue)
                .Select(static entry => KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(entry.RackNumber))
                .Concat(typedRacks.Select(static rack => rack.RackNumber))
                .Append(KnowledgeBaseCompositionRackSlotRulesService.DefaultRackNumber)
                .Any(static rackNumber => rackNumber > KnowledgeBaseCompositionRackSlotRulesService.DefaultRackNumber);
            var states = new List<KnowledgeBaseCompositionEntryState>(typedEntries.Count);
            var slottedStates = new List<KnowledgeBaseCompositionEntryState>();
            var auxiliaryStates = new List<KnowledgeBaseCompositionEntryState>();
            int auxiliaryIndex = 0;

            foreach (var entry in typedEntries)
            {
                if (!entry.SlotNumber.HasValue)
                    auxiliaryIndex++;

                var state = CreateEntryState(entry, auxiliaryIndex, hasExpansionRacks);

                states.Add(state);
                if (state.IsSlotted)
                    slottedStates.Add(state);
                else
                    auxiliaryStates.Add(state);
            }

            var rackStates = CreateRackStates(typedRacks, slottedStates);
            return new KnowledgeBaseCompositionState
            {
                SupportsEditing = true,
                SourceText = typedRacks.Count > 0
                    ? "Показана сохранённая модель Rack и записей состава. Проверка выполняется по профилю SIMATIC S7-300."
                    : "Показаны сохранённые записи состава, сгруппированные по Rack0+. Проверка выполняется по профилю SIMATIC S7-300.",
                TotalEntries = states.Count,
                RackCount = rackStates.Count,
                SlottedEntries = slottedStates.Count,
                AuxiliaryEntries = auxiliaryStates.Count,
                WarningCount = rackStates.Sum(static rack => rack.WarningCount),
                HintCount = rackStates.Sum(static rack => rack.HintCount),
                RackStates = rackStates,
                SlottedEntryStates = slottedStates,
                AuxiliaryEntryStates = auxiliaryStates,
                Entries = states,
                HasRackModel = typedRacks.Count > 0
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
                    SlotRoleText = "-",
                    ComponentTypeText = child.NodeType.ToString(),
                    ComponentText = GetDisplayText(child.Name),
                    OrderNumberText = "-",
                    FirmwareText = "-",
                    MpiDpPnAddressText = "-",
                    InputAddressText = "-",
                    OutputAddressText = "-",
                    CommentText = "-",
                    InterfaceRowsText = "-",
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
                SupportsEditing = true,
                SourceText = "Записи состава еще не заполнены. Пока показаны дочерние узлы дерева; Rack0 доступен для первичной компоновки.",
                TotalEntries = states.Count,
                RackCount = 1,
                AuxiliaryEntries = states.Count,
                RackStates = CreateRackStates(Array.Empty<KbCompositionRack>(), Array.Empty<KnowledgeBaseCompositionEntryState>()),
                AuxiliaryEntryStates = auxiliaryStates,
                Entries = states
            };
        }

        private static KnowledgeBaseCompositionEntryState CreateEntryState(
            KbCompositionEntry entry,
            int auxiliaryIndex,
            bool hasExpansionRacks = false)
        {
            bool isSlotted = entry.SlotNumber.HasValue;
            int rackNumber = KnowledgeBaseCompositionRackSlotRulesService.NormalizeRackNumber(entry.RackNumber);
            string rackText = isSlotted
                ? KnowledgeBaseCompositionRackSlotRulesService.FormatRackText(rackNumber)
                : string.Empty;
            string slotText = entry.SlotNumber?.ToString(CultureInfo.InvariantCulture) ?? "-";
            var slotAdvisory = isSlotted
                ? KnowledgeBaseCompositionRackSlotRulesService.GetSlotAdvisory(
                    rackNumber,
                    entry.SlotNumber!.Value,
                    entry.ComponentType,
                    entry.Model,
                    hasExpansionRacks: hasExpansionRacks)
                : KnowledgeBaseCompositionSlotAdvisory.None;

            return new KnowledgeBaseCompositionEntryState
            {
                EntryId = entry.EntryId,
                IsSlotted = isSlotted,
                RackNumber = rackNumber,
                SlotNumberValue = entry.SlotNumber,
                PositionOrder = entry.PositionOrder,
                RackText = rackText,
                PositionText = isSlotted
                    ? $"Слот {entry.SlotNumber!.Value}"
                    : $"Позиция {auxiliaryIndex}",
                SlotText = slotText,
                SlotRoleText = isSlotted
                    ? KnowledgeBaseCompositionRackSlotRulesService.GetSlotRoleText(rackNumber, entry.SlotNumber!.Value)
                    : "-",
                SlotAdvisoryText = GetDisplayText(slotAdvisory.Text),
                HasSlotWarning = slotAdvisory.HasWarning,
                HasSlotHint = slotAdvisory.HasHint,
                ComponentTypeText = GetDisplayText(entry.ComponentType),
                ComponentText = GetDisplayText(entry.Model),
                OrderNumberText = GetDisplayText(entry.OrderNumber),
                FirmwareText = GetDisplayText(entry.Firmware),
                MpiDpPnAddressText = GetDisplayText(entry.MpiDpPnAddress),
                InputAddressText = GetDisplayText(entry.InputAddress),
                OutputAddressText = GetDisplayText(entry.OutputAddress),
                CommentText = GetDisplayText(entry.Comment),
                InterfaceRowsText = GetDisplayText(entry.InterfaceRows),
                IpAddressText = GetDisplayText(entry.IpAddress),
                LastCalibrationText = FormatDate(entry.LastCalibrationAt),
                NextCalibrationText = FormatDate(entry.NextCalibrationAt),
                NotesText = GetDisplayText(entry.Notes)
            };
        }

        private static IReadOnlyList<KnowledgeBaseCompositionRackState> CreateRackStates(
            IReadOnlyList<KbCompositionRack> typedRacks,
            IReadOnlyList<KnowledgeBaseCompositionEntryState> slottedStates)
        {
            var explicitRacksByNumber = typedRacks
                .GroupBy(static rack => rack.RackNumber)
                .ToDictionary(static group => group.Key, static group => group.First());
            var rackNumbers = typedRacks
                .Select(static rack => rack.RackNumber)
                .Concat(slottedStates.Select(static state => state.RackNumber))
                .Append(KnowledgeBaseCompositionRackSlotRulesService.DefaultRackNumber)
                .Distinct()
                .OrderBy(rackNumber => explicitRacksByNumber.TryGetValue(rackNumber, out KbCompositionRack? rack)
                    ? rack.SortOrder
                    : rackNumber)
                .ThenBy(static rackNumber => rackNumber)
                .ToList();

            var rackStates = new List<KnowledgeBaseCompositionRackState>(rackNumbers.Count);
            foreach (int rackNumber in rackNumbers)
            {
                explicitRacksByNumber.TryGetValue(rackNumber, out KbCompositionRack? rackMetadata);
                var rackEntries = slottedStates
                    .Where(state => state.RackNumber == rackNumber)
                    .OrderBy(static state => state.SlotNumberValue ?? int.MaxValue)
                    .ThenBy(static state => state.PositionOrder)
                    .ThenBy(static state => state.EntryId, StringComparer.Ordinal)
                    .ToList();

                int maxSlot = Math.Max(
                    KnowledgeBaseCompositionRackSlotRulesService.DefaultSlotCount,
                    rackEntries.Select(static state => state.SlotNumberValue ?? 0).DefaultIfEmpty(0).Max());
                bool hasExpansionRacks = rackNumbers.Any(static number => number > KnowledgeBaseCompositionRackSlotRulesService.DefaultRackNumber);
                var slotRows = CreateSlotRows(rackNumber, maxSlot, rackEntries, hasExpansionRacks);

                rackStates.Add(new KnowledgeBaseCompositionRackState
                {
                    RackId = rackMetadata?.RackId ?? string.Empty,
                    RackNumber = rackNumber,
                    Title = rackMetadata == null
                        ? KnowledgeBaseCompositionRackSlotRulesService.FormatRackTitle(rackNumber)
                        : KnowledgeBaseCompositionRackSlotRulesService.FormatRackTitle(
                            rackNumber,
                            rackMetadata.RackType,
                            rackMetadata.Label),
                    RackTypeText = rackMetadata?.RackType ?? "UR",
                    LabelText = rackMetadata?.Label ?? string.Empty,
                    NetworkLinkText = rackMetadata?.NetworkLink ?? string.Empty,
                    NotesText = rackMetadata?.Notes ?? string.Empty,
                    IsExplicit = rackMetadata != null,
                    FilledSlots = rackEntries
                        .Where(static state => state.SlotNumberValue.HasValue)
                        .Select(static state => state.SlotNumberValue!.Value)
                        .Distinct()
                        .Count(),
                    TotalSlots = maxSlot,
                    WarningCount = slotRows.Count(static state => state.HasSlotWarning),
                    HintCount = slotRows.Count(static state => state.HasSlotHint),
                    SlotRows = slotRows,
                    Entries = rackEntries
                });
            }

            return rackStates;
        }

        private static IReadOnlyList<KnowledgeBaseCompositionEntryState> CreateSlotRows(
            int rackNumber,
            int maxSlot,
            IReadOnlyList<KnowledgeBaseCompositionEntryState> rackEntries,
            bool hasExpansionRacks)
        {
            var rows = new List<KnowledgeBaseCompositionEntryState>();
            for (int slotNumber = 1; slotNumber <= maxSlot; slotNumber++)
            {
                var slotEntries = rackEntries
                    .Where(state => state.SlotNumberValue == slotNumber)
                    .ToList();
                if (slotEntries.Count > 0)
                {
                    rows.AddRange(slotEntries);
                    continue;
                }

                var slotAdvisory = KnowledgeBaseCompositionRackSlotRulesService.GetSlotAdvisory(
                    rackNumber,
                    slotNumber,
                    componentType: string.Empty,
                    model: string.Empty,
                    isPlaceholder: true,
                    hasExpansionRacks);

                rows.Add(new KnowledgeBaseCompositionEntryState
                {
                    IsSlotted = true,
                    IsPlaceholder = true,
                    RackNumber = rackNumber,
                    SlotNumberValue = slotNumber,
                    RackText = KnowledgeBaseCompositionRackSlotRulesService.FormatRackText(rackNumber),
                    PositionText = $"Слот {slotNumber.ToString(CultureInfo.InvariantCulture)}",
                    SlotText = slotNumber.ToString(CultureInfo.InvariantCulture),
                    SlotRoleText = KnowledgeBaseCompositionRackSlotRulesService.GetSlotRoleText(rackNumber, slotNumber),
                    SlotAdvisoryText = GetDisplayText(slotAdvisory.Text),
                    HasSlotWarning = slotAdvisory.HasWarning,
                    HasSlotHint = slotAdvisory.HasHint,
                    ComponentTypeText = "-",
                    ComponentText = "-",
                    OrderNumberText = "-",
                    FirmwareText = "-",
                    MpiDpPnAddressText = "-",
                    InputAddressText = "-",
                    OutputAddressText = "-",
                    CommentText = "-",
                    InterfaceRowsText = "-",
                    IpAddressText = "-",
                    LastCalibrationText = "-",
                    NextCalibrationText = "-",
                    NotesText = "-"
                });
            }

            return rows;
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
