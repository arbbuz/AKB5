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
        private const int FixedExecutorSlotCount = 3;
        private const int ObjectLineMaxLength = 105;
        private const int InspectionResultBaseLineCount = 8;
        private const int InspectionResultLineMaxLength = 78;
        private const int CustomerNamePositionLineMaxLength = 95;
        private const int ExecutorNamePositionMaxLength = 49;
        private const int SignatureNameMaxLength = 48;

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

                    var replacements = new Dictionary<string, string>(
                        BuildScalarReplacements(request.Act),
                        StringComparer.Ordinal);
                    IReadOnlyList<KbActExecutor> executors = NormalizeExecutors(request.Executors);
                    int inspectionResultLineCount = AddAcceptedInspectionTemplateReplacements(
                        replacements,
                        request.Act,
                        executors);
                    EnsureAcceptedInspectionResultRows(mainPart.Document.Body, inspectionResultLineCount);
                    AddFixedExecutorReplacements(replacements, executors);
                    PopulateExecutorTable(mainPart.Document.Body, replacements, executors);

                    ReplacePlaceholders(mainPart.Document.Body, replacements);
                    foreach (HeaderPart headerPart in mainPart.HeaderParts)
                        ReplacePlaceholders(headerPart.Header, replacements);

                    foreach (FooterPart footerPart in mainPart.FooterParts)
                        ReplacePlaceholders(footerPart.Footer, replacements);

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
                ["{{ApprovalDate}}"] = FormatDate(act.ActDate),
                ["{{ActType}}"] = KnowledgeBaseActJournalService.FormatActType(act.ActType),
                ["{{Status}}"] = KnowledgeBaseActJournalService.FormatStatus(act.Status),
                ["{{WorkshopName}}"] = act.WorkshopName,
                ["{{ObjectName}}"] = act.ObjectNameSnapshot,
                ["{{Lvl3Name}}"] = act.Lvl3NameSnapshot,
                ["{{ObjectPath}}"] = act.ObjectPathSnapshot,
                ["{{InstallationPlace}}"] = FormatInstallationPlace(act),
                ["{{InspectionWorkDescription}}"] = FormatInspectionWorkDescription(act),
                ["{{EquipmentName}}"] = act.EquipmentName,
                ["{{EquipmentModel}}"] = snapshot?.Model ?? string.Empty,
                ["{{OrderNumber}}"] = snapshot?.OrderNumber ?? string.Empty,
                ["{{SerialNumber}}"] = snapshot?.SerialNumber ?? string.Empty,
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
                ["{{CreatedBy}}"] = act.CreatedBy,
                ["{{ContactPerson}}"] = string.Empty
            };
        }

        private static int AddAcceptedInspectionTemplateReplacements(
            IDictionary<string, string> replacements,
            KbAct act,
            IReadOnlyList<KbActExecutor> executors)
        {
            replacements["{{ObjectLine1}}"] = FitSingleLine(
                FormatInstallationPlace(act),
                ObjectLineMaxLength);
            replacements["{{ObjectLine2}}"] = FitSingleLine(
                FormatEquipmentAndRequestDescription(act),
                ObjectLineMaxLength);

            string[] inspectionResultLines = SplitIntoFixedLinesPreservingOverflow(
                act.InspectionResult,
                InspectionResultLineMaxLength,
                minimumLineCount: InspectionResultBaseLineCount);
            for (int i = 0; i < inspectionResultLines.Length; i++)
            {
                string slot = (i + 1).ToString(CultureInfo.InvariantCulture);
                replacements[$"{{{{InspectionResultLine{slot}}}}}"] = inspectionResultLines[i];
            }

            string[] customerLines = SplitIntoFixedLines(
                FormatNamePosition(act.CustomerName, act.CustomerPosition),
                CustomerNamePositionLineMaxLength,
                lineCount: 2);
            replacements["{{CustomerNamePosition}}"] = customerLines[0];
            replacements["{{CustomerNamePosition1}}"] = customerLines[0];
            replacements["{{CustomerNamePosition2}}"] = customerLines[1];
            replacements["{{CustomerSignatureName}}"] = FitSingleLine(act.CustomerName, SignatureNameMaxLength);

            KbActExecutor? executor = executors.FirstOrDefault();
            string executorName = executor == null ? string.Empty : FormatExecutorName(executor);
            string[] executorLines = SplitIntoTwoLinesPreservingRemainder(
                FormatNamePosition(executorName, executor?.Position ?? string.Empty),
                ExecutorNamePositionMaxLength);
            replacements["{{ExecutorNamePosition}}"] = executorLines[0];
            replacements["{{ExecutorNamePosition2}}"] = executorLines[1];
            replacements["{{ExecutorSignatureName}}"] = FitSingleLine(executorName, SignatureNameMaxLength);

            replacements["{{TransferredToLine}}"] = string.Empty;
            replacements["{{WorkStart}}"] = FormatDate(act.FailureDate ?? act.ActDate);
            replacements["{{WorkEnd}}"] = FormatDate(act.ActDate);
            return inspectionResultLines.Length;
        }

        private static string FormatInstallationPlace(KbAct act)
        {
            string workshopName = act.WorkshopName?.Trim() ?? string.Empty;
            string objectName = act.ObjectNameSnapshot?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(workshopName) &&
                !string.IsNullOrWhiteSpace(objectName))
            {
                return $"{workshopName}, {objectName}";
            }

            return !string.IsNullOrWhiteSpace(workshopName)
                ? workshopName
                : objectName;
        }

        private static string FormatInspectionWorkDescription(KbAct act)
        {
            string[] parts =
            [
                FormatInstallationPlace(act),
                act.EquipmentName?.Trim() ?? string.Empty,
                act.FaultDescription?.Trim() ?? string.Empty
            ];

            return string.Join(
                ". ",
                parts.Where(static part => !string.IsNullOrWhiteSpace(part)));
        }

        private static string FormatEquipmentAndRequestDescription(KbAct act)
        {
            string[] parts =
            [
                act.EquipmentName?.Trim() ?? string.Empty,
                act.FaultDescription?.Trim() ?? string.Empty
            ];

            return string.Join(
                ". ",
                parts.Where(static part => !string.IsNullOrWhiteSpace(part)));
        }

        private static string FormatNamePosition(string name, string position)
        {
            string normalizedName = NormalizeInlineText(name);
            string normalizedPosition = NormalizeInlineText(position);
            if (string.IsNullOrWhiteSpace(normalizedName))
                return normalizedPosition;

            if (string.IsNullOrWhiteSpace(normalizedPosition))
                return normalizedName;

            return $"{normalizedName}, {normalizedPosition}";
        }

        private static string[] SplitIntoFixedLines(string value, int maxLength, int lineCount)
        {
            var lines = new List<string>(lineCount);
            string remaining = NormalizeInlineText(value);
            while (lines.Count < lineCount && !string.IsNullOrWhiteSpace(remaining))
            {
                bool isLastLine = lines.Count == lineCount - 1;
                if (remaining.Length <= maxLength)
                {
                    lines.Add(remaining);
                    remaining = string.Empty;
                    break;
                }

                if (isLastLine)
                {
                    lines.Add(FitSingleLine(remaining, maxLength));
                    remaining = string.Empty;
                    break;
                }

                int splitIndex = FindSplitIndex(remaining, maxLength);
                lines.Add(remaining[..splitIndex].Trim());
                remaining = remaining[splitIndex..].Trim();
            }

            while (lines.Count < lineCount)
                lines.Add(string.Empty);

            return lines.ToArray();
        }

        private static string[] SplitIntoTwoLinesPreservingRemainder(string value, int maxLength)
        {
            string remaining = NormalizeInlineText(value);
            if (remaining.Length <= maxLength)
                return [remaining, string.Empty];

            int splitIndex = FindSplitIndex(remaining, maxLength);
            return
            [
                remaining[..splitIndex].Trim(),
                remaining[splitIndex..].Trim()
            ];
        }

        private static string[] SplitIntoFixedLinesPreservingOverflow(
            string value,
            int maxLength,
            int minimumLineCount)
        {
            var lines = new List<string>(minimumLineCount);
            string remaining = NormalizeInlineText(value);
            while (!string.IsNullOrWhiteSpace(remaining))
            {
                if (remaining.Length <= maxLength)
                {
                    lines.Add(remaining);
                    remaining = string.Empty;
                    break;
                }

                int splitIndex = FindSplitIndex(remaining, maxLength);
                lines.Add(remaining[..splitIndex].Trim());
                remaining = remaining[splitIndex..].Trim();
            }

            while (lines.Count < minimumLineCount)
                lines.Add(string.Empty);

            return lines.ToArray();
        }

        private static int FindSplitIndex(string value, int maxLength)
        {
            int upperBound = Math.Min(maxLength, value.Length - 1);
            int splitIndex = value.LastIndexOf(' ', upperBound);
            return splitIndex > 0 ? splitIndex : Math.Min(maxLength, value.Length);
        }

        private static string FitSingleLine(string value, int maxLength)
        {
            string normalized = NormalizeInlineText(value);
            if (normalized.Length <= maxLength)
                return normalized;

            return maxLength <= 3
                ? normalized[..maxLength]
                : string.Concat(normalized.AsSpan(0, maxLength - 3), "...");
        }

        private static string NormalizeInlineText(string? value) =>
            string.Join(
                " ",
                (value ?? string.Empty)
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        private static IReadOnlyList<KbActExecutor> NormalizeExecutors(IReadOnlyList<KbActExecutor>? executors) =>
            KnowledgeBaseDataService.NormalizeActExecutors(executors)
                .OrderBy(static executor => executor.SortOrder)
                .ThenBy(static executor => executor.ExecutorId, StringComparer.Ordinal)
                .ToList();

        private static void AddFixedExecutorReplacements(
            IDictionary<string, string> replacements,
            IReadOnlyList<KbActExecutor> executors)
        {
            for (int i = 0; i < FixedExecutorSlotCount; i++)
            {
                KbActExecutor? executor = i < executors.Count ? executors[i] : null;
                string slot = (i + 1).ToString(CultureInfo.InvariantCulture);
                replacements[$"{{{{Executor{slot}Name}}}}"] = executor == null
                    ? string.Empty
                    : FormatExecutorName(executor);
                replacements[$"{{{{Executor{slot}Position}}}}"] = executor?.Position ?? string.Empty;
            }
        }

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

        private static void EnsureAcceptedInspectionResultRows(Body body, int lineCount)
        {
            if (lineCount <= InspectionResultBaseLineCount)
                return;

            TableRow? markerRow = body
                .Descendants<TableRow>()
                .FirstOrDefault(static row =>
                    row.InnerText.Contains(
                        $"{{{{InspectionResultLine{InspectionResultBaseLineCount}}}}}",
                        StringComparison.Ordinal));
            if (markerRow?.Parent == null)
                return;

            OpenXmlElement parent = markerRow.Parent;
            OpenXmlElement insertAfter = markerRow;
            for (int line = InspectionResultBaseLineCount + 1; line <= lineCount; line++)
            {
                var row = (TableRow)markerRow.CloneNode(deep: true);
                ReplacePlaceholders(
                    row,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        [$"{{{{InspectionResultLine{InspectionResultBaseLineCount}}}}}"] =
                            $"{{{{InspectionResultLine{line}}}}}"
                    });
                parent.InsertAfter(row, insertAfter);
                insertAfter = row;
            }
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
                ReplacePlaceholdersInParagraph(paragraph, replacements);

            foreach (Text text in root.Descendants<Text>()
                         .Where(static text => !text.Ancestors<Paragraph>().Any()))
                text.Text = ReplacePlaceholdersInValue(text.Text, replacements);
        }

        private static void ReplacePlaceholdersInParagraph(
            Paragraph paragraph,
            IReadOnlyDictionary<string, string> replacements)
        {
            List<Text> textNodes = paragraph.Descendants<Text>().ToList();
            if (textNodes.Count == 0)
                return;

            foreach (Text text in textNodes)
                text.Text = ReplacePlaceholdersInValue(text.Text, replacements);

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
