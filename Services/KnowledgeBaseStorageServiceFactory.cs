namespace AsutpKnowledgeBase.Services
{
    public static class KnowledgeBaseStorageServiceFactory
    {
        public static IKnowledgeBaseStorageService CreateFileStorage(
            string savePath,
            IAppLogger? logger = null) =>
            new KnowledgeBaseRoutedStorageService(savePath, logger);

        public static IKnowledgeBaseStorageService CreateLegacyJsonFileStorage(
            string savePath,
            IAppLogger? logger = null) =>
            new JsonStorageService(savePath, logger);

        public static IKnowledgeBaseStorageService CreateSqliteStorage(
            string savePath,
            IAppLogger? logger = null) =>
            new SqliteKnowledgeBaseStorageService(savePath, logger);
    }
}
