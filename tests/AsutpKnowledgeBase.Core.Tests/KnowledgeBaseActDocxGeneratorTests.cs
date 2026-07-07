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
        Assert.Contains("выхода из строя электрооборудования", documentText, StringComparison.Ordinal);
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
        Assert.Contains("SIMATIC S7-300, PS 307, БЛОК ПИТАНИЯ", documentText, StringComparison.Ordinal);
        Assert.Contains("Проверка после ремонта", documentText, StringComparison.Ordinal);
        Assert.Contains("Заявка 1", documentText, StringComparison.Ordinal);
        Assert.Contains("Работы выполнены, замечаний нет", documentText, StringComparison.Ordinal);
        Assert.Contains("2", documentText, StringComparison.Ordinal);
        Assert.Contains("Сидоров Сидор Сидорович", documentText, StringComparison.Ordinal);
        Assert.Contains("Иванов Иван Иванович", documentText, StringComparison.Ordinal);
        Assert.Contains("Павлов Павел Павлович", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("ШКМ1", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("Rack", documentText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Slot", documentText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Малашкеевич", documentText, StringComparison.Ordinal);
        Assert.DoesNotContain("2025", documentText, StringComparison.Ordinal);
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

    private static string ReadDocumentText(string path)
    {
        using WordprocessingDocument document = WordprocessingDocument.Open(path, false);
        return string.Join(
            "\n",
            document.MainDocumentPart!.Document.Descendants<Text>().Select(static text => text.Text));
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"akb5-act-docx-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
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
