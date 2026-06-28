using System.Globalization;
using System.Security.Cryptography;
using AsutpKnowledgeBase.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseActDocxGeneratorPlugin : IKnowledgeBaseActDocxGenerator
    {
        private static readonly string[] ExecutorPlaceholders =
        [
            "{{ExecutorIndex}}",
            "{{ExecutorName}}",
            "{{ExecutorPosition}}"
        ];

        public KnowledgeBaseActDocxGenerationResult Generate(KnowledgeBaseActDocxGenerationRequest request)
        {
            if (request.Act == null)
                return Failure("Не переданы данные акта.");

            if (string.IsNullOrWhiteSpace(request.TemplatePath))
                return Failure("Не указан путь к шаблону DOCX.");

            if (!File.Exists(request.TemplatePath))
                return Failure($"Шаблон DOCX не найден: {request.TemplatePath}");

            if (string.IsNullOrWhiteSpace(request.OutputPath))
                return Failure("Не указан путь сохранения DOCX.");

            string outputPath = Path.GetFullPath(request.OutputPath);
            if (File.Exists(outputPath) && !request.OverwriteExisting)
                return Failure($"Файл DOCX уже существует: {outputPath}");

            string? outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            string tempOutputPath = BuildTempOutputPath(outputPath);
            try
            {
                File.Copy(request.TemplatePath, tempOutputPath, overwrite: false);
                using (WordprocessingDocument document = WordprocessingDocument.Open(tempOutputPath, true))
                {
                    MainDocumentPart? mainPart = document.MainDocumentPart;
                    if (mainPart?.Document?.Body == null)
                        return DeleteOutputAndFail(tempOutputPath, "Шаблон DOCX не содержит основной части документа.");

                    IReadOnlyDictionary<string, string> scalarReplacements = BuildScalarReplacements(request.Act);
                    IReadOnlyList<KbActExecutor> executors = NormalizeExecutors(request.Executors);
                    if (!PopulateExecutorTable(mainPart.Document.Body, scalarReplacements, executors))
                    {
                        return DeleteOutputAndFail(
                            outputPath,
                            "В шаблоне DOCX не найдена строка таблицы исполнителей с {{ExecutorName}}.");
                    }

                    ReplacePlaceholders(mainPart.Document.Body, scalarReplacements);
                    foreach (HeaderPart headerPart in mainPart.HeaderParts)
                        ReplacePlaceholders(headerPart.Header, scalarReplacements);

                    foreach (FooterPart footerPart in mainPart.FooterParts)
                        ReplacePlaceholders(footerPart.Footer, scalarReplacements);

                    mainPart.Document.Save();
                }

                CommitGeneratedFile(tempOutputPath, outputPath, request.OverwriteExisting);
                tempOutputPath = string.Empty;
                return new KnowledgeBaseActDocxGenerationResult
                {
                    IsSuccess = true,
                    OutputPath = outputPath,
                    ContentHash = ComputeSha256(outputPath)
                };
            }
            catch (Exception ex)
            {
                TryDeleteOutput(tempOutputPath);
                return Failure($"Не удалось сформировать DOCX: {ex.GetBaseException().Message}");
            }
        }

        private static IReadOnlyDictionary<string, string> BuildScalarReplacements(KbAct act)
        {
            KbActEquipmentSnapshot? snapshot = act.EquipmentSnapshot;
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["{{ActNumber}}"] = act.ActNumber,
                ["{{ActYear}}"] = act.ActYear > 0
                    ? act.ActYear.ToString(CultureInfo.InvariantCulture)
                    : string.Empty,
                ["{{ActDate}}"] = FormatDate(act.ActDate),
                ["{{ActType}}"] = KnowledgeBaseActJournalService.FormatActType(act.ActType),
                ["{{Status}}"] = KnowledgeBaseActJournalService.FormatStatus(act.Status),
                ["{{WorkshopName}}"] = act.WorkshopName,
                ["{{ObjectName}}"] = act.ObjectNameSnapshot,
                ["{{Lvl3Name}}"] = act.Lvl3NameSnapshot,
                ["{{ObjectPath}}"] = act.ObjectPathSnapshot,
                ["{{EquipmentName}}"] = act.EquipmentName,
                ["{{EquipmentModel}}"] = snapshot?.Model ?? string.Empty,
                ["{{OrderNumber}}"] = snapshot?.OrderNumber ?? string.Empty,
                ["{{FailureDate}}"] = FormatDate(act.FailureDate),
                ["{{FaultDescription}}"] = act.FaultDescription,
                ["{{FailureReason}}"] = act.FailureReason,
                ["{{InspectionResult}}"] = act.InspectionResult,
                ["{{FaultCriterion}}"] = act.FaultCriterion,
                ["{{RequestDocument}}"] = act.RequestDocument,
                ["{{ActualLaborHours}}"] = act.ActualLaborHours,
                ["{{CustomerName}}"] = act.CustomerName,
                ["{{CustomerPosition}}"] = act.CustomerPosition,
                ["{{ApproverName}}"] = act.ApproverName,
                ["{{ApproverPosition}}"] = act.ApproverPosition,
                ["{{CreatedBy}}"] = act.CreatedBy
            };
        }

        private static IReadOnlyList<KbActExecutor> NormalizeExecutors(IReadOnlyList<KbActExecutor>? executors) =>
            KnowledgeBaseDataService.NormalizeActExecutors(executors)
                .OrderBy(static executor => executor.SortOrder)
                .ThenBy(static executor => executor.ExecutorId, StringComparer.Ordinal)
                .ToList();

        private static bool PopulateExecutorTable(
            Body body,
            IReadOnlyDictionary<string, string> scalarReplacements,
            IReadOnlyList<KbActExecutor> executors)
        {
            TableRow? markerRow = body
                .Descendants<TableRow>()
                .FirstOrDefault(ContainsExecutorPlaceholder);
            if (markerRow == null)
                return false;

            if (executors.Count == 0)
            {
                markerRow.Remove();
                return true;
            }

            int index = 1;
            foreach (KbActExecutor executor in executors)
            {
                var row = (TableRow)markerRow.CloneNode(deep: true);
                var replacements = new Dictionary<string, string>(scalarReplacements, StringComparer.Ordinal)
                {
                    ["{{ExecutorIndex}}"] = index.ToString(CultureInfo.InvariantCulture),
                    ["{{ExecutorName}}"] = FormatExecutorName(executor),
                    ["{{ExecutorPosition}}"] = executor.Position
                };

                ReplacePlaceholders(row, replacements);
                markerRow.InsertBeforeSelf(row);
                index++;
            }

            markerRow.Remove();
            return true;
        }

        private static bool ContainsExecutorPlaceholder(OpenXmlElement element)
        {
            string text = element.InnerText;
            return ExecutorPlaceholders.Any(placeholder =>
                text.Contains(placeholder, StringComparison.Ordinal));
        }

        private static void ReplacePlaceholders(
            OpenXmlElement? root,
            IReadOnlyDictionary<string, string> replacements)
        {
            if (root == null || replacements.Count == 0)
                return;

            foreach (Paragraph paragraph in root.Descendants<Paragraph>())
                ReplacePlaceholdersInScope(paragraph, replacements);

            foreach (Text text in root.Descendants<Text>())
                text.Text = ReplacePlaceholdersInValue(text.Text, replacements);
        }

        private static void ReplacePlaceholdersInScope(
            OpenXmlElement scope,
            IReadOnlyDictionary<string, string> replacements)
        {
            List<Text> textNodes = scope.Descendants<Text>().ToList();
            if (textNodes.Count <= 1)
                return;

            string combined = string.Concat(textNodes.Select(static text => text.Text));
            string replaced = ReplacePlaceholdersInValue(combined, replacements);
            if (string.Equals(combined, replaced, StringComparison.Ordinal))
                return;

            textNodes[0].Text = replaced;
            for (int i = 1; i < textNodes.Count; i++)
                textNodes[i].Text = string.Empty;
        }

        private static string ReplacePlaceholdersInValue(
            string value,
            IReadOnlyDictionary<string, string> replacements)
        {
            string result = value;
            foreach (var pair in replacements)
                result = result.Replace(pair.Key, pair.Value ?? string.Empty, StringComparison.Ordinal);

            return result;
        }

        private static string FormatDate(DateTime? value) =>
            value?.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture) ?? string.Empty;

        private static string FormatExecutorName(KbActExecutor executor) =>
            string.Join(
                    " ",
                    new[]
                    {
                        executor.LastName,
                        executor.FirstName,
                        executor.MiddleName
                    }
                    .Where(static part => !string.IsNullOrWhiteSpace(part))
                    .Select(static part => part.Trim()))
                .Trim();

        private static string ComputeSha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using var sha256 = SHA256.Create();
            return Convert.ToHexString(sha256.ComputeHash(stream));
        }

        private static string BuildTempOutputPath(string outputPath)
        {
            string directory = Path.GetDirectoryName(outputPath) ?? AppContext.BaseDirectory;
            string fileName = Path.GetFileNameWithoutExtension(outputPath);
            return Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.tmp.docx");
        }

        private static void CommitGeneratedFile(string tempOutputPath, string outputPath, bool overwriteExisting)
        {
            if (File.Exists(outputPath))
            {
                if (!overwriteExisting)
                    throw new IOException($"Файл DOCX уже существует: {outputPath}");

                File.Replace(tempOutputPath, outputPath, null, ignoreMetadataErrors: true);
                return;
            }

            File.Move(tempOutputPath, outputPath);
        }

        private static KnowledgeBaseActDocxGenerationResult DeleteOutputAndFail(string outputPath, string errorMessage)
        {
            TryDeleteOutput(outputPath);
            return Failure(errorMessage);
        }

        private static void TryDeleteOutput(string outputPath)
        {
            try
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
            catch
            {
                // Best effort cleanup; the original generation error is more important.
            }
        }

        private static KnowledgeBaseActDocxGenerationResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
