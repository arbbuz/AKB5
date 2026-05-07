using System.Globalization;

namespace AsutpKnowledgeBase.Services
{
    public static class KnowledgeBaseStoragePaths
    {
        public const string ApplicationFolderName = "AKB5";
        public const string DefaultSqliteFileName = "knowledge-base.akb";
        public const string LegacyJsonFileName = "ASUTP_KnowledgeBase.json";
        public const string SqliteExtension = ".akb";
        public const string JsonExtension = ".json";

        public static string GetDefaultSqlitePath() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ApplicationFolderName,
                DefaultSqliteFileName);

        public static string GetLegacyJsonPath() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                LegacyJsonFileName);

        public static bool IsSqlitePath(string? path) =>
            string.Equals(Path.GetExtension(path), SqliteExtension, StringComparison.OrdinalIgnoreCase);

        public static string BuildPostMigrationJsonExportPath(string sqlitePath, DateTime createdAt)
        {
            string directory = Path.GetDirectoryName(sqlitePath) ?? string.Empty;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sqlitePath);
            string timestamp = createdAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            return Path.Combine(directory, $"{fileNameWithoutExtension}.migration-{timestamp}.json");
        }
    }
}
