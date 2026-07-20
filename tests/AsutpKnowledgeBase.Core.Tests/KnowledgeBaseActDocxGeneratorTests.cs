using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AsutpKnowledgeBase.Core.Tests;

public sealed class KnowledgeBaseActDocxGeneratorTests
{
    [Fact]
    public void Generate_ReplacesPlaceholdersAndBuildsExecutorRows()
    {
        string tempDirectory = CreateTempDirectory();
        string templatePath = Path.Combine(tempDirectory, "template.docx");
        string outputPath = Path.Combine(tempDirectory, "result.docx");
        CreateTemplate(templatePath);
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = CreateAct(),
                Executors =
                [
                    CreateExecutor("executor-2", 2, "Петров", "Петр", "Петрович", "инженер АСУТП"),
                    CreateExecutor("executor-1", 1, "Иванов", "Иван", "Иванович", "электромеханик")
                ],
                TemplatePath = templatePath,
                OutputPath = outputPath
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(File.Exists(outputPath));
        Assert.Equal(outputPath, result.OutputPath);
        Assert.False(string.IsNullOrWhiteSpace(result.ContentHash));

        string documentText = ReadDocumentText(outputPath);
        Assert.Contains("2026-0001", documentText, StringComparison.Ordinal);
        Assert.Contains("Купоросный цех", documentText, StringComparison.Ordinal);
        Assert.Contains("SIMATIC S7-300, PS 307, БЛОК ПИТАНИЯ", documentText, StringComparison.Ordinal);
        Assert.Contains("6ES7307-1BA00-0AA0", documentText, StringComparison.Ordinal);
        Assert.Contains("Иванов Иван Иванович", documentText, StringComparison.Ordinal);
        Assert.Contains("Петров Петр Петрович", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", documentText, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WhenTemplateMissing_ReturnsClearError()
    {
        string tempDirectory = CreateTempDirectory();
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = CreateAct(),
                Executors = [CreateExecutor("executor-1", 1, "Иванов", "Иван", string.Empty, "электромеханик")],
                TemplatePath = Path.Combine(tempDirectory, "missing.docx"),
                OutputPath = Path.Combine(tempDirectory, "result.docx")
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("Шаблон DOCX не найден", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_WhenOutputExists_DoesNotOverwrite()
    {
        string tempDirectory = CreateTempDirectory();
        string templatePath = Path.Combine(tempDirectory, "template.docx");
        string outputPath = Path.Combine(tempDirectory, "result.docx");
        CreateTemplate(templatePath);
        File.WriteAllText(outputPath, "existing");
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = CreateAct(),
                Executors = [CreateExecutor("executor-1", 1, "Иванов", "Иван", string.Empty, "электромеханик")],
                TemplatePath = templatePath,
                OutputPath = outputPath
            });

        Assert.False(result.IsSuccess);
        Assert.Equal("existing", File.ReadAllText(outputPath));
    }

    [Fact]
    public void Generate_WhenOverwriteRequested_ReplacesExistingOutput()
    {
        string tempDirectory = CreateTempDirectory();
        string templatePath = Path.Combine(tempDirectory, "template.docx");
        string outputPath = Path.Combine(tempDirectory, "result.docx");
        CreateTemplate(templatePath);
        File.WriteAllText(outputPath, "existing");
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = CreateAct(),
                Executors = [CreateExecutor("executor-1", 1, "Иванов", "Иван", string.Empty, "электромеханик")],
                TemplatePath = templatePath,
                OutputPath = outputPath,
                OverwriteExisting = true
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(outputPath, result.OutputPath);
        Assert.Contains("2026-0001", ReadDocumentText(outputPath), StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithFixedExecutorPlaceholders_DoesNotRequireExecutorTable()
    {
        string tempDirectory = CreateTempDirectory();
        string templatePath = Path.Combine(tempDirectory, "fixed-executors-template.docx");
        string outputPath = Path.Combine(tempDirectory, "fixed-executors-result.docx");
        CreateFixedExecutorTemplate(templatePath);
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = CreateAct(),
                Executors =
                [
                    CreateExecutor("executor-1", 1, "Иванов", "Иван", "Иванович", "электромеханик"),
                    CreateExecutor("executor-2", 2, "Петров", "Петр", "Петрович", "инженер АСУТП")
                ],
                TemplatePath = templatePath,
                OutputPath = outputPath
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        string documentText = ReadDocumentText(outputPath);
        Assert.Contains(
            "Купоросный цех (КЦ), АСУ использования конденсатов КЦ для выработки пара в котельной",
            documentText,
            StringComparison.Ordinal);
        Assert.Contains("SN-0001", documentText, StringComparison.Ordinal);
        Assert.Contains("Иванов Иван Иванович", documentText, StringComparison.Ordinal);
        Assert.Contains("электромеханик", documentText, StringComparison.Ordinal);
        Assert.Contains("Петров Петр Петрович", documentText, StringComparison.Ordinal);
        Assert.Contains("инженер АСУТП", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", documentText, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WhenPlaceholderIsUnderlinedRun_PreservesUnderline()
    {
        string tempDirectory = CreateTempDirectory();
        string templatePath = Path.Combine(tempDirectory, "underlined-placeholder-template.docx");
        string outputPath = Path.Combine(tempDirectory, "underlined-placeholder-result.docx");
        CreateUnderlinedPlaceholderTemplate(templatePath);
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = CreateAct(),
                Executors = [],
                TemplatePath = templatePath,
                OutputPath = outputPath
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);

        using WordprocessingDocument document = WordprocessingDocument.Open(outputPath, false);
        Paragraph paragraph = document.MainDocumentPart!.Document.Body!.Descendants<Paragraph>().Single();
        List<Run> runs = paragraph.Elements<Run>().ToList();

        Assert.Contains(
            runs,
            static run => run.InnerText == "Number: " && run.RunProperties?.Underline == null);
        Assert.Contains(
            runs,
            static run => run.InnerText == "2026-0001" &&
                run.RunProperties?.Underline?.Val?.Value == UnderlineValues.Single);
    }

    [Fact]
    public void Generate_WithExternalRepositoryTemplate_CreatesDocument()
    {
        string repositoryRoot = FindRepositoryRoot();
        string templatePath = Path.Combine(
            repositoryRoot,
            "Templates",
            "Acts",
            KnowledgeBaseActDocxTemplateService.GetTemplateFileName(KbActType.EquipmentFailure));
        string outputPath = Path.Combine(CreateTempDirectory(), "external-template-result.docx");
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = CreateAct(),
                Executors =
                [
                    CreateExecutor("executor-1", 1, "Иванов", "Иван", "Иванович", "электромеханик"),
                    CreateExecutor("executor-2", 2, "Петров", "Петр", "Петрович", "инженер АСУТП")
                ],
                TemplatePath = templatePath,
                OutputPath = outputPath
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        string documentText = ReadDocumentText(outputPath);
        Assert.Contains(
            "выхода из строя электрооборудования",
            documentText.Replace("\n", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal);
        Assert.Contains(
            "Купоросный цех (КЦ), АСУ использования конденсатов КЦ для выработки пара в котельной",
            documentText,
            StringComparison.Ordinal);
        Assert.Contains("SIMATIC S7-300, PS 307, БЛОК ПИТАНИЯ", documentText, StringComparison.Ordinal);
        Assert.Contains("6ES7307-1BA00-0AA0", documentText, StringComparison.Ordinal);
        Assert.Contains("SN-0001", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("2026-0001", documentText, StringComparison.Ordinal);
        Assert.Contains("Иванов Иван Иванович", documentText, StringComparison.Ordinal);
        Assert.Contains("Петров Петр Петрович", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("ШКМ1", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("Rack", documentText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Slot", documentText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("3001", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("Плохой отклик", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", documentText, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithExternalInspectionTemplate_CreatesDocumentFromProvidedForm()
    {
        string repositoryRoot = FindRepositoryRoot();
        string templatePath = Path.Combine(
            repositoryRoot,
            "Templates",
            "Acts",
            KnowledgeBaseActDocxTemplateService.GetTemplateFileName(KbActType.InspectionWork));
        string outputPath = Path.Combine(CreateTempDirectory(), "inspection-template-result.docx");
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = CreateInspectionAct(),
                Executors = [CreateExecutor("executor-1", 1, "Иванов", "Иван", "Иванович", "электромеханик")],
                TemplatePath = templatePath,
                OutputPath = outputPath
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        string documentText = ReadDocumentText(outputPath);
        Assert.Contains("о приемке выполненных работ", documentText, StringComparison.Ordinal);
        Assert.Contains("26.06.2026", documentText, StringComparison.Ordinal);
        Assert.Contains(
            "Купоросный цех (КЦ), АСУ использования конденсатов КЦ для выработки пара в котельной",
            documentText,
            StringComparison.Ordinal);
        Assert.DoesNotContain("SIMATIC S7-300, PS 307, БЛОК ПИТАНИЯ", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("Проверка после ремонта", documentText, StringComparison.Ordinal);
        Assert.Contains("Заявка 1", documentText, StringComparison.Ordinal);
        Assert.Contains("Работы выполнены, замечаний нет", documentText, StringComparison.Ordinal);
        Assert.Contains("2", documentText, StringComparison.Ordinal);
        Assert.Contains("Сидоров Сидор Сидорович", documentText, StringComparison.Ordinal);
        Assert.Contains("Иванов Иван Иванович", documentText, StringComparison.Ordinal);
        Assert.Contains("Павлов Павел Павлович", documentText, StringComparison.Ordinal);
        Assert.Contains("начальник цеха", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("ШКМ1", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("Rack", documentText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Slot", documentText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Малашкеевич", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("2025", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", documentText, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithLongExternalFailureFields_AddsUnnumberedParagraphs()
    {
        string repositoryRoot = FindRepositoryRoot();
        string templatePath = Path.Combine(
            repositoryRoot,
            "Templates",
            "Acts",
            KnowledgeBaseActDocxTemplateService.GetTemplateFileName(KbActType.EquipmentFailure));
        string outputPath = Path.Combine(CreateTempDirectory(), "external-template-long-failure-fields.docx");
        KbAct act = CreateAct();
        act.FaultDescription = string.Join(
            " ",
            Enumerable.Range(1, 45).Select(static index => $"fault-segment-{index:D2}"));
        act.FailureReason = string.Join(
            " ",
            Enumerable.Range(1, 45).Select(static index => $"reason-segment-{index:D2}"));
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = act,
                Executors =
                [
                    CreateExecutor(
                        "executor-1",
                        1,
                        "Executor",
                        "Person",
                        string.Empty,
                        "Executor Position")
                ],
                TemplatePath = templatePath,
                OutputPath = outputPath
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        using WordprocessingDocument document = WordprocessingDocument.Open(outputPath, false);
        List<Paragraph> faultParagraphs = document.MainDocumentPart!.Document
            .Descendants<Paragraph>()
            .Where(static paragraph => paragraph.InnerText.Contains("fault-segment-", StringComparison.Ordinal))
            .ToList();
        List<Paragraph> reasonParagraphs = document.MainDocumentPart.Document
            .Descendants<Paragraph>()
            .Where(static paragraph => paragraph.InnerText.Contains("reason-segment-", StringComparison.Ordinal))
            .ToList();

        Assert.True(faultParagraphs.Count > 1);
        Assert.True(reasonParagraphs.Count > 1);
        Assert.NotNull(faultParagraphs[0].ParagraphProperties?.NumberingProperties);
        Assert.NotNull(reasonParagraphs[0].ParagraphProperties?.NumberingProperties);
        Assert.All(faultParagraphs.Skip(1), static paragraph =>
            Assert.Null(paragraph.ParagraphProperties?.NumberingProperties));
        Assert.All(reasonParagraphs.Skip(1), static paragraph =>
            Assert.Null(paragraph.ParagraphProperties?.NumberingProperties));
        Assert.All(faultParagraphs, AssertFollowedByFieldLine);
        Assert.All(reasonParagraphs, AssertFollowedByFieldLine);
        string documentText = ReadDocumentText(outputPath);
        Assert.Contains("fault-segment-45", documentText, StringComparison.Ordinal);
        Assert.Contains("reason-segment-45", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", documentText, StringComparison.Ordinal);
    }

    private static void AssertFollowedByFieldLine(Paragraph paragraph)
    {
        Paragraph? lineParagraph = paragraph.NextSibling<Paragraph>();
        Assert.NotNull(lineParagraph);
        Assert.NotEmpty(lineParagraph.InnerText.Trim());
        Assert.All(lineParagraph.InnerText.Trim(), static character => Assert.Equal('_', character));
    }

    [Fact]
    public void Generate_WithExternalInspectionTemplate_PreservesLongInspectionResult()
    {
        string repositoryRoot = FindRepositoryRoot();
        string templatePath = Path.Combine(
            repositoryRoot,
            "Templates",
            "Acts",
            KnowledgeBaseActDocxTemplateService.GetTemplateFileName(KbActType.InspectionWork));
        string outputPath = Path.Combine(CreateTempDirectory(), "inspection-template-long-result.docx");
        KbAct act = CreateInspectionAct();
        act.InspectionResult = string.Join(
            " ",
            Enumerable.Range(1, 45).Select(static index => $"inspection-result-segment-{index:D2}"))
            + " tail-token-zeta";
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = act,
                Executors =
                [
                    CreateExecutor(
                        "executor-1",
                        1,
                        "Ivanov",
                        "Ivan",
                        "Ivanovich",
                        "lead-engineer-for-automation-systems executor-position-tail-token")
                ],
                TemplatePath = templatePath,
                OutputPath = outputPath
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        string documentText = ReadDocumentText(outputPath);
        string normalizedDocumentText = NormalizeWhitespace(documentText);
        Assert.Contains("inspection-result-segment-01", normalizedDocumentText, StringComparison.Ordinal);
        Assert.Contains("inspection-result-segment-45", normalizedDocumentText, StringComparison.Ordinal);
        Assert.Contains("tail-token-zeta", normalizedDocumentText, StringComparison.Ordinal);
        Assert.Contains("executor-position-tail-token", normalizedDocumentText, StringComparison.Ordinal);
        Assert.DoesNotContain("...", normalizedDocumentText, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", documentText, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithExternalInspectionTemplate_UsesCellWidthAndKeepsWordsWhole()
    {
        string repositoryRoot = FindRepositoryRoot();
        string templatePath = Path.Combine(
            repositoryRoot,
            "Templates",
            "Acts",
            KnowledgeBaseActDocxTemplateService.GetTemplateFileName(KbActType.InspectionWork));
        string outputPath = Path.Combine(CreateTempDirectory(), "inspection-template-width-wrapping.docx");
        string expectedFontName;
        string expectedFontSize;
        using (WordprocessingDocument template = WordprocessingDocument.Open(templatePath, false))
        {
            Run markerRun = template.MainDocumentPart!.Document
                .Descendants<Run>()
                .First(run => string.Concat(run.Descendants<Text>().Select(static text => text.Text))
                    .Contains("{{InspectionResultLine1}}", StringComparison.Ordinal));
            expectedFontName = markerRun.RunProperties?.RunFonts?.Ascii?.Value ?? string.Empty;
            expectedFontSize = markerRun.RunProperties?.FontSize?.Val?.Value ?? string.Empty;
        }

        Assert.False(string.IsNullOrWhiteSpace(expectedFontName));
        Assert.False(string.IsNullOrWhiteSpace(expectedFontSize));
        KbAct act = CreateInspectionAct();
        const string wholeWord = "производительность";
        act.InspectionResult = $"маркер {string.Join(" ", Enumerable.Repeat("и", 80))} {wholeWord} конец";
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = act,
                Executors = [CreateExecutor("executor-1", 1, "Иванов", "Иван", "Иванович", "Инженер")],
                TemplatePath = templatePath,
                OutputPath = outputPath
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        using WordprocessingDocument document = WordprocessingDocument.Open(outputPath, false);
        TableCell firstResultCell = document.MainDocumentPart!.Document
            .Descendants<TableCell>()
            .First(cell => GetCellText(cell).TrimStart().StartsWith("маркер", StringComparison.Ordinal));
        string firstLine = GetCellText(firstResultCell).Trim();
        Assert.True(firstLine.Length > 78, $"Первая строка содержит только {firstLine.Length} символов: {firstLine}");

        IReadOnlyList<string> cellTexts = document.MainDocumentPart.Document
            .Descendants<TableCell>()
            .Select(GetCellText)
            .ToList();
        Assert.Contains(cellTexts, text => text.Split(' ').Contains(wholeWord, StringComparer.Ordinal));
        Assert.DoesNotContain(cellTexts, text => text.Contains("производи-", StringComparison.Ordinal));

        Run resultRun = firstResultCell.Descendants<Run>()
            .First(run => string.Concat(run.Descendants<Text>().Select(static text => text.Text))
                .Contains("маркер", StringComparison.Ordinal));
        Assert.Equal(expectedFontName, resultRun.RunProperties?.RunFonts?.Ascii?.Value);
        Assert.Equal(expectedFontSize, resultRun.RunProperties?.FontSize?.Val?.Value);
    }

    [Fact]
    public void Generate_WithSplitPlaceholders_PreservesPlaceholderRunFormatting()
    {
        string repositoryRoot = FindRepositoryRoot();
        string templatePath = Path.Combine(
            repositoryRoot,
            "Templates",
            "Acts",
            KnowledgeBaseActDocxTemplateService.GetTemplateFileName(KbActType.InspectionWork));
        string outputPath = Path.Combine(CreateTempDirectory(), "inspection-template-placeholder-formatting.docx");
        (string ExecutorFont, string ExecutorSize) = ReadPlaceholderFormat(
            templatePath,
            "ExecutorNamePosition");
        (string CustomerFont, string CustomerSize) = ReadPlaceholderFormat(
            templatePath,
            "CustomerNamePosition");
        KbAct act = CreateInspectionAct();
        act.CustomerName = "Customer Person";
        act.CustomerPosition = "Customer Position";
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = act,
                Executors =
                [
                    CreateExecutor(
                        "executor-1",
                        1,
                        "Executor",
                        "Person",
                        string.Empty,
                        "Executor Position")
                ],
                TemplatePath = templatePath,
                OutputPath = outputPath
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        using WordprocessingDocument document = WordprocessingDocument.Open(outputPath, false);
        AssertRunFormat(document, "Executor Person, Executor Position", ExecutorFont, ExecutorSize);
        AssertRunFormat(document, "Customer Person, Customer Position", CustomerFont, CustomerSize);
    }

    [Fact]
    public void Generate_WithExternalInspectionTemplate_SplitsLongExecutorNamePositionAcrossTwoRows()
    {
        string repositoryRoot = FindRepositoryRoot();
        string templatePath = Path.Combine(
            repositoryRoot,
            "Templates",
            "Acts",
            KnowledgeBaseActDocxTemplateService.GetTemplateFileName(KbActType.InspectionWork));
        string outputPath = Path.Combine(CreateTempDirectory(), "inspection-template-executor-lines.docx");
        KbAct act = CreateInspectionAct();
        var generator = new KnowledgeBaseActDocxGeneratorPlugin();

        KnowledgeBaseActDocxGenerationResult result = generator.Generate(
            new KnowledgeBaseActDocxGenerationRequest
            {
                Act = act,
                Executors =
                [
                    CreateExecutor(
                        "executor-1",
                        1,
                        "Belikov",
                        "Alexey",
                        string.Empty,
                        "lead engineer automation unit area block section tail-token")
                ],
                TemplatePath = templatePath,
                OutputPath = outputPath
            });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        string documentText = ReadDocumentText(outputPath);
        Assert.Contains("Belikov Alexey, lead engineer automation unit", documentText, StringComparison.Ordinal);
        Assert.Contains("area block section tail-token", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Belikov Alexey, lead engineer automation unit area block section tail-token",
            documentText,
            StringComparison.Ordinal);
        Assert.DoesNotContain("{{", documentText, StringComparison.Ordinal);
    }


    private static void CreateTemplate(string path)
    {
        using WordprocessingDocument document = WordprocessingDocument.Create(
            path,
            WordprocessingDocumentType.Document);
        MainDocumentPart mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(
            new Body(
                new Paragraph(new Run(new Text("Акт {{ActNumber}} от {{ActDate}}"))),
                new Paragraph(new Run(new Text("Цех: {{WorkshopName}}"))),
                new Paragraph(new Run(new Text("Объект: {{ObjectName}}"))),
                new Paragraph(new Run(new Text("Карточка: {{Lvl3Name}}"))),
                new Paragraph(new Run(new Text("Оборудование: {{EquipmentName}}"))),
                new Paragraph(new Run(new Text("Заказной номер: {{OrderNumber}}"))),
                new Paragraph(new Run(new Text("Неисправность: {{FaultDescription}}"))),
                new Table(
                    new TableRow(
                        CreateCell("N"),
                        CreateCell("Исполнитель"),
                        CreateCell("Должность")),
                    new TableRow(
                        CreateCell("{{ExecutorIndex}}"),
                        CreateCell("{{ExecutorName}}"),
                        CreateCell("{{ExecutorPosition}}")))));
        mainPart.Document.Save();
    }

    private static void CreateFixedExecutorTemplate(string path)
    {
        using WordprocessingDocument document = WordprocessingDocument.Create(
            path,
            WordprocessingDocumentType.Document);
        MainDocumentPart mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(
            new Body(
                new Paragraph(new Run(new Text("Место установки: {{InstallationPlace}}"))),
                new Paragraph(new Run(new Text("Серийный номер: {{SerialNumber}}"))),
                new Paragraph(new Run(new Text("Контакт: {{ContactPerson}}"))),
                new Paragraph(new Run(new Text("1. {{Executor1Position}} {{Executor1Name}}"))),
                new Paragraph(new Run(new Text("2. {{Executor2Position}} {{Executor2Name}}"))),
                new Paragraph(new Run(new Text("3. {{Executor3Position}} {{Executor3Name}}")))));
        mainPart.Document.Save();
    }

    private static void CreateUnderlinedPlaceholderTemplate(string path)
    {
        using WordprocessingDocument document = WordprocessingDocument.Create(
            path,
            WordprocessingDocumentType.Document);
        MainDocumentPart mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(
            new Body(
                new Paragraph(
                    new Run(new Text("Number: ") { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(
                        new RunProperties(new Underline { Val = UnderlineValues.Single }),
                        new Text("{{ActNumber}}")))));
        mainPart.Document.Save();
    }

    private static TableCell CreateCell(string text) =>
        new(new Paragraph(new Run(new Text(text))));

    private static string GetCellText(TableCell cell) =>
        string.Concat(cell.Descendants<Text>().Select(static text => text.Text));

    private static string ReadDocumentText(string path)
    {
        using WordprocessingDocument document = WordprocessingDocument.Open(path, false);
        return string.Join(
            "\n",
            document.MainDocumentPart!.Document.Descendants<Text>().Select(static text => text.Text));
    }

    private static string NormalizeWhitespace(string value) =>
        string.Join(
            " ",
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"akb5-act-docx-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static (string FontName, string FontSize) ReadPlaceholderFormat(
        string templatePath,
        string placeholderText)
    {
        using WordprocessingDocument template = WordprocessingDocument.Open(templatePath, false);
        Run run = template.MainDocumentPart!.Document
            .Descendants<Run>()
            .First(candidate => string.Concat(candidate.Descendants<Text>().Select(static text => text.Text))
                .Contains(placeholderText, StringComparison.Ordinal));
        string fontName = run.RunProperties?.RunFonts?.Ascii?.Value ?? string.Empty;
        string fontSize = run.RunProperties?.FontSize?.Val?.Value ?? string.Empty;
        Assert.False(string.IsNullOrWhiteSpace(fontName));
        Assert.False(string.IsNullOrWhiteSpace(fontSize));
        return (fontName, fontSize);
    }

    private static void AssertRunFormat(
        WordprocessingDocument document,
        string expectedText,
        string expectedFontName,
        string expectedFontSize)
    {
        Run run = document.MainDocumentPart!.Document
            .Descendants<Run>()
            .First(candidate => string.Concat(candidate.Descendants<Text>().Select(static text => text.Text))
                .Contains(expectedText, StringComparison.Ordinal));
        Assert.Equal(expectedFontName, run.RunProperties?.RunFonts?.Ascii?.Value);
        Assert.Equal(expectedFontSize, run.RunProperties?.FontSize?.Val?.Value);
    }

    private static string FindRepositoryRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(directory, "Templates", "Acts", "equipment_failure_act.docx");
            if (File.Exists(candidate))
                return directory;

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new DirectoryNotFoundException("Не найдена папка Templates\\Acts с внешним шаблоном акта.");
    }

    private static KbAct CreateAct() =>
        new()
        {
            ActId = "act-1",
            ActYear = 2026,
            ActNumber = "2026-0001",
            ActType = KbActType.EquipmentFailure,
            Status = KbActStatus.Draft,
            ActDate = new DateTime(2026, 6, 26),
            WorkshopName = "Купоросный цех (КЦ)",
            ObjectNameSnapshot = "АСУ использования конденсатов КЦ для выработки пара в котельной",
            Lvl3NameSnapshot = "ШКМ1",
            EquipmentName = "SIMATIC S7-300, PS 307, БЛОК ПИТАНИЯ",
            EquipmentSnapshot = new KbActEquipmentSnapshot
            {
                Model = "PS 307",
                OrderNumber = "6ES7307-1BA00-0AA0",
                SerialNumber = "SN-0001"
            },
            FailureDate = new DateTime(2026, 6, 26),
            FaultDescription = "Не включается",
            FailureReason = "Отказ блока питания",
            InspectionResult = "Требуется замена",
            FaultCriterion = "Нет выходного напряжения",
            RequestDocument = "Заявка 1",
            ActualLaborHours = "2",
            CustomerName = "Сидоров Сидор Сидорович",
            CustomerPosition = "мастер",
            ApproverName = "Павлов Павел Павлович",
            ApproverPosition = "начальник цеха"
        };

    private static KbAct CreateInspectionAct()
    {
        KbAct act = CreateAct();
        act.ActType = KbActType.InspectionWork;
        act.ActNumber = "2026-0002";
        act.FaultDescription = "Проверка после ремонта";
        act.InspectionResult = "Работы выполнены, замечаний нет";
        act.FailureReason = string.Empty;
        act.FaultCriterion = string.Empty;
        return act;
    }

    private static KbActExecutor CreateExecutor(
        string executorId,
        int sortOrder,
        string lastName,
        string firstName,
        string middleName,
        string position) =>
        new()
        {
            ExecutorId = executorId,
            ActId = "act-1",
            SortOrder = sortOrder,
            LastName = lastName,
            FirstName = firstName,
            MiddleName = middleName,
            Position = position
        };
}
