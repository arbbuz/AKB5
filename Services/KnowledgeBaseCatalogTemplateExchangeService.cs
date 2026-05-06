using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseCatalogTemplateExportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public byte[] JsonBytes { get; init; } = Array.Empty<byte>();

        public int ExportedCatalogItemCount { get; init; }

        public int ExportedTemplateCount { get; init; }
    }

    public sealed class KnowledgeBaseCatalogTemplateImportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public List<KbEquipmentCatalogItem> EquipmentCatalogItems { get; init; } = new();

        public List<KbObjectTemplate> ObjectTemplates { get; init; } = new();

        public int ImportedCatalogItemCount { get; init; }

        public int ImportedTemplateCount { get; init; }

        public int AddedCatalogItemCount { get; init; }

        public int AddedTemplateCount { get; init; }

        public int SkippedCatalogItemCount { get; init; }

        public int SkippedTemplateCount { get; init; }
    }

    public sealed class KnowledgeBaseCatalogTemplateExchangeService
    {
        private const int CurrentExchangeSchemaVersion = 1;
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public KnowledgeBaseCatalogTemplateExportResult ExportJson(
            IEnumerable<KbEquipmentCatalogItem>? equipmentCatalogItems,
            IEnumerable<KbObjectTemplate>? objectTemplates)
        {
            try
            {
                List<KbEquipmentCatalogItem> normalizedCatalogItems =
                    KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(equipmentCatalogItems);
                List<KbObjectTemplate> normalizedTemplates =
                    KnowledgeBaseDataService.NormalizeObjectTemplates(objectTemplates);

                var document = new CatalogTemplateExchangeDocument
                {
                    ExchangeSchemaVersion = CurrentExchangeSchemaVersion,
                    EquipmentCatalogItems = normalizedCatalogItems,
                    ObjectTemplates = normalizedTemplates
                };

                string json = JsonSerializer.Serialize(document, SerializerOptions);
                return new KnowledgeBaseCatalogTemplateExportResult
                {
                    IsSuccess = true,
                    JsonBytes = Encoding.UTF8.GetBytes(json),
                    ExportedCatalogItemCount = normalizedCatalogItems.Count,
                    ExportedTemplateCount = normalizedTemplates.Count
                };
            }
            catch (Exception ex)
            {
                return ExportFailure($"Не удалось экспортировать каталог и шаблоны в JSON: {ex.Message}");
            }
        }

        public KnowledgeBaseCatalogTemplateImportResult ImportJson(
            byte[]? jsonBytes,
            IEnumerable<KbEquipmentCatalogItem>? currentCatalogItems,
            IEnumerable<KbObjectTemplate>? currentObjectTemplates)
        {
            if (jsonBytes == null || jsonBytes.Length == 0)
                return ImportFailure("Файл JSON с каталогом и шаблонами не был передан.");

            try
            {
                string json = Encoding.UTF8.GetString(jsonBytes);
                CatalogTemplateExchangeDocument document = ReadExchangeDocument(json);

                List<KbEquipmentCatalogItem> normalizedCurrentCatalog =
                    KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(currentCatalogItems);
                List<KbObjectTemplate> normalizedCurrentTemplates =
                    KnowledgeBaseDataService.NormalizeObjectTemplates(currentObjectTemplates);
                List<KbEquipmentCatalogItem> normalizedImportedCatalog =
                    KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(document.EquipmentCatalogItems);
                List<KbObjectTemplate> normalizedImportedTemplates =
                    KnowledgeBaseDataService.NormalizeObjectTemplates(document.ObjectTemplates);

                List<KbEquipmentCatalogItem> mergedCatalog =
                    KnowledgeBaseDataService.NormalizeEquipmentCatalogItems(
                        normalizedCurrentCatalog.Concat(normalizedImportedCatalog));
                List<KbObjectTemplate> mergedTemplates =
                    KnowledgeBaseDataService.NormalizeObjectTemplates(
                        normalizedCurrentTemplates.Concat(normalizedImportedTemplates));

                var currentCatalogIds = normalizedCurrentCatalog
                    .Select(static item => item.CatalogItemId)
                    .ToHashSet(StringComparer.Ordinal);
                var currentTemplateIds = normalizedCurrentTemplates
                    .Select(static template => template.TemplateId)
                    .ToHashSet(StringComparer.Ordinal);

                int addedCatalogItems = mergedCatalog.Count(item => !currentCatalogIds.Contains(item.CatalogItemId));
                int addedTemplates = mergedTemplates.Count(template => !currentTemplateIds.Contains(template.TemplateId));

                return new KnowledgeBaseCatalogTemplateImportResult
                {
                    IsSuccess = true,
                    EquipmentCatalogItems = mergedCatalog,
                    ObjectTemplates = mergedTemplates,
                    ImportedCatalogItemCount = normalizedImportedCatalog.Count,
                    ImportedTemplateCount = normalizedImportedTemplates.Count,
                    AddedCatalogItemCount = addedCatalogItems,
                    AddedTemplateCount = addedTemplates,
                    SkippedCatalogItemCount = Math.Max(0, normalizedImportedCatalog.Count - addedCatalogItems),
                    SkippedTemplateCount = Math.Max(0, normalizedImportedTemplates.Count - addedTemplates)
                };
            }
            catch (Exception ex)
            {
                return ImportFailure($"Не удалось импортировать каталог и шаблоны из JSON: {ex.Message}");
            }
        }

        private static CatalogTemplateExchangeDocument ReadExchangeDocument(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    "Ожидается JSON-объект с полями EquipmentCatalogItems и/или ObjectTemplates.");
            }

            bool hasCatalogSection = HasProperty(document.RootElement, nameof(CatalogTemplateExchangeDocument.EquipmentCatalogItems));
            bool hasTemplateSection = HasProperty(document.RootElement, nameof(CatalogTemplateExchangeDocument.ObjectTemplates));
            if (!hasCatalogSection && !hasTemplateSection)
            {
                throw new InvalidOperationException(
                    "В JSON не найдены разделы EquipmentCatalogItems или ObjectTemplates.");
            }

            CatalogTemplateExchangeDocument exchangeDocument =
                JsonSerializer.Deserialize<CatalogTemplateExchangeDocument>(json, SerializerOptions) ??
                new CatalogTemplateExchangeDocument();

            if (exchangeDocument.ExchangeSchemaVersion > CurrentExchangeSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Версия файла обмена {exchangeDocument.ExchangeSchemaVersion} не поддерживается.");
            }

            return exchangeDocument;
        }

        private static bool HasProperty(JsonElement root, string propertyName) =>
            root.EnumerateObject().Any(property =>
                string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));

        private static KnowledgeBaseCatalogTemplateExportResult ExportFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseCatalogTemplateImportResult ImportFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private sealed class CatalogTemplateExchangeDocument
        {
            public int ExchangeSchemaVersion { get; set; } = CurrentExchangeSchemaVersion;

            public List<KbEquipmentCatalogItem> EquipmentCatalogItems { get; set; } = new();

            public List<KbObjectTemplate> ObjectTemplates { get; set; } = new();
        }
    }
}
