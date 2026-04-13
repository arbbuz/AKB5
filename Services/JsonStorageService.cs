using System.Text.Json;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public class JsonStorageService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly IAppLogger _logger;

        public string SavePath { get; set; }

        public JsonStorageService(string savePath, IAppLogger? logger = null)
        {
            SavePath = savePath;
            _logger = logger ?? NullAppLogger.Instance;
        }

        public JsonLoadResult Load()
        {
            string backupPath = GetBackupPath(SavePath);
            _logger.Log(
                "JsonLoadStarted",
                AppLogLevel.Information,
                "Starting JSON load.",
                properties: CreateProperties(
                    ("path", SavePath),
                    ("backupPath", backupPath),
                    ("fileMissing", false),
                    ("usedBackup", false)));

            if (!File.Exists(SavePath))
            {
                _logger.Log(
                    "JsonLoadFailed",
                    AppLogLevel.Warning,
                    "JSON file was not found.",
                    properties: CreateProperties(
                        ("path", SavePath),
                        ("backupPath", backupPath),
                        ("fileMissing", true),
                        ("usedBackup", false)));

                return new JsonLoadResult
                {
                    FileMissing = true,
                    SourcePath = SavePath
                };
            }

            if (TryReadData(SavePath, out var data, out var errorMessage, out var readException))
            {
                _logger.Log(
                    "JsonLoadSucceeded",
                    AppLogLevel.Information,
                    "JSON file loaded successfully.",
                    properties: CreateProperties(
                        ("path", SavePath),
                        ("backupPath", backupPath),
                        ("fileMissing", false),
                        ("usedBackup", false),
                        ("schemaVersion", data?.SchemaVersion)));

                return new JsonLoadResult
                {
                    Data = data,
                    SourcePath = SavePath
                };
            }

            SavedData? backupData = null;
            string? backupErrorMessage = null;
            Exception? backupException = null;

            if (File.Exists(backupPath) &&
                TryReadData(backupPath, out backupData, out backupErrorMessage, out backupException))
            {
                _logger.Log(
                    "JsonLoadFallbackToBackup",
                    AppLogLevel.Warning,
                    "Primary JSON file failed to load. Falling back to backup.",
                    readException,
                    CreateProperties(
                        ("path", SavePath),
                        ("backupPath", backupPath),
                        ("fileMissing", false),
                        ("usedBackup", true),
                        ("primaryErrorMessage", errorMessage),
                        ("primarySchemaVersion", data?.SchemaVersion),
                        ("schemaVersion", backupData?.SchemaVersion)));

                _logger.Log(
                    "JsonLoadSucceeded",
                    AppLogLevel.Information,
                    "Backup JSON file loaded successfully.",
                    properties: CreateProperties(
                        ("path", backupPath),
                        ("backupPath", backupPath),
                        ("fileMissing", false),
                        ("usedBackup", true),
                        ("schemaVersion", backupData?.SchemaVersion)));

                return new JsonLoadResult
                {
                    Data = backupData,
                    SourcePath = backupPath,
                    BackupPath = backupPath,
                    LoadedFromBackup = true,
                    PrimaryErrorMessage = errorMessage
                };
            }

            _logger.Log(
                "JsonLoadFailed",
                readException == null && backupException == null
                    ? AppLogLevel.Warning
                    : AppLogLevel.Error,
                "JSON file failed to load.",
                readException ?? backupException,
                CreateProperties(
                    ("path", SavePath),
                    ("backupPath", File.Exists(backupPath) ? backupPath : null),
                    ("fileMissing", false),
                    ("usedBackup", false),
                    ("primaryErrorMessage", errorMessage),
                    ("backupErrorMessage", backupErrorMessage),
                    ("schemaVersion", data?.SchemaVersion)));

            return new JsonLoadResult
            {
                SourcePath = SavePath,
                BackupPath = File.Exists(backupPath) ? backupPath : null,
                ErrorMessage = errorMessage,
                PrimaryErrorMessage = errorMessage
            };
        }

        public bool Save(SavedData data, out string? errorMessage)
        {
            errorMessage = null;
            string tempPath = $"{SavePath}.tmp";
            string backupPath = GetBackupPath(SavePath);

            _logger.Log(
                "JsonSaveStarted",
                AppLogLevel.Information,
                "Starting JSON save.",
                properties: CreateProperties(
                    ("path", SavePath),
                    ("backupPath", backupPath),
                    ("tempPath", tempPath),
                    ("schemaVersion", data.SchemaVersion)));

            try
            {
                string? directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(data, SerializerOptions);
                File.WriteAllText(tempPath, json);

                if (File.Exists(SavePath))
                    File.Copy(SavePath, backupPath, true);

                File.Move(tempPath, SavePath, true);

                _logger.Log(
                    "JsonSaveSucceeded",
                    AppLogLevel.Information,
                    "JSON file saved successfully.",
                    properties: CreateProperties(
                        ("path", SavePath),
                        ("backupPath", backupPath),
                        ("tempPath", tempPath),
                        ("schemaVersion", data.SchemaVersion)));

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                }

                _logger.Log(
                    "JsonSaveFailed",
                    AppLogLevel.Error,
                    "JSON file save failed.",
                    ex,
                    CreateProperties(
                        ("path", SavePath),
                        ("backupPath", backupPath),
                        ("tempPath", tempPath),
                        ("schemaVersion", data.SchemaVersion)));

                return false;
            }
        }

        private static string GetBackupPath(string savePath) => $"{savePath}.bak";

        private static bool TryReadData(
            string path,
            out SavedData? data,
            out string? errorMessage,
            out Exception? exception)
        {
            data = null;
            errorMessage = null;
            exception = null;

            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    errorMessage = "Файл пустой.";
                    return false;
                }

                data = JsonSerializer.Deserialize<SavedData>(json, SerializerOptions);
                errorMessage = ValidateData(data);
                return errorMessage == null;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                exception = ex;
                return false;
            }
        }

        private static Dictionary<string, object?> CreateProperties(params (string Key, object? Value)[] values)
        {
            var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                if (string.IsNullOrWhiteSpace(key) || value == null)
                    continue;

                properties[key] = value;
            }

            return properties;
        }

        private static string? ValidateData(SavedData? data)
        {
            if (data == null)
                return "Файл не содержит корректную структуру базы.";

            string? schemaVersionError = KnowledgeBaseDataService.ValidateSupportedSchemaVersion(data.SchemaVersion);
            if (schemaVersionError != null)
                return schemaVersionError;

            if (data.Config == null)
                return "Отсутствует раздел Config.";

            if (data.Config.LevelNames == null)
                return "В Config отсутствует список LevelNames.";

            if (data.Workshops == null)
                return "Отсутствует раздел Workshops.";

            foreach (var pair in data.Workshops)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    return "Обнаружен цех с пустым именем.";

                if (pair.Value == null)
                    return $"У цеха '{pair.Key}' отсутствует список корневых узлов.";
            }

            string? workshopValidationError = KnowledgeBaseDataService.ValidateWorkshopNames(data.Workshops);
            if (workshopValidationError != null)
                return workshopValidationError;

            foreach (var pair in data.Workshops)
            {
                var path = new List<string> { pair.Key };
                foreach (var node in pair.Value)
                {
                    var nodeError = ValidateNode(node, path);
                    if (nodeError != null)
                        return nodeError;
                }
            }

            return null;
        }

        private static string? ValidateNode(KbNode? node, List<string> path)
        {
            if (node == null)
                return $"Обнаружен пустой узел по пути '{string.Join(" -> ", path)}'.";

            node.Details ??= new KbNodeDetails();

            if (string.IsNullOrWhiteSpace(node.Name))
                return $"Обнаружен узел с пустым именем по пути '{string.Join(" -> ", path)}'.";

            if (node.LevelIndex < 0)
                return $"Узел '{node.Name}' имеет отрицательный LevelIndex.";

            if (node.Children == null)
                return $"Узел '{node.Name}' не содержит списка Children.";

            path.Add(node.Name);
            foreach (var child in node.Children)
            {
                var childError = ValidateNode(child, path);
                if (childError != null)
                {
                    path.RemoveAt(path.Count - 1);
                    return childError;
                }
            }

            path.RemoveAt(path.Count - 1);
            return null;
        }
    }
}
