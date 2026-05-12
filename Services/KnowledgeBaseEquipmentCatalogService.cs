using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseEquipmentCatalogMutationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbEquipmentCatalogItem> EquipmentCatalogItems { get; init; } = new();
    }

    public sealed class KnowledgeBaseEquipmentCatalogService
    {
        public List<KbEquipmentCatalogItem> Search(
            IEnumerable<KbEquipmentCatalogItem>? items,
            string? searchText)
        {
            List<KbEquipmentCatalogItem> normalizedItems =
                KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(items)
                    .Select(CloneCatalogItem)
                    .ToList();
            string normalizedSearchText = NormalizeSearchText(searchText);
            if (string.IsNullOrWhiteSpace(normalizedSearchText))
                return normalizedItems;

            return normalizedItems
                .Where(item => BuildSearchText(item).Contains(normalizedSearchText, StringComparison.Ordinal))
                .ToList();
        }

        public KnowledgeBaseEquipmentCatalogMutationResult UpsertItem(
            IEnumerable<KbEquipmentCatalogItem>? currentItems,
            KbEquipmentCatalogItem? draftItem)
        {
            if (!TryNormalizeDraft(draftItem, out KbEquipmentCatalogItem normalizedDraft, out string errorMessage))
                return Failure(errorMessage);

            List<KbEquipmentCatalogItem> normalizedItems =
                KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(currentItems)
                    .Select(CloneCatalogItem)
                    .ToList();

            string draftId = normalizedDraft.CatalogItemId.Trim();
            bool hasExistingItem = normalizedItems.Any(item =>
                string.Equals(item.CatalogItemId, draftId, StringComparison.Ordinal));
            string draftSemanticKey = BuildSemanticKey(normalizedDraft);

            bool hasDuplicateSemanticKey = normalizedItems.Any(item =>
                !string.Equals(item.CatalogItemId, draftId, StringComparison.Ordinal) &&
                string.Equals(BuildSemanticKey(item), draftSemanticKey, StringComparison.OrdinalIgnoreCase));
            if (hasDuplicateSemanticKey)
            {
                return Failure(
                    "В каталоге уже есть запись с таким видом оборудования, производителем, серией и моделью.");
            }

            var nextItems = normalizedItems
                .Where(item => !string.Equals(item.CatalogItemId, draftId, StringComparison.Ordinal))
                .ToList();
            nextItems.Add(normalizedDraft);

            List<KbEquipmentCatalogItem> normalizedResult =
                KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(nextItems);
            bool itemPersisted = normalizedResult.Any(item =>
                string.Equals(item.CatalogItemId, draftId, StringComparison.Ordinal));
            if (!itemPersisted)
            {
                return Failure(hasExistingItem
                    ? "Запись каталога не удалось обновить после нормализации."
                    : "Запись каталога не удалось добавить после нормализации.");
            }

            return Success(normalizedResult);
        }

        public KnowledgeBaseEquipmentCatalogMutationResult DeleteItem(
            IEnumerable<KbEquipmentCatalogItem>? currentItems,
            string? catalogItemId)
        {
            string normalizedId = catalogItemId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedId))
                return Failure("Выберите запись каталога для удаления.");

            List<KbEquipmentCatalogItem> normalizedItems =
                KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(currentItems)
                    .Select(CloneCatalogItem)
                    .ToList();

            if (!normalizedItems.Any(item => string.Equals(item.CatalogItemId, normalizedId, StringComparison.Ordinal)))
                return Failure("Выбранная запись каталога не найдена.");

            return Success(
                normalizedItems
                    .Where(item => !string.Equals(item.CatalogItemId, normalizedId, StringComparison.Ordinal))
                    .ToList());
        }

        public static KbEquipmentCatalogItem CloneCatalogItem(KbEquipmentCatalogItem item) =>
            new()
            {
                CatalogItemId = item.CatalogItemId?.Trim() ?? string.Empty,
                EquipmentKind = item.EquipmentKind?.Trim() ?? string.Empty,
                Manufacturer = item.Manufacturer?.Trim() ?? string.Empty,
                Series = item.Series?.Trim() ?? string.Empty,
                Model = item.Model?.Trim() ?? string.Empty,
                DefaultNodeType = Enum.IsDefined(typeof(KbNodeType), item.DefaultNodeType)
                    ? item.DefaultNodeType
                    : KbNodeType.Device,
                Description = item.Description?.Trim() ?? string.Empty,
                Properties = (item.Properties ?? new List<KbEquipmentCatalogProperty>())
                    .Select(static property => new KbEquipmentCatalogProperty
                    {
                        Name = property.Name?.Trim() ?? string.Empty,
                        Value = property.Value?.Trim() ?? string.Empty
                    })
                    .ToList()
            };

        private static bool TryNormalizeDraft(
            KbEquipmentCatalogItem? draftItem,
            out KbEquipmentCatalogItem normalizedDraft,
            out string errorMessage)
        {
            normalizedDraft = new KbEquipmentCatalogItem();
            errorMessage = string.Empty;
            if (draftItem == null)
            {
                errorMessage = "Запись каталога не была передана.";
                return false;
            }

            KbEquipmentCatalogItem draft = CloneCatalogItem(draftItem);
            if (string.IsNullOrWhiteSpace(draft.CatalogItemId))
                draft.CatalogItemId = $"catalog-{Guid.NewGuid():N}";

            List<KbEquipmentCatalogItem> normalizedItems =
                KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(new[] { draft });
            if (normalizedItems.Count == 0)
            {
                errorMessage = "Укажите вид оборудования, производителя, серию или модель.";
                return false;
            }

            normalizedDraft = normalizedItems[0];
            return true;
        }

        private static string BuildSearchText(KbEquipmentCatalogItem item)
        {
            var parts = new List<string>
            {
                item.EquipmentKind,
                item.Manufacturer,
                item.Model,
                item.Description
            };

            return NormalizeSearchText(string.Join(" ", parts));
        }

        private static string BuildSemanticKey(KbEquipmentCatalogItem item) =>
            string.Join(
                "|",
                NormalizeSearchText(item.EquipmentKind),
                NormalizeSearchText(item.Manufacturer),
                NormalizeSearchText(item.Series),
                NormalizeSearchText(item.Model));

        private static string NormalizeSearchText(string? value) =>
            string.Join(
                    " ",
                    (value ?? string.Empty)
                        .Trim()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToUpperInvariant();

        private static KnowledgeBaseEquipmentCatalogMutationResult Success(
            List<KbEquipmentCatalogItem> equipmentCatalogItems) =>
            new()
            {
                IsSuccess = true,
                EquipmentCatalogItems = KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(equipmentCatalogItems)
            };

        private static KnowledgeBaseEquipmentCatalogMutationResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
