using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseSnapshotMetadata
    {
        public string SnapshotFileName { get; init; } = string.Empty;

        public string SourcePath { get; init; } = string.Empty;

        public string Kind { get; init; } = string.Empty;

        public string Note { get; init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public long SizeBytes { get; init; }
    }

    public sealed class KnowledgeBaseSnapshotEntry
    {
        public string SnapshotId { get; init; } = string.Empty;

        public string SnapshotPath { get; init; } = string.Empty;

        public string MetadataPath { get; init; } = string.Empty;

        public string SnapshotFileName { get; init; } = string.Empty;

        public string SourcePath { get; init; } = string.Empty;

        public string Kind { get; init; } = string.Empty;

        public string Note { get; init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public long SizeBytes { get; init; }

        public bool HasMetadata { get; init; }
    }

    public sealed class KnowledgeBaseSnapshotCreateResult
    {
        public bool IsSuccess { get; init; }

        public bool IsSkipped { get; init; }

        public string SnapshotPath { get; init; } = string.Empty;

        public string MetadataPath { get; init; } = string.Empty;

        public string? ErrorMessage { get; init; }

        public long SizeBytes { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }

    public sealed class KnowledgeBaseSnapshotListResult
    {
        public bool IsSuccess { get; init; }

        public string SnapshotDirectoryPath { get; init; } = string.Empty;

        public IReadOnlyList<KnowledgeBaseSnapshotEntry> Snapshots { get; init; } =
            Array.Empty<KnowledgeBaseSnapshotEntry>();

        public string? ErrorMessage { get; init; }
    }

    public sealed class KnowledgeBaseSnapshotDataResult
    {
        public bool IsSuccess { get; init; }

        public string SnapshotPath { get; init; } = string.Empty;

        public SavedData? Data { get; init; }

        public string? ErrorMessage { get; init; }
    }

    public sealed class KnowledgeBaseSnapshotRestoreResult
    {
        public bool IsSuccess { get; init; }

        public string SnapshotPath { get; init; } = string.Empty;

        public string ProtectiveSnapshotPath { get; init; } = string.Empty;

        public SavedData? RestoredData { get; init; }

        public string? ErrorMessage { get; init; }
    }

    public class KnowledgeBaseSnapshotService
    {
        public const string SnapshotDirectoryName = ".akb-snapshots";

        private static readonly JsonSerializerOptions MetadataSerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        private readonly Func<DateTimeOffset> _clock;

        public KnowledgeBaseSnapshotService(Func<DateTimeOffset>? clock = null)
        {
            _clock = clock ?? (() => DateTimeOffset.Now);
        }

        public KnowledgeBaseSnapshotCreateResult CreateAutomaticSnapshot(
            string sourceJsonPath,
            string reason)
        {
            string normalizedSourcePath = sourceJsonPath?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedSourcePath))
            {
                return Failure("Не указан путь к JSON-файлу для создания снимка.");
            }

            if (!File.Exists(normalizedSourcePath))
            {
                return new KnowledgeBaseSnapshotCreateResult
                {
                    IsSkipped = true,
                    ErrorMessage = "Исходный JSON-файл еще не существует."
                };
            }

            try
            {
                string fullSourcePath = Path.GetFullPath(normalizedSourcePath);
                string sourceDirectory =
                    Path.GetDirectoryName(fullSourcePath) ??
                    throw new InvalidOperationException("Не удалось определить каталог JSON-файла.");
                string snapshotDirectory = Path.Combine(sourceDirectory, SnapshotDirectoryName);
                Directory.CreateDirectory(snapshotDirectory);

                DateTimeOffset createdAt = _clock();
                string snapshotPath = BuildUniqueSnapshotPath(
                    snapshotDirectory,
                    fullSourcePath,
                    createdAt,
                    reason);

                using (var source = new FileStream(
                           fullSourcePath,
                           FileMode.Open,
                           FileAccess.Read,
                           FileShare.ReadWrite | FileShare.Delete))
                using (var destination = new FileStream(
                           snapshotPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.Read))
                {
                    source.CopyTo(destination);
                }

                return new KnowledgeBaseSnapshotCreateResult
                {
                    IsSuccess = true,
                    SnapshotPath = snapshotPath,
                    CreatedAt = createdAt,
                    SizeBytes = new FileInfo(snapshotPath).Length
                };
            }
            catch (Exception ex)
            {
                return Failure($"Не удалось создать снимок JSON-файла: {ex.Message}");
            }
        }

        public KnowledgeBaseSnapshotCreateResult CreateManualSnapshot(
            string sourceJsonPath,
            string json,
            string note)
        {
            string normalizedSourcePath = sourceJsonPath?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedSourcePath))
            {
                return Failure("Не указан путь к JSON-файлу для создания снимка.");
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return Failure("Содержимое снимка пустое.");
            }

            string normalizedNote = note?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedNote))
            {
                return Failure("Укажите примечание к снимку.");
            }

            try
            {
                string fullSourcePath = Path.GetFullPath(normalizedSourcePath);
                string sourceDirectory =
                    Path.GetDirectoryName(fullSourcePath) ??
                    throw new InvalidOperationException("Не удалось определить каталог JSON-файла.");
                string snapshotDirectory = Path.Combine(sourceDirectory, SnapshotDirectoryName);
                Directory.CreateDirectory(snapshotDirectory);

                DateTimeOffset createdAt = _clock();
                string snapshotPath = BuildUniqueSnapshotPath(
                    snapshotDirectory,
                    fullSourcePath,
                    createdAt,
                    "manual");

                File.WriteAllText(snapshotPath, json);
                long sizeBytes = new FileInfo(snapshotPath).Length;
                string metadataPath = $"{snapshotPath}.meta.json";
                var metadata = new KnowledgeBaseSnapshotMetadata
                {
                    SnapshotFileName = Path.GetFileName(snapshotPath),
                    SourcePath = fullSourcePath,
                    Kind = "manual",
                    Note = normalizedNote,
                    CreatedAt = createdAt,
                    SizeBytes = sizeBytes
                };
                File.WriteAllText(
                    metadataPath,
                    JsonSerializer.Serialize(metadata, MetadataSerializerOptions));

                return new KnowledgeBaseSnapshotCreateResult
                {
                    IsSuccess = true,
                    SnapshotPath = snapshotPath,
                    MetadataPath = metadataPath,
                    CreatedAt = createdAt,
                    SizeBytes = sizeBytes
                };
            }
            catch (Exception ex)
            {
                return Failure($"Не удалось создать снимок JSON-файла: {ex.Message}");
            }
        }

        public KnowledgeBaseSnapshotListResult ListSnapshots(string sourceJsonPath)
        {
            string normalizedSourcePath = sourceJsonPath?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedSourcePath))
            {
                return ListFailure(
                    string.Empty,
                    "Не указан путь к JSON-файлу для просмотра снимков.");
            }

            try
            {
                string fullSourcePath = Path.GetFullPath(normalizedSourcePath);
                string sourceDirectory =
                    Path.GetDirectoryName(fullSourcePath) ??
                    throw new InvalidOperationException("Не удалось определить каталог JSON-файла.");
                string snapshotDirectory = Path.Combine(sourceDirectory, SnapshotDirectoryName);

                if (!Directory.Exists(snapshotDirectory))
                {
                    return new KnowledgeBaseSnapshotListResult
                    {
                        IsSuccess = true,
                        SnapshotDirectoryPath = snapshotDirectory
                    };
                }

                var snapshots = Directory
                    .EnumerateFiles(snapshotDirectory, "*.json", SearchOption.TopDirectoryOnly)
                    .Where(static path => !path.EndsWith(".meta.json", StringComparison.OrdinalIgnoreCase))
                    .Select(path => BuildSnapshotEntry(path, fullSourcePath))
                    .OrderByDescending(static entry => entry.CreatedAt)
                    .ThenByDescending(static entry => entry.SnapshotFileName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new KnowledgeBaseSnapshotListResult
                {
                    IsSuccess = true,
                    SnapshotDirectoryPath = snapshotDirectory,
                    Snapshots = snapshots
                };
            }
            catch (Exception ex)
            {
                return ListFailure(
                    string.Empty,
                    $"Не удалось прочитать список снимков JSON-файла: {ex.Message}");
            }
        }

        private static KnowledgeBaseSnapshotCreateResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseSnapshotListResult ListFailure(
            string snapshotDirectoryPath,
            string errorMessage) =>
            new()
            {
                IsSuccess = false,
                SnapshotDirectoryPath = snapshotDirectoryPath,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseSnapshotEntry BuildSnapshotEntry(
            string snapshotPath,
            string fallbackSourcePath)
        {
            var fileInfo = new FileInfo(snapshotPath);
            string metadataPath = $"{snapshotPath}.meta.json";
            KnowledgeBaseSnapshotMetadata? metadata = TryReadMetadata(metadataPath);
            DateTimeOffset createdAt = ResolveCreatedAt(fileInfo, metadata);

            return new KnowledgeBaseSnapshotEntry
            {
                SnapshotPath = fileInfo.FullName,
                MetadataPath = File.Exists(metadataPath) ? metadataPath : string.Empty,
                SnapshotFileName = ResolveSnapshotFileName(fileInfo, metadata),
                SourcePath = string.IsNullOrWhiteSpace(metadata?.SourcePath)
                    ? fallbackSourcePath
                    : metadata.SourcePath.Trim(),
                Kind = ResolveSnapshotKind(fileInfo, metadata),
                Note = metadata?.Note?.Trim() ?? string.Empty,
                CreatedAt = createdAt,
                SizeBytes = fileInfo.Exists ? fileInfo.Length : Math.Max(0, metadata?.SizeBytes ?? 0),
                HasMetadata = metadata != null
            };
        }

        private static KnowledgeBaseSnapshotMetadata? TryReadMetadata(string metadataPath)
        {
            if (!File.Exists(metadataPath))
                return null;

            try
            {
                return JsonSerializer.Deserialize<KnowledgeBaseSnapshotMetadata>(
                    File.ReadAllText(metadataPath),
                    MetadataSerializerOptions);
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveSnapshotFileName(
            FileInfo fileInfo,
            KnowledgeBaseSnapshotMetadata? metadata)
        {
            string fileName = metadata?.SnapshotFileName?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(fileName) ? fileInfo.Name : fileName;
        }

        private static string ResolveSnapshotKind(
            FileInfo fileInfo,
            KnowledgeBaseSnapshotMetadata? metadata)
        {
            string kind = metadata?.Kind?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(kind))
                return kind;

            if (TryParseSnapshotFileName(fileInfo.Name, out _, out kind))
                return kind;

            return "snapshot";
        }

        private static DateTimeOffset ResolveCreatedAt(
            FileInfo fileInfo,
            KnowledgeBaseSnapshotMetadata? metadata)
        {
            if (metadata != null && metadata.CreatedAt != default)
                return metadata.CreatedAt;

            if (TryParseSnapshotFileName(fileInfo.Name, out DateTimeOffset createdAt, out _))
                return createdAt;

            return fileInfo.Exists
                ? new DateTimeOffset(fileInfo.LastWriteTime)
                : DateTimeOffset.MinValue;
        }

        private static bool TryParseSnapshotFileName(
            string fileName,
            out DateTimeOffset createdAt,
            out string kind)
        {
            createdAt = default;
            kind = string.Empty;

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string[] parts = nameWithoutExtension.Split('.', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!DateTimeOffset.TryParseExact(
                        parts[i],
                        "yyyyMMdd-HHmmss-fff'Z'",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out DateTimeOffset parsedCreatedAt))
                {
                    continue;
                }

                createdAt = parsedCreatedAt;
                kind = TrimAttemptSuffix(parts[i + 1]);
                return true;
            }

            return false;
        }

        private static string TrimAttemptSuffix(string value)
        {
            int hyphenIndex = value.LastIndexOf('-');
            if (hyphenIndex <= 0 || hyphenIndex == value.Length - 1)
                return value;

            return int.TryParse(
                value[(hyphenIndex + 1)..],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out _)
                ? value[..hyphenIndex]
                : value;
        }

        private static string BuildUniqueSnapshotPath(
            string snapshotDirectory,
            string sourcePath,
            DateTimeOffset createdAt,
            string reason)
        {
            string sourceName = Path.GetFileNameWithoutExtension(sourcePath);
            if (string.IsNullOrWhiteSpace(sourceName))
                sourceName = "knowledge-base";

            string sourceExtension = Path.GetExtension(sourcePath);
            if (string.IsNullOrWhiteSpace(sourceExtension))
                sourceExtension = ".json";

            string normalizedReason = NormalizeFileNamePart(reason, fallback: "snapshot");
            string timestamp = createdAt
                .ToUniversalTime()
                .ToString("yyyyMMdd-HHmmss-fff'Z'", CultureInfo.InvariantCulture);
            string baseName = $"{sourceName}.{timestamp}.{normalizedReason}";

            for (int attempt = 0; attempt < 1000; attempt++)
            {
                string suffix = attempt == 0 ? string.Empty : $"-{attempt + 1}";
                string candidate = Path.Combine(snapshotDirectory, $"{baseName}{suffix}{sourceExtension}");
                if (!File.Exists(candidate))
                    return candidate;
            }

            throw new IOException("Не удалось подобрать свободное имя файла снимка.");
        }

        private static string NormalizeFileNamePart(string? value, string fallback)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                normalized = fallback;

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                normalized = normalized.Replace(invalidChar, '-');

            normalized = normalized.Replace(' ', '-');
            return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }
    }
}
