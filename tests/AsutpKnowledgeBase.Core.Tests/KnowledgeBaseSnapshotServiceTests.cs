using System.Text.Json;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseSnapshotServiceTests
{
    [Fact]
    public void CreateAutomaticSnapshot_CopiesSourceJsonIntoTimestampedSnapshotDirectory()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            File.WriteAllText(path, """{"LastWorkshop":"Цех 1"}""");
            var service = new KnowledgeBaseSnapshotService(
                () => new DateTimeOffset(2026, 5, 6, 8, 9, 10, 123, TimeSpan.Zero));

            KnowledgeBaseSnapshotCreateResult result =
                service.CreateAutomaticSnapshot(path, "manual test");

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.False(result.IsSkipped);
            Assert.Equal(new FileInfo(path).Length, result.SizeBytes);
            Assert.Equal(File.ReadAllText(path), File.ReadAllText(result.SnapshotPath));
            Assert.Equal(
                Path.Combine(tempDirectory, KnowledgeBaseSnapshotService.SnapshotDirectoryName),
                Path.GetDirectoryName(result.SnapshotPath));
            Assert.Equal(
                "kb.20260506-080910-123Z.manual-test.json",
                Path.GetFileName(result.SnapshotPath));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void CreateAutomaticSnapshot_WhenSourceIsMissing_Skips()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "missing.json");
            var service = new KnowledgeBaseSnapshotService();

            KnowledgeBaseSnapshotCreateResult result =
                service.CreateAutomaticSnapshot(path, "before-save");

            Assert.False(result.IsSuccess);
            Assert.True(result.IsSkipped);
            Assert.Empty(result.SnapshotPath);
            Assert.False(Directory.Exists(Path.Combine(
                tempDirectory,
                KnowledgeBaseSnapshotService.SnapshotDirectoryName)));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ListSnapshots_WhenSnapshotDirectoryIsMissing_ReturnsEmptyList()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new KnowledgeBaseSnapshotService();

            KnowledgeBaseSnapshotListResult result = service.ListSnapshots(path);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Empty(result.Snapshots);
            Assert.Equal(
                Path.Combine(tempDirectory, KnowledgeBaseSnapshotService.SnapshotDirectoryName),
                result.SnapshotDirectoryPath);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void CreateManualSnapshot_WritesSnapshotAndMetadataWithNote()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new KnowledgeBaseSnapshotService(
                () => new DateTimeOffset(2026, 5, 6, 9, 10, 11, 456, TimeSpan.Zero));
            string json = """{"LastWorkshop":"Цех 2"}""";

            KnowledgeBaseSnapshotCreateResult result =
                service.CreateManualSnapshot(path, json, "Перед массовой правкой");

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(json, File.ReadAllText(result.SnapshotPath));
            Assert.Equal(
                "kb.20260506-091011-456Z.manual.json",
                Path.GetFileName(result.SnapshotPath));
            Assert.Equal($"{result.SnapshotPath}.meta.json", result.MetadataPath);

            KnowledgeBaseSnapshotMetadata metadata =
                JsonSerializer.Deserialize<KnowledgeBaseSnapshotMetadata>(File.ReadAllText(result.MetadataPath))!;
            Assert.Equal("manual", metadata.Kind);
            Assert.Equal("Перед массовой правкой", metadata.Note);
            Assert.Equal(Path.GetFullPath(path), metadata.SourcePath);
            Assert.Equal(Path.GetFileName(result.SnapshotPath), metadata.SnapshotFileName);
            Assert.Equal(new FileInfo(result.SnapshotPath).Length, metadata.SizeBytes);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ListSnapshots_ReadsManualMetadataAndAutomaticFallbackSortedNewestFirst()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            File.WriteAllText(path, """{"LastWorkshop":"Цех 1"}""");

            var automaticService = new KnowledgeBaseSnapshotService(
                () => new DateTimeOffset(2026, 5, 6, 8, 0, 0, 0, TimeSpan.Zero));
            KnowledgeBaseSnapshotCreateResult automaticResult =
                automaticService.CreateAutomaticSnapshot(path, "before-save");
            Assert.True(automaticResult.IsSuccess, automaticResult.ErrorMessage);

            var manualService = new KnowledgeBaseSnapshotService(
                () => new DateTimeOffset(2026, 5, 6, 9, 0, 0, 0, TimeSpan.Zero));
            KnowledgeBaseSnapshotCreateResult manualResult =
                manualService.CreateManualSnapshot(path, """{"LastWorkshop":"Цех 2"}""", "Перед импортом");
            Assert.True(manualResult.IsSuccess, manualResult.ErrorMessage);

            KnowledgeBaseSnapshotListResult result = manualService.ListSnapshots(path);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(2, result.Snapshots.Count);

            KnowledgeBaseSnapshotEntry manualEntry = result.Snapshots[0];
            Assert.Equal("manual", manualEntry.Kind);
            Assert.Equal("Перед импортом", manualEntry.Note);
            Assert.True(manualEntry.HasMetadata);
            Assert.Equal(manualResult.SnapshotPath, manualEntry.SnapshotPath);
            Assert.Equal(manualResult.MetadataPath, manualEntry.MetadataPath);
            Assert.Equal(Path.GetFullPath(path), manualEntry.SourcePath);

            KnowledgeBaseSnapshotEntry automaticEntry = result.Snapshots[1];
            Assert.Equal("before-save", automaticEntry.Kind);
            Assert.Equal(string.Empty, automaticEntry.Note);
            Assert.False(automaticEntry.HasMetadata);
            Assert.Equal(automaticResult.SnapshotPath, automaticEntry.SnapshotPath);
            Assert.Equal(string.Empty, automaticEntry.MetadataPath);
            Assert.Equal(Path.GetFullPath(path), automaticEntry.SourcePath);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void CreateManualSnapshot_WhenNoteIsEmpty_ReturnsFailure()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "kb.json");
            var service = new KnowledgeBaseSnapshotService();

            KnowledgeBaseSnapshotCreateResult result =
                service.CreateManualSnapshot(path, "{}", " ");

            Assert.False(result.IsSuccess);
            Assert.False(result.IsSkipped);
            Assert.Contains("примечание", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "akb-snapshot-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
