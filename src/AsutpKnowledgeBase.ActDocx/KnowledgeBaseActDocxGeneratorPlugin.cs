using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
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
        private const float TextWidthSafetyMarginPoints = 8F;
        private const string WordprocessingNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
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
                    AddExpandableFailureTextFieldReplacements(
                        replacements,
                        request.Act,
                        mainPart);
                    int inspectionResultLineCount = AddAcceptedInspectionTemplateReplacements(
                        replacements,
                        request.Act,
                        executors,
                        mainPart.Document.Body);
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
            IReadOnlyList<KbActExecutor> executors,
            Body body)
        {
            replacements["{{ObjectLine1}}"] = FitSingleLine(
                FormatInstallationPlace(act),
                ObjectLineMaxLength);
            replacements["{{ObjectLine2}}"] = string.Empty;

            string[] inspectionResultLines = act.ActType == KbActType.InspectionWork
                ? SplitIntoWidthFittedLines(
                    act.InspectionResult,
                    ResolveInspectionResultLayout(body),
                    minimumLineCount: InspectionResultBaseLineCount)
                : Enumerable.Repeat(string.Empty, InspectionResultBaseLineCount).ToArray();
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

        private static void AddExpandableFailureTextFieldReplacements(
            IDictionary<string, string> replacements,
            KbAct act,
            MainDocumentPart mainPart)
        {
            if (act.ActType != KbActType.EquipmentFailure)
                return;

            AddExpandableParagraphFieldReplacements(
                replacements,
                mainPart,
                "{{FaultDescription}}",
                act.FaultDescription);
            AddExpandableParagraphFieldReplacements(
                replacements,
                mainPart,
                "{{FailureReason}}",
                act.FailureReason);
        }

        private static void AddExpandableParagraphFieldReplacements(
            IDictionary<string, string> replacements,
            MainDocumentPart mainPart,
            string placeholder,
            string value)
        {
            ExpandableParagraphField? field = ResolveExpandableParagraphField(mainPart, placeholder);
            if (field == null)
                return;

            string[] lines = SplitIntoWidthFittedLines(value, field.Layout, minimumLineCount: 1);
            replacements[placeholder] = lines[0];
            OpenXmlElement insertAfter = field.LineParagraph;
            for (int i = 1; i < lines.Length; i++)
            {
                string linePlaceholder = BuildLinePlaceholder(placeholder, i + 1);
                var paragraph = (Paragraph)field.MarkerParagraph.CloneNode(deep: true);
                ParagraphProperties properties = paragraph.ParagraphProperties ??
                    paragraph.PrependChild(new ParagraphProperties());
                properties.NumberingProperties = null;
                if (field.Indentation == null)
                {
                    properties.Indentation = null;
                }
                else
                {
                    var indentation = (Indentation)field.Indentation.CloneNode(deep: true);
                    indentation.Hanging = null;
                    indentation.FirstLine = null;
                    properties.Indentation = indentation;
                }

                ReplacePlaceholdersInParagraph(
                    paragraph,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        [placeholder] = linePlaceholder
                    });
                insertAfter.InsertAfterSelf(paragraph);
                insertAfter = paragraph;

                var lineParagraph = (Paragraph)field.LineParagraph.CloneNode(deep: true);
                insertAfter.InsertAfterSelf(lineParagraph);
                insertAfter = lineParagraph;
                replacements[linePlaceholder] = lines[i];
            }
        }

        private static string BuildLinePlaceholder(string placeholder, int lineNumber) =>
            placeholder[..^2] + "Line" + lineNumber.ToString(CultureInfo.InvariantCulture) + "}}";

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

        private static string[] SplitIntoWidthFittedLines(
            string value,
            TextFieldLayout layout,
            int minimumLineCount)
        {
            var lines = new List<string>(minimumLineCount);
            string normalized = NormalizeInlineText(value);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                using var measurer = new GdiTextWidthMeasurer(
                    layout.FontName,
                    layout.FontSizePoints);

                string currentLine = string.Empty;
                foreach (string word in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    string candidate = string.IsNullOrEmpty(currentLine)
                        ? word
                        : $"{currentLine} {word}";
                    float candidateWidth = measurer.MeasureWidthPoints(candidate);
                    if (string.IsNullOrEmpty(currentLine) || candidateWidth <= layout.UsableWidthPoints)
                    {
                        currentLine = candidate;
                        continue;
                    }

                    lines.Add(currentLine);
                    currentLine = word;
                }

                if (!string.IsNullOrEmpty(currentLine))
                    lines.Add(currentLine);
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

        private static ExpandableParagraphField? ResolveExpandableParagraphField(
            MainDocumentPart mainPart,
            string placeholder)
        {
            Body? body = mainPart.Document.Body;
            Paragraph? markerParagraph = body?
                .Descendants<Paragraph>()
                .FirstOrDefault(paragraph => string.Equals(
                    string.Concat(paragraph.Descendants<Text>().Select(static text => text.Text)).Trim(),
                    placeholder,
                    StringComparison.Ordinal));
            if (body == null || markerParagraph == null)
                return null;

            Text markerText = ResolvePlaceholderFormatText(markerParagraph, placeholder);

            Paragraph? lineParagraph = markerParagraph.NextSibling<Paragraph>();
            if (lineParagraph == null ||
                lineParagraph.InnerText.Trim().Any(static character => character != '_'))
            {
                return null;
            }

            SectionProperties? section = body.Elements<SectionProperties>().LastOrDefault();
            OpenXmlElement? pageSize = FindChild(section, "pgSz");
            OpenXmlElement? pageMargin = FindChild(section, "pgMar");
            int pageWidthTwips = GetIntegerAttribute(pageSize, "w");
            int leftMarginTwips = GetIntegerAttribute(pageMargin, "left");
            int rightMarginTwips = GetIntegerAttribute(pageMargin, "right");
            if (pageWidthTwips <= 0)
                throw new InvalidDataException($"Для поля {placeholder} не задана ширина страницы.");

            Indentation? indentation = ResolveParagraphIndentation(mainPart, markerParagraph);
            int leftIndentTwips = GetFirstIntegerAttribute(indentation, "left", "start");
            int rightIndentTwips = GetFirstIntegerAttribute(indentation, "right", "end");
            int usableWidthTwips = pageWidthTwips -
                leftMarginTwips -
                rightMarginTwips -
                leftIndentTwips -
                rightIndentTwips;
            float usableWidthPoints = usableWidthTwips / 20F - TextWidthSafetyMarginPoints;
            if (usableWidthPoints <= 0F)
                throw new InvalidDataException($"Полезная ширина поля {placeholder} должна быть положительной.");

            TextFormat textFormat = ResolvePlaceholderTextFormat(
                markerText,
                markerParagraph,
                placeholder);
            return new ExpandableParagraphField(
                markerParagraph,
                lineParagraph,
                indentation,
                new TextFieldLayout(
                    usableWidthPoints,
                    textFormat.FontName,
                    textFormat.FontSizePoints));
        }

        private static Text ResolvePlaceholderFormatText(
            Paragraph paragraph,
            string placeholder)
        {
            List<Text> textNodes = paragraph.Descendants<Text>().ToList();
            string combined = string.Concat(textNodes.Select(static text => text.Text));
            int matchStart = combined.IndexOf(placeholder, StringComparison.Ordinal);
            if (matchStart < 0)
                throw new InvalidDataException($"Не найден плейсхолдер {placeholder}.");

            int[] nodeStarts = new int[textNodes.Count];
            int currentStart = 0;
            for (int i = 0; i < textNodes.Count; i++)
            {
                nodeStarts[i] = currentStart;
                currentStart += textNodes[i].Text.Length;
            }

            int matchEnd = matchStart + placeholder.Length;
            int startNodeIndex = FindTextNodeIndex(textNodes, nodeStarts, matchStart);
            int endNodeIndex = FindTextNodeIndex(textNodes, nodeStarts, matchEnd - 1);
            int formatNodeIndex = FindPlaceholderFormatNodeIndex(
                textNodes,
                startNodeIndex,
                endNodeIndex,
                matchStart - nodeStarts[startNodeIndex],
                matchEnd - nodeStarts[endNodeIndex]);
            return textNodes[formatNodeIndex];
        }

        private static Indentation? ResolveParagraphIndentation(
            MainDocumentPart mainPart,
            Paragraph paragraph)
        {
            Indentation? directIndentation = paragraph.ParagraphProperties?.Indentation;
            if (directIndentation != null)
                return directIndentation;

            OpenXmlElement? numberingProperties = FindChild(paragraph.ParagraphProperties, "numPr");
            int numberingId = GetIntegerAttribute(FindChild(numberingProperties, "numId"), "val");
            int levelIndex = GetIntegerAttribute(FindChild(numberingProperties, "ilvl"), "val");
            OpenXmlElement? numbering = mainPart.NumberingDefinitionsPart?.Numbering;
            OpenXmlElement? numberingInstance = numbering?.ChildElements.FirstOrDefault(element =>
                string.Equals(element.LocalName, "num", StringComparison.Ordinal) &&
                GetIntegerAttribute(element, "numId") == numberingId);
            int abstractNumberingId = GetIntegerAttribute(
                FindChild(numberingInstance, "abstractNumId"),
                "val");
            OpenXmlElement? abstractNumbering = numbering?.ChildElements.FirstOrDefault(element =>
                string.Equals(element.LocalName, "abstractNum", StringComparison.Ordinal) &&
                GetIntegerAttribute(element, "abstractNumId") == abstractNumberingId);
            OpenXmlElement? level = abstractNumbering?.ChildElements.FirstOrDefault(element =>
                string.Equals(element.LocalName, "lvl", StringComparison.Ordinal) &&
                GetIntegerAttribute(element, "ilvl") == levelIndex);
            return FindChild(FindChild(level, "pPr"), "ind") as Indentation;
        }

        private static TextFieldLayout ResolveInspectionResultLayout(Body body)
        {
            Text? markerText = body
                .Descendants<Text>()
                .FirstOrDefault(static text =>
                    text.Text.Contains("{{InspectionResultLine1}}", StringComparison.Ordinal));
            TableCell? cell = markerText?.Ancestors<TableCell>().FirstOrDefault();
            Paragraph? paragraph = markerText?.Ancestors<Paragraph>().FirstOrDefault();
            if (cell == null || paragraph == null)
                throw new InvalidDataException("В шаблоне не найдено поле {{InspectionResultLine1}}.");

            TextFormat textFormat = ResolvePlaceholderTextFormat(
                markerText,
                paragraph,
                "{{InspectionResultLine1}}");

            OpenXmlElement? cellWidth = FindChild(cell.TableCellProperties, "tcW");
            string widthType = GetAttributeValue(cellWidth, "type");
            int cellWidthTwips = GetIntegerAttribute(cellWidth, "w");
            if (cellWidthTwips <= 0 ||
                !string.IsNullOrEmpty(widthType) && !string.Equals(widthType, "dxa", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Ширина поля {{InspectionResultLine1}} должна быть задана в twips.");
            }

            OpenXmlElement? margins = FindChild(cell.TableCellProperties, "tcMar");
            if (margins == null)
            {
                Table? table = cell.Ancestors<Table>().FirstOrDefault();
                margins = FindChild(FindChild(table, "tblPr"), "tblCellMar");
            }

            int leftMarginTwips = GetSideWidthTwips(margins, "left", "start");
            int rightMarginTwips = GetSideWidthTwips(margins, "right", "end");
            OpenXmlElement? indentation = FindChild(paragraph.ParagraphProperties, "ind");
            int leftIndentTwips = GetFirstIntegerAttribute(indentation, "left", "start");
            int rightIndentTwips = GetFirstIntegerAttribute(indentation, "right", "end");
            int usableWidthTwips = cellWidthTwips -
                leftMarginTwips -
                rightMarginTwips -
                leftIndentTwips -
                rightIndentTwips;
            if (usableWidthTwips <= 0)
                throw new InvalidDataException("Полезная ширина поля {{InspectionResultLine1}} должна быть положительной.");

            float usableWidthPoints = usableWidthTwips / 20F - TextWidthSafetyMarginPoints;
            string fixedParagraphText = paragraph.InnerText.Replace(
                "{{InspectionResultLine1}}",
                string.Empty,
                StringComparison.Ordinal);
            if (!string.IsNullOrEmpty(fixedParagraphText))
            {
                using var measurer = new GdiTextWidthMeasurer(
                    textFormat.FontName,
                    textFormat.FontSizePoints);
                usableWidthPoints -= measurer.MeasureWidthPoints(fixedParagraphText);
            }

            if (usableWidthPoints <= 0F)
                throw new InvalidDataException("Полезная ширина текста {{InspectionResultLine1}} должна быть положительной.");

            return new TextFieldLayout(
                usableWidthPoints,
                textFormat.FontName,
                textFormat.FontSizePoints);
        }

        private static TextFormat ResolvePlaceholderTextFormat(
            Text markerText,
            Paragraph paragraph,
            string placeholder)
        {
            OpenXmlElement? runProperties = markerText.Ancestors<Run>().FirstOrDefault()?.RunProperties;
            OpenXmlElement? paragraphRunProperties = FindChild(paragraph.ParagraphProperties, "rPr");
            string fontName = GetRunFontName(runProperties);
            if (string.IsNullOrWhiteSpace(fontName))
                fontName = GetRunFontName(paragraphRunProperties);

            string fontSizeHalfPoints = GetRunFontSize(runProperties);
            if (string.IsNullOrWhiteSpace(fontSizeHalfPoints))
                fontSizeHalfPoints = GetRunFontSize(paragraphRunProperties);

            if (string.IsNullOrWhiteSpace(fontName) ||
                !float.TryParse(
                    fontSizeHalfPoints,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float parsedHalfPoints) ||
                parsedHalfPoints <= 0F)
            {
                throw new InvalidDataException(
                    $"Для поля {placeholder} должны быть явно заданы шрифт и размер.");
            }

            return new TextFormat(fontName, parsedHalfPoints / 2F);
        }

        private static string GetRunFontName(OpenXmlElement? runProperties)
        {
            OpenXmlElement? runFonts = FindChild(runProperties, "rFonts");
            return GetAttributeValue(runFonts, "ascii") is string ascii && !string.IsNullOrWhiteSpace(ascii)
                ? ascii
                : GetAttributeValue(runFonts, "hAnsi");
        }

        private static string GetRunFontSize(OpenXmlElement? runProperties) =>
            GetAttributeValue(FindChild(runProperties, "sz"), "val");

        private static int GetSideWidthTwips(
            OpenXmlElement? margins,
            string primarySide,
            string alternateSide)
        {
            OpenXmlElement? side = FindChild(margins, primarySide) ?? FindChild(margins, alternateSide);
            string widthType = GetAttributeValue(side, "type");
            if (!string.IsNullOrEmpty(widthType) && !string.Equals(widthType, "dxa", StringComparison.Ordinal))
                return 0;

            return GetIntegerAttribute(side, "w");
        }

        private static OpenXmlElement? FindChild(OpenXmlElement? parent, string localName) =>
            parent?.ChildElements.FirstOrDefault(child =>
                string.Equals(child.LocalName, localName, StringComparison.Ordinal));

        private static int GetFirstIntegerAttribute(OpenXmlElement? element, params string[] localNames)
        {
            foreach (string localName in localNames)
            {
                int value = GetIntegerAttribute(element, localName);
                if (value != 0)
                    return value;
            }

            return 0;
        }

        private static int GetIntegerAttribute(OpenXmlElement? element, string localName)
        {
            string value = GetAttributeValue(element, localName);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
                ? result
                : 0;
        }

        private static string GetAttributeValue(OpenXmlElement? element, string localName) =>
            element?.GetAttributes()
                .FirstOrDefault(attribute =>
                    string.Equals(attribute.LocalName, localName, StringComparison.Ordinal) &&
                    string.Equals(attribute.NamespaceUri, WordprocessingNamespace, StringComparison.Ordinal))
                .Value ?? string.Empty;

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

            foreach (var pair in replacements)
                ReplacePlaceholderInTextNodes(textNodes, pair.Key, pair.Value ?? string.Empty);
        }

        private static void ReplacePlaceholderInTextNodes(
            IReadOnlyList<Text> textNodes,
            string placeholder,
            string replacement)
        {
            while (true)
            {
                string combined = string.Concat(textNodes.Select(static text => text.Text));
                int matchStart = combined.IndexOf(placeholder, StringComparison.Ordinal);
                if (matchStart < 0)
                    return;

                int[] nodeStarts = new int[textNodes.Count];
                int currentStart = 0;
                for (int i = 0; i < textNodes.Count; i++)
                {
                    nodeStarts[i] = currentStart;
                    currentStart += textNodes[i].Text.Length;
                }

                int matchEnd = matchStart + placeholder.Length;
                int startNodeIndex = FindTextNodeIndex(textNodes, nodeStarts, matchStart);
                int endNodeIndex = FindTextNodeIndex(textNodes, nodeStarts, matchEnd - 1);
                int startOffset = matchStart - nodeStarts[startNodeIndex];
                int endOffset = matchEnd - nodeStarts[endNodeIndex];
                int formatNodeIndex = FindPlaceholderFormatNodeIndex(
                    textNodes,
                    startNodeIndex,
                    endNodeIndex,
                    startOffset,
                    endOffset);

                for (int i = startNodeIndex; i <= endNodeIndex; i++)
                {
                    string original = textNodes[i].Text;
                    string prefix = i == startNodeIndex ? original[..startOffset] : string.Empty;
                    string suffix = i == endNodeIndex ? original[endOffset..] : string.Empty;
                    textNodes[i].Text = i == formatNodeIndex
                        ? prefix + replacement + suffix
                        : prefix + suffix;
                }
            }
        }

        private static int FindTextNodeIndex(
            IReadOnlyList<Text> textNodes,
            IReadOnlyList<int> nodeStarts,
            int position)
        {
            for (int i = 0; i < textNodes.Count; i++)
            {
                int nodeEnd = nodeStarts[i] + textNodes[i].Text.Length;
                if (position >= nodeStarts[i] && position < nodeEnd)
                    return i;
            }

            throw new InvalidDataException("Не удалось определить часть текста для замены плейсхолдера.");
        }

        private static int FindPlaceholderFormatNodeIndex(
            IReadOnlyList<Text> textNodes,
            int startNodeIndex,
            int endNodeIndex,
            int startOffset,
            int endOffset)
        {
            int bestNodeIndex = startNodeIndex;
            int bestScore = -1;
            for (int i = startNodeIndex; i <= endNodeIndex; i++)
            {
                string text = textNodes[i].Text;
                int segmentStart = i == startNodeIndex ? startOffset : 0;
                int segmentEnd = i == endNodeIndex ? endOffset : text.Length;
                int score = text[segmentStart..segmentEnd].Count(static character =>
                    character != '{' && character != '}' && !char.IsWhiteSpace(character));
                if (score <= bestScore)
                    continue;

                bestNodeIndex = i;
                bestScore = score;
            }

            return bestNodeIndex;
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

        private sealed class GdiTextWidthMeasurer : IDisposable
        {
            private const int LogPixelsX = 88;
            private const int LogPixelsY = 90;
            private const int NormalFontWeight = 400;
            private const uint RussianCharset = 204;
            private const uint ClearTypeQuality = 5;

            private readonly IntPtr _deviceContext;
            private readonly IntPtr _font;
            private readonly IntPtr _previousFont;
            private readonly int _dpiX;
            private bool _disposed;

            public GdiTextWidthMeasurer(string fontName, float fontSizePoints)
            {
                if (!OperatingSystem.IsWindows())
                    throw new PlatformNotSupportedException("Измерение текста DOCX поддерживается только в Windows.");

                _deviceContext = CreateCompatibleDC(IntPtr.Zero);
                if (_deviceContext == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Не удалось создать контекст измерения текста.");

                _dpiX = GetDeviceCaps(_deviceContext, LogPixelsX);
                int dpiY = GetDeviceCaps(_deviceContext, LogPixelsY);
                if (_dpiX <= 0 || dpiY <= 0)
                {
                    DeleteDC(_deviceContext);
                    throw new InvalidOperationException("Не удалось определить разрешение контекста измерения текста.");
                }

                int fontHeight = -(int)Math.Round(fontSizePoints * dpiY / 72F, MidpointRounding.AwayFromZero);
                _font = CreateFontW(
                    fontHeight,
                    0,
                    0,
                    0,
                    NormalFontWeight,
                    0,
                    0,
                    0,
                    RussianCharset,
                    0,
                    0,
                    ClearTypeQuality,
                    0,
                    fontName);
                if (_font == IntPtr.Zero)
                {
                    DeleteDC(_deviceContext);
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Не удалось загрузить шрифт {fontName}.");
                }

                _previousFont = SelectObject(_deviceContext, _font);
                if (_previousFont == IntPtr.Zero || _previousFont == new IntPtr(-1))
                {
                    DeleteObject(_font);
                    DeleteDC(_deviceContext);
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Не удалось выбрать шрифт для измерения текста.");
                }
            }

            public float MeasureWidthPoints(string value)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (string.IsNullOrEmpty(value))
                    return 0F;

                if (!GetTextExtentPoint32W(_deviceContext, value, value.Length, out NativeSize size))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Не удалось измерить ширину текста.");

                return size.Width * 72F / _dpiX;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                SelectObject(_deviceContext, _previousFont);
                DeleteObject(_font);
                DeleteDC(_deviceContext);
                _disposed = true;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct NativeSize
            {
                public int Width;

                public int Height;
            }

            [DllImport("gdi32.dll", SetLastError = true)]
            private static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);

            [DllImport("gdi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool DeleteDC(IntPtr deviceContext);

            [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern IntPtr CreateFontW(
                int height,
                int width,
                int escapement,
                int orientation,
                int weight,
                uint italic,
                uint underline,
                uint strikeOut,
                uint characterSet,
                uint outputPrecision,
                uint clipPrecision,
                uint quality,
                uint pitchAndFamily,
                string faceName);

            [DllImport("gdi32.dll", SetLastError = true)]
            private static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr graphicsObject);

            [DllImport("gdi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool DeleteObject(IntPtr graphicsObject);

            [DllImport("gdi32.dll")]
            private static extern int GetDeviceCaps(IntPtr deviceContext, int index);

            [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool GetTextExtentPoint32W(
                IntPtr deviceContext,
                string text,
                int textLength,
                out NativeSize size);
        }

        private readonly record struct TextFormat(
            string FontName,
            float FontSizePoints);

        private readonly record struct TextFieldLayout(
            float UsableWidthPoints,
            string FontName,
            float FontSizePoints);

        private sealed record ExpandableParagraphField(
            Paragraph MarkerParagraph,
            Paragraph LineParagraph,
            Indentation? Indentation,
            TextFieldLayout Layout);

        private static KnowledgeBaseActDocxGenerationResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
