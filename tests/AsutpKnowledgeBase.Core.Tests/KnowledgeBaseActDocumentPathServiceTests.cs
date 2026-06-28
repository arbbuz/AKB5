using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseActDocumentPathServiceTests
{
    [Fact]
    public void PrepareDocumentPath_UsesDefaultRelativeDocumentsDirectory()
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), $"akb5-act-docs-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(baseDirectory, "knowledge-base.akb");
        var service = new KnowledgeBaseActDocumentPathService();

        KnowledgeBaseActDocumentPathResult result = service.PrepareDocumentPath(
            new KnowledgeBaseActDocumentPathRequest
            {
                Act = CreateAct(),
                Config = KnowledgeBaseDataService.CreateDefaultConfig(),
                DatabasePath = databasePath,
                ApplicationBasePath = baseDirectory
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(
            @"Documents\Acts\2026-0001_Отказ_оборудования_SIMATIC_S7-300_PS_307_БЛОК_ПИТАНИЯ.docx",
            result.StoredPath);
        Assert.Equal(
            Path.Combine(
                baseDirectory,
                "Documents",
                "Acts",
                "2026-0001_Отказ_оборудования_SIMATIC_S7-300_PS_307_БЛОК_ПИТАНИЯ.docx"),
            result.AbsolutePath);
    }

    [Fact]
    public void BuildDocumentFileName_SanitizesInvalidCharactersAndShortensLongEquipmentName()
    {
        KbAct act = CreateAct();
        act.EquipmentName = "Модуль: DI/16? очень длинное описание, сегмент 2, сегмент 3, лишний сегмент";

        string fileName = KnowledgeBaseActDocumentPathService.BuildDocumentFileName(act);

        Assert.EndsWith(".docx", fileName, StringComparison.Ordinal);
        Assert.DoesNotContain(":", fileName, StringComparison.Ordinal);
        Assert.DoesNotContain("/", fileName, StringComparison.Ordinal);
        Assert.DoesNotContain("?", fileName, StringComparison.Ordinal);
        Assert.StartsWith("2026-0001_Отказ_оборудования_", fileName, StringComparison.Ordinal);
        Assert.DoesNotContain("лишний", fileName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrepareDocumentPath_UsesSelectedPathAndStoresLastDirectory()
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), $"akb5-act-docs-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(baseDirectory, "knowledge-base.akb");
        string selectedPath = Path.Combine(baseDirectory, "CustomActs", "act-file");
        var service = new KnowledgeBaseActDocumentPathService();

        KnowledgeBaseActDocumentPathResult result = service.PrepareDocumentPath(
            new KnowledgeBaseActDocumentPathRequest
            {
                Act = CreateAct(),
                Config = KnowledgeBaseDataService.CreateDefaultConfig(),
                DatabasePath = databasePath,
                ApplicationBasePath = baseDirectory,
                SelectedPath = selectedPath
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(@"CustomActs\act-file.docx", result.StoredPath);
        Assert.Equal(@"CustomActs", result.StoredDirectoryPath);
        Assert.Equal(Path.Combine(baseDirectory, "CustomActs", "act-file.docx"), result.AbsolutePath);
    }

    [Fact]
    public void PrepareDocumentPath_WhenFileExists_ReturnsCollisionError()
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), $"akb5-act-docs-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(baseDirectory, "knowledge-base.akb");
        string expectedAbsolutePath = Path.Combine(
            baseDirectory,
            "Documents",
            "Acts",
            "2026-0001_Отказ_оборудования_SIMATIC_S7-300_PS_307_БЛОК_ПИТАНИЯ.docx");
        var service = new KnowledgeBaseActDocumentPathService(
            fileExists: path => string.Equals(path, expectedAbsolutePath, StringComparison.OrdinalIgnoreCase));

        KnowledgeBaseActDocumentPathResult result = service.PrepareDocumentPath(
            new KnowledgeBaseActDocumentPathRequest
            {
                Act = CreateAct(),
                Config = KnowledgeBaseDataService.CreateDefaultConfig(),
                DatabasePath = databasePath,
                ApplicationBasePath = baseDirectory
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("уже существует", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrepareDocumentPath_WhenExistingFileAllowed_ReturnsTargetFileExists()
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), $"akb5-act-docs-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(baseDirectory, "knowledge-base.akb");
        string expectedAbsolutePath = Path.Combine(
            baseDirectory,
            "Documents",
            "Acts",
            "2026-0001_Отказ_оборудования_SIMATIC_S7-300_PS_307_БЛОК_ПИТАНИЯ.docx");
        var service = new KnowledgeBaseActDocumentPathService(
            fileExists: path => string.Equals(path, expectedAbsolutePath, StringComparison.OrdinalIgnoreCase));

        KnowledgeBaseActDocumentPathResult result = service.PrepareDocumentPath(
            new KnowledgeBaseActDocumentPathRequest
            {
                Act = CreateAct(),
                Config = KnowledgeBaseDataService.CreateDefaultConfig(),
                DatabasePath = databasePath,
                ApplicationBasePath = baseDirectory,
                AllowExistingFile = true
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.TargetFileExists);
        Assert.Equal(expectedAbsolutePath, result.AbsolutePath);
    }

    [Fact]
    public void PrepareDocumentPath_WhenAnotherActUsesPath_ReturnsCollisionError()
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), $"akb5-act-docs-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(baseDirectory, "knowledge-base.akb");
        var service = new KnowledgeBaseActDocumentPathService();

        KnowledgeBaseActDocumentPathResult result = service.PrepareDocumentPath(
            new KnowledgeBaseActDocumentPathRequest
            {
                Act = CreateAct(),
                Config = KnowledgeBaseDataService.CreateDefaultConfig(),
                DatabasePath = databasePath,
                ApplicationBasePath = baseDirectory,
                ExistingDocuments = new[]
                {
                    new KbActDocument
                    {
                        ActId = "another-act",
                        Path = @"Documents\Acts\2026-0001_Отказ_оборудования_SIMATIC_S7-300_PS_307_БЛОК_ПИТАНИЯ.docx"
                    }
                }
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("используется другим актом", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static KbAct CreateAct() =>
        new()
        {
            ActId = "act-1",
            ActYear = 2026,
            ActNumber = "2026-0001",
            ActType = KbActType.EquipmentFailure,
            ActDate = new DateTime(2026, 6, 26),
            EquipmentName = "SIMATIC S7-300, PS 307, БЛОК ПИТАНИЯ, ВХОД: ~120/230В",
            EquipmentSnapshot = new KbActEquipmentSnapshot
            {
                Model = "PS 307",
                OrderNumber = "6ES7307-1BA00-0AA0"
            }
        };
}
