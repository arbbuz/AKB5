using System.Globalization;
using Microsoft.Data.Sqlite;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseExternalBackupResult
    {
        public bool IsSuccess { get; init; }

        public bool BackupCreated { get; init; }

        public string BackupPath { get; init; } = string.Empty;

        public string? ErrorMessage { get; init; }
    }

    public sealed class KnowledgeBaseExternalBackupService
    {
        public const string BackupDirectoryName = "backups";

        private readonly Func<DateTimeOffset> _clock;

        public KnowledgeBaseExternalBackupService(Func<DateTimeOffset>? clock = null)
        {
            _clock = clock ?? (() => DateTimeOffset.Now);
        }

        public KnowledgeBaseExternalBackupResult CreateSqliteBackup(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return new KnowledgeBaseExternalBackupResult { IsSuccess = true };

            try
            {
                string backupPath = BuildBackupPath(sourcePath, _clock());
                string? backupDirectory = Path.GetDirectoryName(backupPath);
                if (!string.IsNullOrWhiteSpace(backupDirectory))
                    Directory.CreateDirectory(backupDirectory);

                var sourceBuilder = new SqliteConnectionStringBuilder
                {
                    DataSource = sourcePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                };
                var backupBuilder = new SqliteConnectionStringBuilder
                {
                    DataSource = backupPath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Pooling = false
                };

                using var sourceConnection = new SqliteConnection(sourceBuilder.ToString());
                using var backupConnection = new SqliteConnection(backupBuilder.ToString());
                sourceConnection.Open();
                backupConnection.Open();
                sourceConnection.BackupDatabase(backupConnection);

                return new KnowledgeBaseExternalBackupResult
                {
                    IsSuccess = true,
                    BackupCreated = true,
                    BackupPath = backupPath
                };
            }
            catch (Exception ex)
            {
                return new KnowledgeBaseExternalBackupResult
                {
                    ErrorMessage = ex.Message
                };
            }
        }

        public string BuildBackupPath(string sourcePath, DateTimeOffset createdAt)
        {
            string sourceDirectory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
            string sourceFileName = Path.GetFileNameWithoutExtension(sourcePath);
            string sourceExtension = Path.GetExtension(sourcePath);
            string dateDirectory = createdAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string timestamp = createdAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string backupDirectory = Path.Combine(sourceDirectory, BackupDirectoryName, dateDirectory);
            string basePath = Path.Combine(backupDirectory, $"{sourceFileName}-{timestamp}{sourceExtension}");

            if (!File.Exists(basePath))
                return basePath;

            for (int index = 1; index <= 999; index++)
            {
                string candidate = Path.Combine(
                    backupDirectory,
                    $"{sourceFileName}-{timestamp}-{index:000}{sourceExtension}");
                if (!File.Exists(candidate))
                    return candidate;
            }

            return Path.Combine(
                backupDirectory,
                $"{sourceFileName}-{timestamp}-{Guid.NewGuid():N}{sourceExtension}");
        }
    }
}
