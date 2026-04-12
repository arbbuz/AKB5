using System.Text.Json;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class FileAppLoggerTests
{
    [Fact]
    public void Log_WritesNdjsonEntryAndCleansUpOldLogs()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string expiredLogPath = Path.Combine(tempDirectory, "app-2000-01-01.ndjson");
            File.WriteAllText(expiredLogPath, "{}");

            var logger = new FileAppLogger(tempDirectory);

            logger.Log(
                "TestEvent",
                AppLogLevel.Information,
                "Test message.",
                new InvalidOperationException("boom"),
                new Dictionary<string, object?>
                {
                    ["path"] = "kb.json",
                    ["usedBackup"] = true
                });

            string logPath = Path.Combine(tempDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.ndjson");
            string[] lines = File.ReadAllLines(logPath);

            Assert.False(File.Exists(expiredLogPath));
            Assert.Single(lines);

            using JsonDocument document = JsonDocument.Parse(lines[0]);
            JsonElement root = document.RootElement;

            Assert.Equal("TestEvent", root.GetProperty("event").GetString());
            Assert.Equal("Information", root.GetProperty("level").GetString());
            Assert.Equal("Test message.", root.GetProperty("message").GetString());
            Assert.Equal("kb.json", root.GetProperty("properties").GetProperty("path").GetString());
            Assert.True(root.GetProperty("properties").GetProperty("usedBackup").GetBoolean());
            Assert.Equal(
                typeof(InvalidOperationException).FullName,
                root.GetProperty("exception").GetProperty("type").GetString());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-file-logger-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
