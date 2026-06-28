using System.Text;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseActDocumentPathRequest
    {
        public KbAct? Act { get; init; }

        public KbConfig? Config { get; init; }

        public IEnumerable<KbActDocument>? ExistingDocuments { get; init; }

        public string SelectedPath { get; init; } = string.Empty;

        public string DatabasePath { get; init; } = string.Empty;

        public string ApplicationBasePath { get; init; } = string.Empty;

        public bool AllowExistingFile { get; init; }
    }

    public sealed class KnowledgeBaseActDocumentPathResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public string FileName { get; init; } = string.Empty;

        public string AbsolutePath { get; init; } = string.Empty;

        public string StoredPath { get; init; } = string.Empty;

        public string StoredDirectoryPath { get; init; } = string.Empty;

        public bool TargetFileExists { get; init; }

        public bool OverwriteExisting { get; init; }

        public bool OpenExistingRequested { get; init; }
    }

    public sealed class KnowledgeBaseActDocumentPathService
    {
        private const int MaxFileStemLength = 140;
        private const int MaxEquipmentPartLength = 70;

        private readonly Func<string, bool> _fileExists;

        public KnowledgeBaseActDocumentPathService(Func<string, bool>? fileExists = null)
        {
            _fileExists = fileExists ?? File.Exists;
        }

        public KnowledgeBaseActDocumentPathResult PrepareDocumentPath(KnowledgeBaseActDocumentPathRequest request)
        {
            if (request.Act == null)
                return Failure("Не переданы данные акта.");

            if (string.IsNullOrWhiteSpace(request.Act.ActNumber))
                return Failure("Акту не присвоен номер.");

            string baseDirectory = ResolveBaseDirectory(request.DatabasePath, request.ApplicationBasePath);
            string documentsDirectory = ResolveDocumentsDirectory(request.Config, baseDirectory);
            string fileName = BuildDocumentFileName(request.Act);
            string absolutePath = string.IsNullOrWhiteSpace(request.SelectedPath)
                ? Path.GetFullPath(Path.Combine(documentsDirectory, fileName))
                : Path.GetFullPath(EnsureDocxExtension(request.SelectedPath));
            string storedPath = BuildStoredPath(absolutePath, baseDirectory);
            string selectedDirectory = Path.GetDirectoryName(absolutePath) ?? documentsDirectory;
            string storedDirectoryPath = BuildStoredPath(selectedDirectory, baseDirectory);

            bool targetFileExists = _fileExists(absolutePath);
            if (targetFileExists && !request.AllowExistingFile)
                return Failure($"Файл документа уже существует: {absolutePath}");

            KbActDocument? conflictingDocument = FindConflictingDocument(
                request.ExistingDocuments,
                request.Act.ActId,
                absolutePath,
                baseDirectory);
            if (conflictingDocument != null)
                return Failure($"Путь документа уже используется другим актом: {conflictingDocument.Path}");

            return new KnowledgeBaseActDocumentPathResult
            {
                IsSuccess = true,
                FileName = Path.GetFileName(absolutePath),
                AbsolutePath = absolutePath,
                StoredPath = storedPath,
                StoredDirectoryPath = storedDirectoryPath,
                TargetFileExists = targetFileExists
            };
        }

        public static string BuildDocumentFileName(KbAct act)
        {
            string actNumber = SanitizeFileNamePart(act.ActNumber);
            string actType = SanitizeFileNamePart(GetActTypeFileNamePart(act.ActType));
            string equipmentName = SanitizeFileNamePart(BuildShortEquipmentName(act));
            if (string.IsNullOrWhiteSpace(equipmentName))
                equipmentName = "Оборудование";

            equipmentName = Shorten(equipmentName, MaxEquipmentPartLength);
            string stem = $"{actNumber}_{actType}_{equipmentName}";
            if (stem.Length > MaxFileStemLength)
            {
                int availableEquipmentLength = Math.Max(20, MaxFileStemLength - actNumber.Length - actType.Length - 2);
                stem = $"{actNumber}_{actType}_{Shorten(equipmentName, availableEquipmentLength)}";
            }

            return $"{stem}.docx";
        }

        public static string ResolveDocumentsDirectory(KbConfig? config, string baseDirectory)
        {
            string configuredPath = string.IsNullOrWhiteSpace(config?.ActDocumentsDirectoryPath)
                ? KbConfig.DefaultActDocumentsDirectoryPath
                : config.ActDocumentsDirectoryPath.Trim();

            return Path.GetFullPath(Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(baseDirectory, configuredPath));
        }

        public static string BuildStoredPath(string absolutePath, string baseDirectory)
        {
            string fullPath = Path.GetFullPath(absolutePath);
            string fullBaseDirectory = Path.GetFullPath(baseDirectory);
            string relativePath = Path.GetRelativePath(fullBaseDirectory, fullPath);
            return IsSafeRelativePath(relativePath)
                ? relativePath
                : fullPath;
        }

        private static string ResolveBaseDirectory(string databasePath, string applicationBasePath)
        {
            string? databaseDirectory = string.IsNullOrWhiteSpace(databasePath)
                ? null
                : Path.GetDirectoryName(Path.GetFullPath(databasePath));
            if (!string.IsNullOrWhiteSpace(databaseDirectory))
                return databaseDirectory;

            return string.IsNullOrWhiteSpace(applicationBasePath)
                ? AppContext.BaseDirectory
                : Path.GetFullPath(applicationBasePath);
        }

        private static string BuildShortEquipmentName(KbAct act)
        {
            string source = !string.IsNullOrWhiteSpace(act.EquipmentName)
                ? act.EquipmentName
                : act.EquipmentSnapshot?.Model ?? string.Empty;
            if (string.IsNullOrWhiteSpace(source))
                source = act.EquipmentSnapshot?.OrderNumber ?? string.Empty;

            string[] segments = source
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static segment => !string.IsNullOrWhiteSpace(segment))
                .Take(3)
                .ToArray();

            return segments.Length == 0
                ? source
                : string.Join(" ", segments);
        }

        private static string GetActTypeFileNamePart(KbActType actType) =>
            actType switch
            {
                KbActType.InspectionWork => "Осмотр",
                KbActType.EquipmentFailure => "Отказ оборудования",
                _ => "Акт"
            };

        private static string SanitizeFileNamePart(string? value)
        {
            string source = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(source))
                return string.Empty;

            var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
            var builder = new StringBuilder(source.Length);
            bool previousWasSeparator = false;
            foreach (char ch in source)
            {
                bool isSeparator = char.IsWhiteSpace(ch) || invalidChars.Contains(ch);
                if (isSeparator)
                {
                    if (!previousWasSeparator && builder.Length > 0)
                        builder.Append('_');

                    previousWasSeparator = true;
                    continue;
                }

                builder.Append(ch);
                previousWasSeparator = false;
            }

            return builder
                .ToString()
                .Trim('_', '.', ' ');
        }

        private static string Shorten(string value, int maxLength)
        {
            if (value.Length <= maxLength)
                return value;

            return value[..maxLength].TrimEnd('_', '-', '.', ' ');
        }

        private static string EnsureDocxExtension(string path) =>
            string.Equals(Path.GetExtension(path), ".docx", StringComparison.OrdinalIgnoreCase)
                ? path
                : Path.ChangeExtension(path, ".docx");

        private static bool IsSafeRelativePath(string relativePath) =>
            !string.IsNullOrWhiteSpace(relativePath) &&
            !Path.IsPathRooted(relativePath) &&
            !relativePath.Equals("..", StringComparison.Ordinal) &&
            !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
            !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);

        private static KbActDocument? FindConflictingDocument(
            IEnumerable<KbActDocument>? documents,
            string currentActId,
            string absolutePath,
            string baseDirectory)
        {
            if (documents == null)
                return null;

            string normalizedTargetPath = NormalizePathForComparison(absolutePath);
            foreach (KbActDocument document in documents)
            {
                if (document == null ||
                    string.IsNullOrWhiteSpace(document.Path) ||
                    string.Equals(document.ActId, currentActId, StringComparison.Ordinal))
                {
                    continue;
                }

                string existingAbsolutePath = Path.IsPathRooted(document.Path)
                    ? document.Path
                    : Path.Combine(baseDirectory, document.Path);
                if (string.Equals(
                    NormalizePathForComparison(existingAbsolutePath),
                    normalizedTargetPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return document;
                }
            }

            return null;
        }

        private static string NormalizePathForComparison(string path) =>
            Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        private static KnowledgeBaseActDocumentPathResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
