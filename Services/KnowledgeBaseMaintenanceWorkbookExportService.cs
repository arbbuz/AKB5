using System.Globalization;
using System.Text.RegularExpressions;
using AsutpKnowledgeBase.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceWorkbookExportResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public byte[]? WorkbookPackage { get; init; }
    }

    public sealed partial class KnowledgeBaseMaintenanceWorkbookExportService
    {
        private const int FirstDayColumnIndex = 6; // F
        private const int TotalHoursColumnIndex = 37; // AK
        private const int NotesColumnIndex = 38; // AL
        private const int HiddenMergeColumnIndex = 40; // AN
        private const int SheetColumnSpanEndIndex = 43; // AQ
        private const string TotalsLabelText = "Итого:";
        private const string PlanText = "план";
        private const string FactText = "факт";
        private const string DefaultDashText = "-";

        private readonly KnowledgeBaseMaintenanceWorkbookTemplateService _templateService;

        public KnowledgeBaseMaintenanceWorkbookExportService(
            KnowledgeBaseMaintenanceWorkbookTemplateService? templateService = null)
        {
            _templateService = templateService ?? new KnowledgeBaseMaintenanceWorkbookTemplateService();
        }

        public KnowledgeBaseMaintenanceWorkbookExportResult ExportMonth(
            byte[]? existingWorkbookPackage,
            KbMaintenanceMonthSheetModel? sheetModel)
        {
            if (sheetModel == null)
                return Failure("Отсутствует модель листа графика ТО.");

            if (sheetModel.Month is < 1 or > 12)
                return Failure("Месяц листа графика ТО должен быть в диапазоне от 1 до 12.");

            if (sheetModel.Year < 1)
                return Failure("Год листа графика ТО должен быть положительным.");

            byte[] workbookBytes = existingWorkbookPackage is { Length: > 0 }
                ? existingWorkbookPackage.ToArray()
                : _templateService.GetTemplatePackage();
            byte[] templateBytes = _templateService.GetTemplatePackage();

            try
            {
                using var workbookStream = new MemoryStream(workbookBytes);
                using (var templateStream = new MemoryStream(templateBytes, writable: false))
                using (SpreadsheetDocument workbookDocument = SpreadsheetDocument.Open(workbookStream, true))
                using (SpreadsheetDocument templateDocument = SpreadsheetDocument.Open(templateStream, false))
                {
                    WorkbookPart workbookPart = workbookDocument.WorkbookPart
                        ?? throw new InvalidOperationException("Книга графика ТО повреждена: отсутствует workbook part.");
                    WorkbookPart templateWorkbookPart = templateDocument.WorkbookPart
                        ?? throw new InvalidOperationException("Встроенный шаблон графика ТО повреждён: отсутствует workbook part.");

                    Sheet targetSheet = FindMonthSheet(workbookPart, sheetModel.Month);
                    Sheet templateSheet = FindMonthSheet(templateWorkbookPart, sheetModel.Month);
                    WorksheetPart targetWorksheetPart = GetWorksheetPart(workbookPart, targetSheet);
                    WorksheetPart templateWorksheetPart = GetWorksheetPart(templateWorkbookPart, templateSheet);

                    SheetLayout targetLayout = SheetLayout.Read(targetWorksheetPart, requireDetailTemplate: false);
                    SheetLayout templateLayout = SheetLayout.Read(templateWorksheetPart, requireDetailTemplate: true);

                    RewriteSelectedMonthSheet(
                        workbookPart,
                        targetSheet,
                        targetWorksheetPart,
                        targetLayout,
                        templateWorksheetPart,
                        templateLayout,
                        sheetModel);

                    ResetWorkbookCalculationChain(workbookPart);
                    targetWorksheetPart.Worksheet.Save();
                    workbookPart.Workbook.Save();
                }

                return new KnowledgeBaseMaintenanceWorkbookExportResult
                {
                    IsSuccess = true,
                    WorkbookPackage = workbookStream.ToArray()
                };
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }

        private static void RewriteSelectedMonthSheet(
            WorkbookPart workbookPart,
            Sheet targetSheet,
            WorksheetPart targetWorksheetPart,
            SheetLayout targetLayout,
            WorksheetPart templateWorksheetPart,
            SheetLayout templateLayout,
            KbMaintenanceMonthSheetModel sheetModel)
        {
            Worksheet targetWorksheet = targetWorksheetPart.Worksheet;
            Worksheet templateWorksheet = templateWorksheetPart.Worksheet;
            SheetData targetSheetData = targetWorksheet.GetFirstChild<SheetData>()
                ?? throw new InvalidOperationException("Лист графика ТО повреждён: отсутствует sheetData.");
            SheetData templateSheetData = templateWorksheet.GetFirstChild<SheetData>()
                ?? throw new InvalidOperationException("Шаблон графика ТО повреждён: отсутствует sheetData.");

            Row systemHeaderTopTemplate = CloneRow(FindRequiredRow(templateSheetData, templateLayout.FirstSystemRowIndex));
            Row systemHeaderBottomTemplate = CloneRow(FindRequiredRow(templateSheetData, templateLayout.FirstSystemRowIndex + 1));
            Row detailPlanTemplate = CloneRow(FindRequiredRow(templateSheetData, templateLayout.FirstDetailPlanRowIndex!.Value));
            Row detailFactTemplate = CloneRow(FindRequiredRow(templateSheetData, templateLayout.FirstDetailPlanRowIndex.Value + 1));
            IReadOnlyList<Row> footerTemplates = Enumerable
                .Range((int)templateLayout.FooterStartRowIndex, (int)templateLayout.FooterRowCount)
                .Select(index => CloneRow(FindRequiredRow(templateSheetData, (uint)index)))
                .ToArray();
            IReadOnlyList<string> templateFooterMerges = ReadMergedRanges(templateWorksheet)
                .Where(range => RangeIntersectsRows(range, templateLayout.FooterStartRowIndex, templateLayout.FooterEndRowIndex))
                .ToArray();

            uint clearEndRowIndex = GetLastUsedRowIndex(targetSheetData, targetLayout.FooterEndRowIndex);
            RemoveRows(targetSheetData, targetLayout.DataStartRowIndex, clearEndRowIndex);
            MergeCells mergeCells = GetOrCreateMergeCells(targetWorksheet);
            ClearMergedRanges(mergeCells, targetLayout.DataStartRowIndex, clearEndRowIndex);
            ClearRowBreaks(targetWorksheet);

            WriteHeader(targetWorksheet, targetLayout, sheetModel);

            uint currentRowIndex = targetLayout.DataStartRowIndex;
            foreach (KbMaintenanceMonthSheetSystemGroup systemGroup in sheetModel.SystemGroups)
            {
                uint groupStartRowIndex = currentRowIndex;
                uint groupEndRowIndex = groupStartRowIndex + 1U + (uint)(systemGroup.DetailRows.Count * 2);

                Row headerTopRow = CloneRowToIndex(systemHeaderTopTemplate, currentRowIndex);
                Row headerBottomRow = CloneRowToIndex(systemHeaderBottomTemplate, currentRowIndex + 1);
                PopulateSystemHeaderRows(headerTopRow, headerBottomRow, systemGroup);
                targetSheetData.Append(headerTopRow, headerBottomRow);

                AddMerge(mergeCells, 1, 1, groupStartRowIndex, groupEndRowIndex);
                AddMerge(mergeCells, 2, 2, currentRowIndex, currentRowIndex + 1);
                AddMerge(mergeCells, 3, 3, currentRowIndex, currentRowIndex + 1);
                AddMerge(mergeCells, 4, 4, currentRowIndex, currentRowIndex + 1);
                AddMerge(mergeCells, NotesColumnIndex, NotesColumnIndex, currentRowIndex, currentRowIndex + 1);
                AddMerge(mergeCells, HiddenMergeColumnIndex, HiddenMergeColumnIndex, currentRowIndex, currentRowIndex + 1);

                currentRowIndex += 2;
                foreach (KbMaintenanceMonthSheetDetailRow detailRow in systemGroup.DetailRows)
                {
                    Row planRow = CloneRowToIndex(detailPlanTemplate, currentRowIndex);
                    Row factRow = CloneRowToIndex(detailFactTemplate, currentRowIndex + 1);
                    PopulateDetailRows(planRow, factRow, detailRow);
                    targetSheetData.Append(planRow, factRow);

                    AddMerge(mergeCells, 2, 2, currentRowIndex, currentRowIndex + 1);
                    AddMerge(mergeCells, 3, 3, currentRowIndex, currentRowIndex + 1);
                    AddMerge(mergeCells, 4, 4, currentRowIndex, currentRowIndex + 1);
                    AddMerge(mergeCells, NotesColumnIndex, NotesColumnIndex, currentRowIndex, currentRowIndex + 1);
                    AddMerge(mergeCells, HiddenMergeColumnIndex, HiddenMergeColumnIndex, currentRowIndex, currentRowIndex + 1);

                    currentRowIndex += 2;
                }
            }

            uint footerStartRowIndex = currentRowIndex;
            for (int index = 0; index < footerTemplates.Count; index++)
            {
                Row footerRow = CloneRowToIndex(footerTemplates[index], footerStartRowIndex + (uint)index);
                targetSheetData.Append(footerRow);
            }

            int footerMergeRowDelta = (int)footerStartRowIndex - (int)templateLayout.FooterStartRowIndex;
            foreach (string mergeRange in templateFooterMerges)
            {
                AddShiftedMerge(mergeCells, mergeRange, footerMergeRowDelta);
            }

            PopulateFooter(
                targetWorksheet,
                footerStartRowIndex,
                targetLayout.DataStartRowIndex,
                footerStartRowIndex == targetLayout.DataStartRowIndex
                    ? targetLayout.DataStartRowIndex - 1
                    : footerStartRowIndex - 1,
                sheetModel);

            UpdateWorksheetDimension(targetWorksheet, footerStartRowIndex + (uint)footerTemplates.Count - 1);
            UpdateDefinedRanges(workbookPart, targetSheet, targetLayout, footerStartRowIndex);
            ResetWorksheetView(targetWorksheet, targetLayout.DataStartRowIndex);
            mergeCells.Count = (uint)mergeCells.ChildElements.Count;
        }

        private static void WriteHeader(
            Worksheet worksheet,
            SheetLayout layout,
            KbMaintenanceMonthSheetModel sheetModel)
        {
            SetSheetCellText(worksheet, layout.MonthTitleRowIndex, 1, $"на {GetRussianMonthName(sheetModel.Month)} {sheetModel.Year} года");
            SetSheetCellText(
                worksheet,
                layout.ApprovalYearRowIndex,
                layout.ApprovalYearColumnIndex,
                $"____ _______________ {sheetModel.Year} года");

            int[] dayTotals = BuildDayTotals(sheetModel);
            SetSheetCellNumber(worksheet, layout.TopSummaryRowIndex, 5, sheetModel.TotalPlannedHours);
            SetSheetCellNumber(worksheet, layout.TopSummaryRowIndex, TotalHoursColumnIndex, sheetModel.TotalPlannedHours);

            for (int dayOfMonth = 1; dayOfMonth <= 31; dayOfMonth++)
            {
                int dayColumnIndex = FirstDayColumnIndex + dayOfMonth - 1;
                SetSheetCellNumber(worksheet, layout.TopSummaryRowIndex, dayColumnIndex, dayTotals[dayOfMonth - 1]);
                SetSheetCellNumber(worksheet, layout.BottomSummaryRowIndex, dayColumnIndex, dayTotals[dayOfMonth - 1]);
            }

            double averageDailyHours = sheetModel.WorkingDayCount > 0
                ? (double)sheetModel.TotalPlannedHours / sheetModel.WorkingDayCount
                : 0d;
            SetSheetCellNumber(worksheet, layout.AverageRowIndex, TotalHoursColumnIndex, averageDailyHours);
            SetSheetCellNumber(worksheet, layout.AverageRowIndex, NotesColumnIndex, sheetModel.WorkingDayCount);
        }

        private static void PopulateSystemHeaderRows(
            Row headerTopRow,
            Row headerBottomRow,
            KbMaintenanceMonthSheetSystemGroup systemGroup)
        {
            SetCellNumber(headerTopRow, 1, systemGroup.SequenceNumber);
            SetCellText(headerTopRow, 2, NormalizeText(systemGroup.SystemName));
            SetCellText(headerTopRow, 3, DefaultDashText);
            SetCellText(headerTopRow, 4, NormalizeText(systemGroup.InventoryNumber, DefaultDashText));
            ClearRowValues(headerTopRow, startColumnIndex: 5, endColumnIndex: SheetColumnSpanEndIndex);
            ClearRowValues(headerBottomRow, startColumnIndex: 1, endColumnIndex: SheetColumnSpanEndIndex);
        }

        private static void PopulateDetailRows(
            Row planRow,
            Row factRow,
            KbMaintenanceMonthSheetDetailRow detailRow)
        {
            SetCellText(planRow, 2, NormalizeText(detailRow.NodeName));
            SetCellText(planRow, 3, DefaultDashText);
            SetCellText(planRow, 4, DefaultDashText);
            SetCellText(planRow, 5, PlanText);
            ClearRowValues(planRow, FirstDayColumnIndex, SheetColumnSpanEndIndex);

            foreach (KbMaintenanceMonthSheetDayCell dayCell in detailRow.DayCells)
            {
                if (dayCell.DayOfMonth is < 1 or > 31)
                    continue;

                int dayColumnIndex = FirstDayColumnIndex + dayCell.DayOfMonth - 1;
                SetCellText(planRow, dayColumnIndex, BuildPlanCellText(dayCell.WorkEntries));
            }

            SetCellNumber(planRow, TotalHoursColumnIndex, detailRow.TotalHours);

            ClearRowValues(factRow, 1, SheetColumnSpanEndIndex);
            SetCellText(factRow, 5, FactText);
        }

        private static void PopulateFooter(
            Worksheet worksheet,
            uint footerStartRowIndex,
            uint dataStartRowIndex,
            uint dataEndRowIndex,
            KbMaintenanceMonthSheetModel sheetModel)
        {
            uint totalsRowIndex = footerStartRowIndex;
            uint dayCountRowIndex = footerStartRowIndex + 1;
            uint groupedTotalsRowIndex = footerStartRowIndex + 2;

            SetSheetCellText(worksheet, totalsRowIndex, 2, TotalsLabelText);
            if (sheetModel.SystemGroups.Count == 0)
            {
                SetSheetCellNumber(worksheet, totalsRowIndex, TotalHoursColumnIndex, 0);
            }
            else
            {
                SetSheetCellFormula(
                    worksheet,
                    totalsRowIndex,
                    TotalHoursColumnIndex,
                    $"SUM({GetCellReference(dataStartRowIndex, TotalHoursColumnIndex)}:{GetCellReference(dataEndRowIndex, TotalHoursColumnIndex)})");
            }

            for (int dayOfMonth = 1; dayOfMonth <= 31; dayOfMonth++)
            {
                int dayColumnIndex = FirstDayColumnIndex + dayOfMonth - 1;
                if (dayOfMonth <= DateTime.DaysInMonth(sheetModel.Year, sheetModel.Month) && sheetModel.SystemGroups.Count > 0)
                {
                    SetSheetCellFormula(
                        worksheet,
                        dayCountRowIndex,
                        dayColumnIndex,
                        $"COUNTA({GetCellReference(dataStartRowIndex, dayColumnIndex)}:{GetCellReference(dataEndRowIndex, dayColumnIndex)})");
                }
                else
                {
                    SetSheetCellNumber(worksheet, dayCountRowIndex, dayColumnIndex, 0);
                }
            }

            SetSheetCellFormula(worksheet, groupedTotalsRowIndex, 6, $"SUM(F{dayCountRowIndex}:M{dayCountRowIndex})");
            SetSheetCellFormula(worksheet, groupedTotalsRowIndex, 13, $"SUM(N{dayCountRowIndex}:T{dayCountRowIndex})");
            SetSheetCellFormula(worksheet, groupedTotalsRowIndex, 20, $"SUM(U{dayCountRowIndex}:AA{dayCountRowIndex})");
            SetSheetCellFormula(worksheet, groupedTotalsRowIndex, 27, $"SUM(AB{dayCountRowIndex}:AH{dayCountRowIndex})");
            SetSheetCellFormula(worksheet, groupedTotalsRowIndex, 34, $"SUM(AI{dayCountRowIndex}:AJ{dayCountRowIndex})");
            SetSheetCellFormula(worksheet, groupedTotalsRowIndex, TotalHoursColumnIndex, $"SUM(F{groupedTotalsRowIndex}:AJ{groupedTotalsRowIndex})");
        }

        private static void UpdateWorksheetDimension(Worksheet worksheet, uint footerEndRowIndex)
        {
            SheetDimension? sheetDimension = worksheet.Elements<SheetDimension>().FirstOrDefault();
            if (sheetDimension == null)
            {
                sheetDimension = new SheetDimension();
                worksheet.InsertAt(sheetDimension, 0);
            }

            string endColumn = GetRangeEndColumn(sheetDimension.Reference?.Value) ?? "AQ";
            sheetDimension.Reference = $"A1:{endColumn}{footerEndRowIndex}";
        }

        private static void UpdateDefinedRanges(
            WorkbookPart workbookPart,
            Sheet targetSheet,
            SheetLayout targetLayout,
            uint footerStartRowIndex)
        {
            Workbook workbook = workbookPart.Workbook;
            DefinedNames definedNames = workbook.DefinedNames ?? workbook.AppendChild(new DefinedNames());
            List<Sheet> sheets = workbook.Sheets!.Elements<Sheet>().ToList();
            int localSheetId = sheets.FindIndex(sheet => ReferenceEquals(sheet, targetSheet));
            if (localSheetId < 0)
                return;

            UpdateDefinedNameRange(
                definedNames,
                targetSheet.Name?.Value ?? string.Empty,
                localSheetId,
                "_xlnm._FilterDatabase",
                startRow: targetLayout.HeaderBottomRowIndex,
                endRow: footerStartRowIndex + 3,
                fallbackEndColumn: "AQ");
            UpdateDefinedNameRange(
                definedNames,
                targetSheet.Name?.Value ?? string.Empty,
                localSheetId,
                "_xlnm.Print_Titles",
                startRow: targetLayout.HeaderTopRowIndex,
                endRow: targetLayout.HeaderBottomRowIndex,
                fallbackEndColumn: null);
            UpdateDefinedNameRange(
                definedNames,
                targetSheet.Name?.Value ?? string.Empty,
                localSheetId,
                "_xlnm.Print_Area",
                startRow: 1,
                endRow: footerStartRowIndex + 6,
                fallbackEndColumn: "AQ");
        }

        private static void UpdateDefinedNameRange(
            DefinedNames definedNames,
            string sheetName,
            int localSheetId,
            string definedName,
            uint startRow,
            uint endRow,
            string? fallbackEndColumn)
        {
            DefinedName? entry = definedNames.Elements<DefinedName>()
                .FirstOrDefault(item =>
                    string.Equals(item.Name?.Value, definedName, StringComparison.Ordinal) &&
                    item.LocalSheetId?.Value == (uint)localSheetId);
            if (entry == null)
                return;

            if (string.Equals(definedName, "_xlnm.Print_Titles", StringComparison.Ordinal))
            {
                entry.Text = $"'{sheetName}'!${startRow}:${endRow}";
                return;
            }

            string endColumn = (GetRangeEndColumn(entry.Text) ?? fallbackEndColumn ?? "AQ").TrimStart('$');
            entry.Text = $"'{sheetName}'!$A${startRow}:$" + endColumn + $"${endRow}";
        }

        private static void ResetWorkbookCalculationChain(WorkbookPart workbookPart)
        {
            if (workbookPart.CalculationChainPart != null)
                workbookPart.DeletePart(workbookPart.CalculationChainPart);

            CalculationProperties calculationProperties =
                workbookPart.Workbook.CalculationProperties ?? workbookPart.Workbook.AppendChild(new CalculationProperties());
            calculationProperties.CalculationMode = CalculateModeValues.Auto;
            calculationProperties.ForceFullCalculation = true;
            calculationProperties.FullCalculationOnLoad = true;
        }

        private static void ResetWorksheetView(Worksheet worksheet, uint firstDataRowIndex)
        {
            SheetViews? sheetViews = worksheet.GetFirstChild<SheetViews>();
            SheetView? sheetView = sheetViews?.Elements<SheetView>().FirstOrDefault();
            if (sheetView == null)
                return;

            Pane? pane = sheetView.Elements<Pane>().FirstOrDefault();
            if (pane != null)
                pane.TopLeftCell = $"E{firstDataRowIndex}";

            foreach (Selection selection in sheetView.Elements<Selection>())
            {
                if (selection.Pane?.Value == PaneValues.TopRight)
                {
                    selection.ActiveCell = "E1";
                    selection.SequenceOfReferences = new ListValue<StringValue> { InnerText = "E1" };
                    continue;
                }

                if (selection.Pane?.Value == PaneValues.BottomLeft)
                {
                    selection.ActiveCell = $"A{firstDataRowIndex}";
                    selection.SequenceOfReferences = new ListValue<StringValue> { InnerText = $"A{firstDataRowIndex}" };
                    continue;
                }

                selection.ActiveCell = $"E{firstDataRowIndex}";
                selection.SequenceOfReferences = new ListValue<StringValue> { InnerText = $"E{firstDataRowIndex}" };
            }
        }

        private static void SetSheetCellText(Worksheet worksheet, uint rowIndex, int columnIndex, string value)
        {
            Row row = GetOrCreateRow(worksheet, rowIndex);
            SetCellText(row, columnIndex, value);
        }

        private static void SetSheetCellNumber(Worksheet worksheet, uint rowIndex, int columnIndex, double value)
        {
            Row row = GetOrCreateRow(worksheet, rowIndex);
            SetCellNumber(row, columnIndex, value);
        }

        private static void SetSheetCellFormula(Worksheet worksheet, uint rowIndex, int columnIndex, string formula)
        {
            Row row = GetOrCreateRow(worksheet, rowIndex);
            SetCellFormula(row, columnIndex, formula);
        }

        private static Row GetOrCreateRow(Worksheet worksheet, uint rowIndex)
        {
            SheetData sheetData = worksheet.GetFirstChild<SheetData>()
                ?? worksheet.AppendChild(new SheetData());
            Row? row = sheetData.Elements<Row>().FirstOrDefault(candidate => candidate.RowIndex?.Value == rowIndex);
            if (row != null)
                return row;

            row = new Row { RowIndex = rowIndex };
            Row? nextRow = sheetData.Elements<Row>().FirstOrDefault(candidate => candidate.RowIndex?.Value > rowIndex);
            if (nextRow == null)
                sheetData.Append(row);
            else
                sheetData.InsertBefore(row, nextRow);

            return row;
        }

        private static Row CloneRowToIndex(Row templateRow, uint newRowIndex)
        {
            Row clone = CloneRow(templateRow);
            clone.RowIndex = newRowIndex;

            foreach (Cell cell in clone.Elements<Cell>())
            {
                string? cellReference = cell.CellReference?.Value;
                if (string.IsNullOrWhiteSpace(cellReference))
                    continue;

                string columnName = Regex.Replace(cellReference, @"\d", string.Empty);
                cell.CellReference = $"{columnName}{newRowIndex}";
            }

            return clone;
        }

        private static Row CloneRow(Row row) =>
            (Row)row.CloneNode(true);

        private static Row FindRequiredRow(SheetData sheetData, uint rowIndex) =>
            sheetData.Elements<Row>().FirstOrDefault(row => row.RowIndex?.Value == rowIndex)
            ?? throw new InvalidOperationException($"Шаблон графика ТО повреждён: отсутствует строка {rowIndex}.");

        private static void RemoveRows(SheetData sheetData, uint startRowIndex, uint endRowIndex)
        {
            foreach (Row row in sheetData.Elements<Row>()
                         .Where(row =>
                         {
                             uint rowIndex = row.RowIndex?.Value ?? 0;
                             return rowIndex >= startRowIndex && rowIndex <= endRowIndex;
                         })
                         .ToList())
            {
                row.Remove();
            }
        }

        private static uint GetLastUsedRowIndex(SheetData sheetData, uint fallbackRowIndex)
        {
            uint lastRowIndex = sheetData.Elements<Row>()
                .Select(row => row.RowIndex?.Value ?? 0)
                .DefaultIfEmpty(fallbackRowIndex)
                .Max();

            return Math.Max(fallbackRowIndex, lastRowIndex);
        }

        private static void ClearRowBreaks(Worksheet worksheet)
        {
            RowBreaks? rowBreaks = worksheet.Elements<RowBreaks>().FirstOrDefault();
            rowBreaks?.Remove();
        }

        private static MergeCells GetOrCreateMergeCells(Worksheet worksheet)
        {
            MergeCells? mergeCells = worksheet.Elements<MergeCells>().FirstOrDefault();
            if (mergeCells != null)
                return mergeCells;

            SheetData sheetData = worksheet.GetFirstChild<SheetData>()
                ?? throw new InvalidOperationException("Лист графика ТО повреждён: отсутствует sheetData.");
            mergeCells = new MergeCells();
            worksheet.InsertAfter(mergeCells, sheetData);
            return mergeCells;
        }

        private static IReadOnlyList<string> ReadMergedRanges(Worksheet worksheet) =>
            worksheet.Elements<MergeCells>().FirstOrDefault()?
                .Elements<MergeCell>()
                .Select(cell => cell.Reference?.Value ?? string.Empty)
                .Where(reference => !string.IsNullOrWhiteSpace(reference))
                .ToArray()
            ?? Array.Empty<string>();

        private static void ClearMergedRanges(MergeCells mergeCells, uint startRowIndex, uint endRowIndex)
        {
            foreach (MergeCell mergeCell in mergeCells.Elements<MergeCell>()
                         .Where(cell => RangeIntersectsRows(cell.Reference?.Value, startRowIndex, endRowIndex))
                         .ToList())
            {
                mergeCell.Remove();
            }
        }

        private static bool RangeIntersectsRows(string? range, uint startRowIndex, uint endRowIndex)
        {
            if (!TryParseRange(range, out (string _, uint startRow, string __, uint endRow) parsed))
                return false;

            return !(parsed.endRow < startRowIndex || parsed.startRow > endRowIndex);
        }

        private static void AddMerge(MergeCells mergeCells, int startColumnIndex, int endColumnIndex, uint startRowIndex, uint endRowIndex)
        {
            if (startRowIndex > endRowIndex)
                return;

            mergeCells.Append(new MergeCell
            {
                Reference = $"{GetColumnName(startColumnIndex)}{startRowIndex}:{GetColumnName(endColumnIndex)}{endRowIndex}"
            });
        }

        private static void AddShiftedMerge(MergeCells mergeCells, string range, int rowDelta)
        {
            if (!TryParseRange(range, out (string startColumn, uint startRow, string endColumn, uint endRow) parsed))
                return;

            int shiftedStartRow = (int)parsed.startRow + rowDelta;
            int shiftedEndRow = (int)parsed.endRow + rowDelta;
            if (shiftedStartRow < 1 || shiftedEndRow < 1)
                return;

            mergeCells.Append(new MergeCell
            {
                Reference = $"{parsed.startColumn}{shiftedStartRow}:{parsed.endColumn}{shiftedEndRow}"
            });
        }

        private static bool TryParseRange(
            string? range,
            out (string startColumn, uint startRow, string endColumn, uint endRow) parsed)
        {
            parsed = default;
            if (string.IsNullOrWhiteSpace(range))
                return false;

            Match match = Regex.Match(
                range,
                @"^(?<startCol>[A-Z]+)(?<startRow>\d+):(?<endCol>[A-Z]+)(?<endRow>\d+)$",
                RegexOptions.CultureInvariant);
            if (!match.Success)
                return false;

            parsed = (
                match.Groups["startCol"].Value,
                uint.Parse(match.Groups["startRow"].Value, CultureInfo.InvariantCulture),
                match.Groups["endCol"].Value,
                uint.Parse(match.Groups["endRow"].Value, CultureInfo.InvariantCulture));
            return true;
        }

        private static void ClearRowValues(Row row, int startColumnIndex, int endColumnIndex)
        {
            for (int columnIndex = startColumnIndex; columnIndex <= endColumnIndex; columnIndex++)
            {
                ClearCellValue(GetOrCreateCell(row, columnIndex));
            }
        }

        private static void SetCellText(Row row, int columnIndex, string value)
        {
            Cell cell = GetOrCreateCell(row, columnIndex);
            ClearCellValue(cell);
            cell.DataType = CellValues.InlineString;
            cell.InlineString = new InlineString(new Text(value) { Space = SpaceProcessingModeValues.Preserve });
        }

        private static void SetCellNumber(Row row, int columnIndex, double value)
        {
            Cell cell = GetOrCreateCell(row, columnIndex);
            ClearCellValue(cell);
            cell.CellValue = new CellValue(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void SetCellFormula(Row row, int columnIndex, string formula)
        {
            Cell cell = GetOrCreateCell(row, columnIndex);
            ClearCellValue(cell);
            cell.CellFormula = new CellFormula(formula);
        }

        private static void ClearCellValue(Cell cell)
        {
            cell.DataType = null;
            cell.CellValue = null;
            cell.InlineString = null;
            cell.CellFormula = null;
        }

        private static Cell GetOrCreateCell(Row row, int columnIndex)
        {
            string reference = GetCellReference(row.RowIndex?.Value ?? 0, columnIndex);
            Cell? cell = row.Elements<Cell>()
                .FirstOrDefault(candidate => string.Equals(candidate.CellReference?.Value, reference, StringComparison.Ordinal));
            if (cell != null)
                return cell;

            cell = new Cell { CellReference = reference };
            Cell? nextCell = row.Elements<Cell>()
                .FirstOrDefault(candidate => GetColumnIndex(candidate.CellReference?.Value) > columnIndex);
            if (nextCell == null)
                row.Append(cell);
            else
                row.InsertBefore(cell, nextCell);

            return cell;
        }

        private static int FindRightmostPopulatedColumn(Worksheet worksheet, uint rowIndex)
        {
            SheetData sheetData = worksheet.GetFirstChild<SheetData>()
                ?? throw new InvalidOperationException("Лист графика ТО повреждён: отсутствует sheetData.");
            Row row = FindRequiredRow(sheetData, rowIndex);
            return row.Elements<Cell>()
                .Select(cell => GetColumnIndex(cell.CellReference?.Value))
                .DefaultIfEmpty(1)
                .Max();
        }

        private static int[] BuildDayTotals(KbMaintenanceMonthSheetModel sheetModel)
        {
            var totals = new int[31];
            foreach (KbMaintenanceMonthSheetDayTotal dailyTotal in sheetModel.DailyTotals)
            {
                if (dailyTotal.DayOfMonth is < 1 or > 31)
                    continue;

                totals[dailyTotal.DayOfMonth - 1] = dailyTotal.TotalHours;
            }

            return totals;
        }

        private static string BuildPlanCellText(IEnumerable<KbMaintenanceMonthSheetWorkEntry> workEntries) =>
            string.Join("; ", workEntries
                .Select(entry => entry.PlanText?.Trim() ?? string.Empty)
                .Where(text => !string.IsNullOrWhiteSpace(text)));

        private static string NormalizeText(string? value, string fallback = DefaultDashText)
        {
            string normalized = value?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(normalized)
                ? fallback
                : normalized;
        }

        private static string GetRussianMonthName(int month) =>
            new CultureInfo("ru-RU").DateTimeFormat.GetMonthName(month);

        private static string GetCellReference(uint rowIndex, int columnIndex) =>
            $"{GetColumnName(columnIndex)}{rowIndex}";

        private static string GetColumnName(int columnIndex)
        {
            int dividend = columnIndex;
            var columnName = string.Empty;
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = (char)('A' + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        private static int GetColumnIndex(string? cellReference)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
                return 0;

            string columnName = Regex.Replace(cellReference, @"\d", string.Empty);
            int columnIndex = 0;
            foreach (char character in columnName)
            {
                columnIndex *= 26;
                columnIndex += character - 'A' + 1;
            }

            return columnIndex;
        }

        private static string? GetRangeEndColumn(string? rangeText)
        {
            if (string.IsNullOrWhiteSpace(rangeText))
                return null;

            Match match = Regex.Match(
                rangeText,
                @"\$?(?<endCol>[A-Z]+)\$?(?<endRow>\d+)$",
                RegexOptions.CultureInvariant);

            return match.Success
                ? match.Groups["endCol"].Value
                : null;
        }

        private static Sheet FindMonthSheet(WorkbookPart workbookPart, int month)
        {
            string expectedName = $"КЦ ({month})";
            return workbookPart.Workbook.Sheets?
                .Elements<Sheet>()
                .FirstOrDefault(sheet => string.Equals(sheet.Name?.Value, expectedName, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"Лист '{expectedName}' не найден в книге графика ТО.");
        }

        private static WorksheetPart GetWorksheetPart(WorkbookPart workbookPart, Sheet sheet)
        {
            string relationshipId = sheet.Id?.Value
                ?? throw new InvalidOperationException($"Лист '{sheet.Name?.Value}' повреждён: отсутствует relationship id.");
            return (WorksheetPart)workbookPart.GetPartById(relationshipId);
        }

        private static KnowledgeBaseMaintenanceWorkbookExportResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private sealed record SheetLayout(
            uint TopSummaryRowIndex,
            uint AverageRowIndex,
            uint MonthTitleRowIndex,
            uint BottomSummaryRowIndex,
            uint HeaderTopRowIndex,
            uint HeaderBottomRowIndex,
            uint FirstSystemRowIndex,
            uint DataStartRowIndex,
            uint FooterStartRowIndex,
            uint FooterEndRowIndex,
            uint FooterRowCount,
            uint ApprovalYearRowIndex,
            int ApprovalYearColumnIndex,
            uint? FirstDetailPlanRowIndex)
        {
            public static SheetLayout Read(WorksheetPart worksheetPart, bool requireDetailTemplate)
            {
                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()
                    ?? throw new InvalidOperationException("Лист графика ТО повреждён: отсутствует sheetData.");
                IReadOnlyList<string> sharedStrings = ReadSharedStrings(
                    worksheetPart.GetParentParts().OfType<WorkbookPart>().FirstOrDefault()?.SharedStringTablePart);

                uint headerTopRowIndex = FindHeaderTopRowIndex(sheetData, sharedStrings);
                uint topSummaryRowIndex = headerTopRowIndex - 6;
                uint footerStartRowIndex = FindFooterStartRowIndex(sheetData, sharedStrings);
                (uint approvalYearRowIndex, int approvalYearColumnIndex) = FindApprovalYearCell(sheetData, sharedStrings, topSummaryRowIndex);
                uint? firstDetailPlanRowIndex = FindFirstPlanRowIndex(sheetData, sharedStrings, headerTopRowIndex + 2, footerStartRowIndex);
                if (requireDetailTemplate && firstDetailPlanRowIndex == null)
                {
                    throw new InvalidOperationException("Шаблон графика ТО повреждён: не найдена строка-шаблон 'план'.");
                }

                return new SheetLayout(
                    TopSummaryRowIndex: topSummaryRowIndex,
                    AverageRowIndex: headerTopRowIndex - 5,
                    MonthTitleRowIndex: headerTopRowIndex - 2,
                    BottomSummaryRowIndex: headerTopRowIndex - 1,
                    HeaderTopRowIndex: headerTopRowIndex,
                    HeaderBottomRowIndex: headerTopRowIndex + 1,
                    FirstSystemRowIndex: headerTopRowIndex + 2,
                    DataStartRowIndex: headerTopRowIndex + 2,
                    FooterStartRowIndex: footerStartRowIndex,
                    FooterEndRowIndex: footerStartRowIndex + 5,
                    FooterRowCount: 6,
                    ApprovalYearRowIndex: approvalYearRowIndex,
                    ApprovalYearColumnIndex: approvalYearColumnIndex,
                    FirstDetailPlanRowIndex: firstDetailPlanRowIndex);
            }

            private static uint FindHeaderTopRowIndex(SheetData sheetData, IReadOnlyList<string> sharedStrings)
            {
                Row? headerRow = sheetData.Elements<Row>()
                    .FirstOrDefault(row => string.Equals(ReadCellText(row, 1, sharedStrings), "N п/п", StringComparison.Ordinal));
                if (headerRow?.RowIndex?.Value is uint rowIndex)
                    return rowIndex;

                throw new InvalidOperationException("Лист графика ТО повреждён: не найдена строка заголовка таблицы.");
            }

            private static uint FindFooterStartRowIndex(SheetData sheetData, IReadOnlyList<string> sharedStrings)
            {
                Row? footerRow = sheetData.Elements<Row>()
                    .FirstOrDefault(row => string.Equals(ReadCellText(row, 2, sharedStrings), TotalsLabelText, StringComparison.Ordinal));
                if (footerRow?.RowIndex?.Value is uint rowIndex)
                    return rowIndex;

                throw new InvalidOperationException("Лист графика ТО повреждён: не найдена строка итогов.");
            }

            private static (uint RowIndex, int ColumnIndex) FindApprovalYearCell(
                SheetData sheetData,
                IReadOnlyList<string> sharedStrings,
                uint topSummaryRowIndex)
            {
                Row? approvalRow = sheetData.Elements<Row>()
                    .Where(row => (row.RowIndex?.Value ?? 0) < topSummaryRowIndex)
                    .LastOrDefault(row => row.Elements<Cell>()
                        .Select(cell => ReadCellText(cell, sharedStrings))
                        .Any(text => text.Contains("года", StringComparison.OrdinalIgnoreCase) && text.Contains('_', StringComparison.Ordinal)));
                if (approvalRow == null)
                {
                    throw new InvalidOperationException("Лист графика ТО повреждён: не найдена строка утверждения года.");
                }

                Cell approvalCell = approvalRow.Elements<Cell>()
                    .First(cell =>
                    {
                        string text = ReadCellText(cell, sharedStrings);
                        return text.Contains("года", StringComparison.OrdinalIgnoreCase) &&
                               text.Contains('_', StringComparison.Ordinal);
                    });

                return (approvalRow.RowIndex!.Value, GetColumnIndex(approvalCell.CellReference?.Value));
            }

            private static uint? FindFirstPlanRowIndex(
                SheetData sheetData,
                IReadOnlyList<string> sharedStrings,
                uint firstDataRowIndex,
                uint footerStartRowIndex)
            {
                Row? planRow = sheetData.Elements<Row>()
                    .FirstOrDefault(row =>
                    {
                        uint rowIndex = row.RowIndex?.Value ?? 0;
                        return rowIndex >= firstDataRowIndex &&
                               rowIndex < footerStartRowIndex &&
                               string.Equals(ReadCellText(row, 5, sharedStrings), PlanText, StringComparison.Ordinal);
                    });

                return planRow?.RowIndex?.Value;
            }

            private static string ReadCellText(Row row, int columnIndex, IReadOnlyList<string> sharedStrings)
            {
                Cell? cell = row.Elements<Cell>()
                    .FirstOrDefault(candidate =>
                        string.Equals(
                            Regex.Replace(candidate.CellReference?.Value ?? string.Empty, @"\d", string.Empty),
                            GetColumnName(columnIndex),
                            StringComparison.Ordinal));

                return cell == null
                    ? string.Empty
                    : ReadCellText(cell, sharedStrings);
            }

            private static string ReadCellText(Cell cell, IReadOnlyList<string> sharedStrings)
            {
                if (cell.DataType?.Value == CellValues.SharedString)
                {
                    if (cell.CellValue == null || !int.TryParse(cell.CellValue.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                        return string.Empty;

                    return index >= 0 && index < sharedStrings.Count
                        ? sharedStrings[index]
                        : string.Empty;
                }

                if (cell.DataType?.Value == CellValues.InlineString)
                {
                    return string.Concat(cell.InlineString?.Descendants<Text>().Select(text => text.Text) ?? Enumerable.Empty<string>());
                }

                return cell.CellValue?.Text ?? string.Empty;
            }

            private static IReadOnlyList<string> ReadSharedStrings(SharedStringTablePart? part)
            {
                if (part?.SharedStringTable == null)
                    return Array.Empty<string>();

                return part.SharedStringTable
                    .Elements<SharedStringItem>()
                    .Select(item => string.Concat(item.Descendants<Text>().Select(text => text.Text)))
                    .ToArray();
            }
        }
    }
}
