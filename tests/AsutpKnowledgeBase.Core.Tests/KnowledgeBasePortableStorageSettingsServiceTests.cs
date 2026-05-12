using System.Text.Json;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBasePortableStorageSettingsServiceTests
{
    [Fact]
    public void Load_WhenSettingsMissing_ReturnsFileMissing()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            var service = new KnowledgeBasePortableStorageSettingsService(tempDirectory);

            KnowledgeBasePortableStorageSettingsLoadResult result = service.Load();

            Assert.True(result.FileMissing);
            Assert.False(result.IsSuccess);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveDatabasePath_WhenPathIsUnderApplicationDirectory_StoresRelativePath()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            var service = new KnowledgeBasePortableStorageSettingsService(tempDirectory);
            string databasePath = Path.Combine(tempDirectory, "database", "knowledge-base.akb");

            Assert.True(service.SaveDatabasePath(databasePath, out string? errorMessage), errorMessage);

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(service.SettingsPath));
            Assert.Equal(
                Path.Combine("database", "knowledge-base.akb"),
                document.RootElement.GetProperty("DatabasePath").GetString());

            KnowledgeBasePortableStorageSettingsLoadResult loadResult = service.Load();
            Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
            Assert.Equal(databasePath, service.ResolveDatabasePath(loadResult.Settings!));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveDatabasePath_WhenPathIsOutsideApplicationDirectory_StoresAbsolutePath()
    {
        string appDirectory = CreateTempDirectory();
        string dataDirectory = CreateTempDirectory();

        try
        {
            var service = new KnowledgeBasePortableStorageSettingsService(appDirectory);
            string databasePath = Path.Combine(dataDirectory, "knowledge-base.akb");

            Assert.True(service.SaveDatabasePath(databasePath, out string? errorMessage), errorMessage);

            KnowledgeBasePortableStorageSettingsLoadResult loadResult = service.Load();
            Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
            Assert.Equal(databasePath, loadResult.Settings!.DatabasePath);
            Assert.Equal(databasePath, service.ResolveDatabasePath(loadResult.Settings));
        }
        finally
        {
            Directory.Delete(appDirectory, recursive: true);
            Directory.Delete(dataDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"akb-portable-settings-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
