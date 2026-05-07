using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseFullJsonExportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public byte[] JsonBytes { get; init; } = Array.Empty<byte>();
    }

    public sealed class KnowledgeBaseFullJsonImportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public SavedData? Data { get; init; }
    }

    public sealed class KnowledgeBaseFullJsonExchangeService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public KnowledgeBaseFullJsonExportResult ExportJson(SavedData data)
        {
            try
            {
                SavedData normalizedData = KnowledgeBaseDataService.NormalizeSavedData(data);
                string json = JsonSerializer.Serialize(normalizedData, SerializerOptions);
                return new KnowledgeBaseFullJsonExportResult
                {
                    IsSuccess = true,
                    JsonBytes = Encoding.UTF8.GetBytes(json)
                };
            }
            catch (Exception ex)
            {
                return ExportFailure($"Не удалось экспортировать базу в JSON: {ex.Message}");
            }
        }

        public KnowledgeBaseFullJsonImportResult ImportJson(byte[]? jsonBytes)
        {
            if (jsonBytes == null || jsonBytes.Length == 0)
                return ImportFailure("Файл JSON с базой не был передан.");

            try
            {
                string json = Encoding.UTF8.GetString(jsonBytes);
                SavedData? data = JsonSerializer.Deserialize<SavedData>(json, SerializerOptions);
                string? validationError = ValidateImportedData(data);
                if (validationError != null)
                    return ImportFailure(validationError);

                return new KnowledgeBaseFullJsonImportResult
                {
                    IsSuccess = true,
                    Data = KnowledgeBaseDataService.NormalizeSavedData(data)
                };
            }
            catch (Exception ex)
            {
                return ImportFailure($"Не удалось импортировать базу из JSON: {ex.Message}");
            }
        }

        private static string? ValidateImportedData(SavedData? data)
        {
            if (data == null)
                return "Файл не содержит корректную структуру базы.";

            string? schemaVersionError = KnowledgeBaseDataService.ValidateSupportedSchemaVersion(data.SchemaVersion);
            if (schemaVersionError != null)
                return schemaVersionError;

            if (data.Config == null)
                return "В JSON отсутствует раздел Config.";

            if (data.Workshops == null)
                return "В JSON отсутствует раздел Workshops.";

            string? workshopValidationError = KnowledgeBaseDataService.ValidateWorkshopNames(data.Workshops);
            if (workshopValidationError != null)
                return workshopValidationError;

            return null;
        }

        private static KnowledgeBaseFullJsonExportResult ExportFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseFullJsonImportResult ImportFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
