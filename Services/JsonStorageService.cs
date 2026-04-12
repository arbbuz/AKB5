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

        public string SavePath { get; set; }

        public JsonStorageService(string savePath) => SavePath = savePath;

        public JsonLoadResult Load()
        {
            if (!File.Exists(SavePath))
            {
                return new JsonLoadResult
                {
                    FileMissing = true,
                    SourcePath = SavePath
                };
            }

            if (TryReadData(SavePath, out var data, out var errorMessage))
            {
                return new JsonLoadResult
                {
                    Data = data,
                    SourcePath = SavePath
                };
            }

            string backupPath = GetBackupPath(SavePath);
            if (File.Exists(backupPath) &&
                TryReadData(backupPath, out var backupData, out var backupErrorMessage))
            {
                return new JsonLoadResult
                {
                    Data = backupData,
                    SourcePath = backupPath,
                    BackupPath = backupPath,
                    LoadedFromBackup = true,
                    PrimaryErrorMessage = errorMessage
                };
            }

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

                return false;
            }
        }

        private static string GetBackupPath(string savePath) => $"{savePath}.bak";

        private static bool TryReadData(string path, out SavedData? data, out string? errorMessage)
        {
            data = null;
            errorMessage = null;

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
                return false;
            }
        }

        private static string? ValidateData(SavedData? data)
        {
            if (data == null)
                return "Файл не содержит корректную структуру базы.";

            if (data.SchemaVersion < 1)
                return $"Неподдерживаемая версия схемы: {data.SchemaVersion}.";

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
