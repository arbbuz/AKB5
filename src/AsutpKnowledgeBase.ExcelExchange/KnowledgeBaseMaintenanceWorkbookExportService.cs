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
        private const int AnnualFirstMonthPlanColumnIndex = 5; // E
        private const int AnnualTotalHoursColumnIndex = 29; // AC
        private const int AnnualSheetColumnSpanEndIndex = 43; // AQ
        private const double DefaultGeneratedRowHeight = 18d;
        private const double TextLineHeight = 15d;
        private const double TextRowHeightPadding = 3d;
        private const int MonthlySystemNameCharactersPerLine = 36;
        private const int MonthlyDetailNameCharactersPerLine = 32;
        private const int MonthlyPlanCellCharactersPerLine = 8;
        private const int AnnualNameCharactersPerLine = 42;
        private const int AnnualPlanCellCharactersPerLine = 10;
        private const string TotalsLabelText = "Итого:";
        private const string AnnualTotalsLabelText = "Итого";
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
                using var workbookStream = CreateExpandableMemoryStream(workbookBytes);
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

        public KnowledgeBaseMaintenanceWorkbookExportResult PrepareYearRecalculationWorkbook(
            byte[]? existingWorkbookPackage,
            int startMonth)
        {
            if (existingWorkbookPackage is not { Length: > 0 })
            {
                return new KnowledgeBaseMaintenanceWorkbookExportResult
                {
                    IsSuccess = true,
                    WorkbookPackage = null
                };
            }

            if (startMonth is < 1 or > 12)
                return Failure("Стартовый месяц графика ТО должен быть в диапазоне от 1 до 12.");

            try
            {
                using var existingStream = new MemoryStream(existingWorkbookPackage, writable: false);
                using SpreadsheetDocument existingDocument = SpreadsheetDocument.Open(existingStream, false);
                WorkbookPart existingWorkbookPart = existingDocument.WorkbookPart
                    ?? throw new InvalidOperationException("Книга графика ТО повреждена: отсутствует workbook part.");

                if (HasUsableMonthSheets(existingWorkbookPart, startMonth, 12))
                {
                    return new KnowledgeBaseMaintenanceWorkbookExportResult
                    {
                        IsSuccess = true,
                        WorkbookPackage = existingWorkbookPackage.ToArray()
                    };
                }

                return new KnowledgeBaseMaintenanceWorkbookExportResult
                {
                    IsSuccess = true,
                    WorkbookPackage = BuildNormalizedYearRecalculationWorkbook(existingWorkbookPackage, startMonth)
                };
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }

        public KnowledgeBaseMaintenanceWorkbookExportResult ExportSingleMonth(KbMaintenanceMonthSheetModel? sheetModel)
        {
            if (sheetModel == null)
                return Failure("Отсутствует модель листа графика ТО.");

            KnowledgeBaseMaintenanceWorkbookExportResult exportResult = ExportMonth(null, sheetModel);
            if (!exportResult.IsSuccess || exportResult.WorkbookPackage == null)
                return exportResult;

            try
            {
                return new KnowledgeBaseMaintenanceWorkbookExportResult
                {
                    IsSuccess = true,
                    WorkbookPackage = PruneWorkbookToSingleMonth(exportResult.WorkbookPackage, sheetModel.Month)
                };
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }

        public KnowledgeBaseMaintenanceWorkbookExportResult ExportAnnual(KbMaintenanceAnnualWorkbookModel? workbookModel)
        {
            if (workbookModel == null)
                return Failure("Отсутствует модель годового графика ТО.");

            if (workbookModel.Year < 1)
                return Failure("Год годового графика ТО должен быть положительным.");

            byte[] workbookBytes = _templateService.GetAnnualTemplatePackage();

            try
            {
                using var workbookStream = CreateExpandableMemoryStream(workbookBytes);
                using (SpreadsheetDocument workbookDocument = SpreadsheetDocument.Open(workbookStream, true))
                {
                    WorkbookPart workbookPart = workbookDocument.WorkbookPart
                        ?? throw new InvalidOperationException("Книга годового графика ТО повреждена: отсутствует workbook part.");
                    Sheet targetSheet = FindAnnualSheet(workbookPart);
                    WorksheetPart targetWorksheetPart = GetWorksheetPart(workbookPart, targetSheet);
                    AnnualSheetLayout layout = AnnualSheetLayout.Read(targetWorksheetPart);

                    RewriteAnnualSheet(
                        workbookPart,
                        targetSheet,
                        targetWorksheetPart,
                        layout,
                        workbookModel);

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
            NormalizeMonthSheetHeaderRows(targetWorksheet, targetLayout);
            ApplyHeaderDayCalendarStyles(workbookPart, targetWorksheet, targetLayout, sheetModel);

            uint currentRowIndex = targetLayout.DataStartRowIndex;
            IReadOnlyList<OrderedMonthSystemGroup> orderedSystemGroups = OrderSystemGroupsByTemplate(
                sheetModel.SystemGroups,
                templateWorksheetPart,
                out int nextAppendedSequenceNumber);
            foreach (OrderedMonthSystemGroup orderedSystemGroup in orderedSystemGroups)
            {
                KbMaintenanceMonthSheetSystemGroup systemGroup = orderedSystemGroup.Group;
                uint groupStartRowIndex = currentRowIndex;
                IReadOnlyList<OrderedMonthDetailRow> orderedDetailRows = OrderMonthDetailRowsByTemplate(
                    systemGroup.DetailRows,
                    orderedSystemGroup.TemplateEntry);
                uint groupEndRowIndex = groupStartRowIndex + 1U + (uint)(orderedDetailRows.Count * 2);
                int sequenceNumber = orderedSystemGroup.TemplateEntry?.SequenceNumber ?? nextAppendedSequenceNumber++;
                string systemName = orderedSystemGroup.TemplateEntry?.SystemName ?? systemGroup.SystemName;
                string inventoryNumber = string.IsNullOrWhiteSpace(orderedSystemGroup.TemplateEntry?.InventoryNumber)
                    ? systemGroup.InventoryNumber
                    : orderedSystemGroup.TemplateEntry.InventoryNumber;

                Row headerTopRow = CloneRowToIndex(systemHeaderTopTemplate, currentRowIndex);
                Row headerBottomRow = CloneRowToIndex(systemHeaderBottomTemplate, currentRowIndex + 1);
                PopulateSystemHeaderRows(headerTopRow, headerBottomRow, sequenceNumber, systemName, inventoryNumber);
                targetSheetData.Append(headerTopRow, headerBottomRow);

                AddMerge(mergeCells, 1, 1, groupStartRowIndex, groupEndRowIndex);
                AddMerge(mergeCells, 2, 2, currentRowIndex, currentRowIndex + 1);
                AddMerge(mergeCells, 3, 3, currentRowIndex, currentRowIndex + 1);
                AddMerge(mergeCells, 4, 4, currentRowIndex, currentRowIndex + 1);
                AddMerge(mergeCells, NotesColumnIndex, NotesColumnIndex, currentRowIndex, currentRowIndex + 1);
                AddMerge(mergeCells, HiddenMergeColumnIndex, HiddenMergeColumnIndex, currentRowIndex, currentRowIndex + 1);

                currentRowIndex += 2;
                foreach (OrderedMonthDetailRow orderedDetailRow in orderedDetailRows)
                {
                    KbMaintenanceMonthSheetDetailRow detailRow = orderedDetailRow.Row;
                    string detailName = orderedDetailRow.TemplateEntry?.NodeName ?? detailRow.NodeName;
                    Row planRow = CloneRowToIndex(detailPlanTemplate, currentRowIndex);
                    Row factRow = CloneRowToIndex(detailFactTemplate, currentRowIndex + 1);
                    PopulateDetailRows(planRow, factRow, detailRow, detailName);
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

        private static byte[] PruneWorkbookToSingleMonth(byte[] packageBytes, int month)
        {
            using var workbookStream = CreateExpandableMemoryStream(packageBytes);
            using (SpreadsheetDocument workbookDocument = SpreadsheetDocument.Open(workbookStream, true))
            {
                WorkbookPart workbookPart = workbookDocument.WorkbookPart
                    ?? throw new InvalidOperationException("Книга графика ТО повреждена: отсутствует workbook part.");
                Sheets sheets = workbookPart.Workbook.Sheets
                    ?? throw new InvalidOperationException("Книга графика ТО повреждена: отсутствует список листов.");
                Sheet targetSheet = FindMonthSheet(workbookPart, month);
                List<Sheet> orderedSheets = sheets.Elements<Sheet>().ToList();
                int targetLocalSheetId = orderedSheets.FindIndex(sheet =>
                    string.Equals(sheet.Id?.Value, targetSheet.Id?.Value, StringComparison.Ordinal));
                if (targetLocalSheetId < 0)
                    throw new InvalidOperationException("Выбранный лист графика ТО не найден в книге.");

                foreach (Sheet sheet in orderedSheets)
                {
                    if (string.Equals(sheet.Id?.Value, targetSheet.Id?.Value, StringComparison.Ordinal))
                        continue;

                    if (!string.IsNullOrWhiteSpace(sheet.Id?.Value) &&
                        workbookPart.GetPartById(sheet.Id!.Value!) is WorksheetPart worksheetPart)
                    {
                        workbookPart.DeletePart(worksheetPart);
                    }

                    sheet.Remove();
                }

                targetSheet.SheetId = 1U;
                PruneDefinedNamesToSingleSheet(workbookPart, targetLocalSheetId);
                ResetWorkbookViewToFirstSheet(workbookPart);
                workbookPart.Workbook.Save();
            }

            return workbookStream.ToArray();
        }

        private byte[] BuildNormalizedYearRecalculationWorkbook(byte[] existingWorkbookPackage, int startMonth)
        {
            byte[] templateBytes = _templateService.GetTemplatePackage();
            using var existingStream = new MemoryStream(existingWorkbookPackage, writable: false);
            using var targetStream = CreateExpandableMemoryStream(templateBytes);
            using (SpreadsheetDocument existingDocument = SpreadsheetDocument.Open(existingStream, false))
            using (SpreadsheetDocument targetDocument = SpreadsheetDocument.Open(targetStream, true))
            {
                WorkbookPart existingWorkbookPart = existingDocument.WorkbookPart
                    ?? throw new InvalidOperationException("Книга графика ТО повреждена: отсутствует workbook part.");
                WorkbookPart targetWorkbookPart = targetDocument.WorkbookPart
                    ?? throw new InvalidOperationException("Встроенный шаблон графика ТО повреждён: отсутствует workbook part.");

                for (int month = 1; month < startMonth; month++)
                {
                    if (!TryFindMonthSheet(existingWorkbookPart, month, out Sheet? sourceSheet) || sourceSheet == null)
                        continue;

                    WorksheetPart sourceWorksheetPart = GetWorksheetPart(existingWorkbookPart, sourceSheet);
                    if (!IsUsableMonthSheet(sourceWorksheetPart))
                        continue;

                    Sheet targetSheet = FindMonthSheet(targetWorkbookPart, month);
                    WorksheetPart targetWorksheetPart = GetWorksheetPart(targetWorkbookPart, targetSheet);
                    targetWorksheetPart.Worksheet = (Worksheet)sourceWorksheetPart.Worksheet.CloneNode(true);
                    targetWorksheetPart.Worksheet.Save();
                }

                ResetWorkbookCalculationChain(targetWorkbookPart);
                targetWorkbookPart.Workbook.Save();
            }

            return targetStream.ToArray();
        }

        private static void RewriteAnnualSheet(
            WorkbookPart workbookPart,
            Sheet targetSheet,
            WorksheetPart targetWorksheetPart,
            AnnualSheetLayout layout,
            KbMaintenanceAnnualWorkbookModel workbookModel)
        {
            Worksheet targetWorksheet = targetWorksheetPart.Worksheet;
            SheetData targetSheetData = targetWorksheet.GetFirstChild<SheetData>()
                ?? throw new InvalidOperationException("Лист годового графика ТО повреждён: отсутствует sheetData.");

            Row systemTemplate = CloneRow(FindRequiredRow(targetSheetData, layout.FirstSystemRowIndex));
            Row detailTemplate = CloneRow(FindRequiredRow(targetSheetData, layout.FirstDetailRowIndex));
            IReadOnlyList<Row> footerTemplates = targetSheetData.Elements<Row>()
                .Where(row => (row.RowIndex?.Value ?? 0) >= layout.FooterStartRowIndex)
                .Select(CloneRow)
                .ToArray();
            IReadOnlyList<string> footerMerges = ReadMergedRanges(targetWorksheet)
                .Where(range => RangeIntersectsRows(range, layout.FooterStartRowIndex, layout.LastUsedRowIndex))
                .ToArray();
            IReadOnlyList<OrderedAnnualSystemGroup> orderedSystemGroups = OrderAnnualSystemGroupsByTemplate(
                workbookModel.SystemGroups,
                targetWorksheetPart,
                layout,
                out int nextAppendedSequenceNumber);

            RemoveRows(targetSheetData, layout.DataStartRowIndex, layout.LastUsedRowIndex);
            MergeCells mergeCells = GetOrCreateMergeCells(targetWorksheet);
            ClearMergedRanges(mergeCells, layout.DataStartRowIndex, layout.LastUsedRowIndex);
            ClearRowBreaks(targetWorksheet);

            WriteAnnualHeader(targetWorksheet, workbookModel);

            uint currentRowIndex = layout.DataStartRowIndex;
            foreach (OrderedAnnualSystemGroup orderedSystemGroup in orderedSystemGroups)
            {
                KbMaintenanceAnnualSystemGroup systemGroup = orderedSystemGroup.Group;
                uint groupStartRowIndex = currentRowIndex;
                int sequenceNumber = orderedSystemGroup.TemplateEntry?.SequenceNumber ?? nextAppendedSequenceNumber++;
                string systemName = orderedSystemGroup.TemplateEntry?.SystemName ?? systemGroup.SystemName;
                string inventoryNumber = string.IsNullOrWhiteSpace(orderedSystemGroup.TemplateEntry?.InventoryNumber)
                    ? systemGroup.InventoryNumber
                    : orderedSystemGroup.TemplateEntry.InventoryNumber;
                Row systemRow = CloneRowToIndex(systemTemplate, currentRowIndex);
                PopulateAnnualSystemRow(systemRow, workbookModel.WorkshopName, sequenceNumber, systemName, inventoryNumber);
                targetSheetData.Append(systemRow);
                currentRowIndex++;

                IReadOnlyList<OrderedAnnualDetailRow> orderedDetailRows = OrderAnnualDetailRowsByTemplate(
                    systemGroup.DetailRows,
                    orderedSystemGroup.TemplateEntry);
                foreach (OrderedAnnualDetailRow orderedDetailRow in orderedDetailRows)
                {
                    KbMaintenanceAnnualDetailRow detailRow = orderedDetailRow.Row;
                    string detailName = orderedDetailRow.TemplateEntry?.NodeName ?? detailRow.NodeName;
                    string detailInventoryNumber = string.IsNullOrWhiteSpace(orderedDetailRow.TemplateEntry?.InventoryNumber)
                        ? detailRow.InventoryNumber
                        : orderedDetailRow.TemplateEntry.InventoryNumber;
                    Row row = CloneRowToIndex(detailTemplate, currentRowIndex);
                    PopulateAnnualDetailRow(row, detailRow, detailName, detailInventoryNumber, layout.PlanColumnByMonth);
                    targetSheetData.Append(row);
                    currentRowIndex++;
                }

                AddMerge(mergeCells, 1, 1, groupStartRowIndex, currentRowIndex - 1);
            }

            uint footerStartRowIndex = currentRowIndex;
            int footerRowDelta = (int)footerStartRowIndex - (int)layout.FooterStartRowIndex;
            foreach (Row footerTemplate in footerTemplates)
            {
                uint oldRowIndex = footerTemplate.RowIndex?.Value ?? layout.FooterStartRowIndex;
                Row footerRow = CloneRowToIndex(footerTemplate, (uint)((int)oldRowIndex + footerRowDelta));
                targetSheetData.Append(footerRow);
            }

            foreach (string mergeRange in footerMerges)
            {
                AddShiftedMerge(mergeCells, mergeRange, footerRowDelta);
            }

            uint totalsRowIndex = footerStartRowIndex;
            PopulateAnnualTotalsRow(targetWorksheet, totalsRowIndex, workbookModel);
            uint finalRowIndex = footerTemplates.Count == 0
                ? totalsRowIndex
                : footerTemplates.Max(row => (uint)((int)(row.RowIndex?.Value ?? layout.FooterStartRowIndex) + footerRowDelta));

            UpdateWorksheetDimension(targetWorksheet, finalRowIndex);
            UpdateAnnualDefinedRanges(workbookPart, targetSheet, layout, finalRowIndex);
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

        private static void WriteAnnualHeader(Worksheet worksheet, KbMaintenanceAnnualWorkbookModel workbookModel)
        {
            SetSheetCellText(worksheet, 10, 1, $"на {workbookModel.Year} год");
            int approvalYear = Math.Max(1, workbookModel.Year - 1);
            SetSheetCellText(worksheet, 7, 21, $"____ ____________ {approvalYear} года");
        }

        private static void NormalizeMonthSheetHeaderRows(Worksheet worksheet, SheetLayout layout)
        {
            for (uint rowIndex = 1; rowIndex <= layout.HeaderBottomRowIndex; rowIndex++)
            {
                UnhideRow(worksheet, rowIndex);
            }
        }

        private static void ApplyHeaderDayCalendarStyles(
            WorkbookPart workbookPart,
            Worksheet worksheet,
            SheetLayout layout,
            KbMaintenanceMonthSheetModel sheetModel)
        {
            int daysInMonth = DateTime.DaysInMonth(sheetModel.Year, sheetModel.Month);
            var nonWorkingDays = sheetModel.NonWorkingDayNumbers
                .Where(day => day >= 1 && day <= daysInMonth)
                .ToHashSet();
            if (nonWorkingDays.Count == 0)
                return;

            Row headerRow = GetOrCreateRow(worksheet, layout.HeaderBottomRowIndex);
            if (!TryResolveHeaderCalendarStyles(workbookPart, headerRow, daysInMonth, out uint workingStyleIndex, out uint nonWorkingStyleIndex))
                return;

            for (int dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth++)
            {
                int columnIndex = FirstDayColumnIndex + dayOfMonth - 1;
                Cell cell = GetOrCreateCell(headerRow, columnIndex);
                cell.StyleIndex = nonWorkingDays.Contains(dayOfMonth)
                    ? nonWorkingStyleIndex
                    : workingStyleIndex;
            }
        }

        private static IReadOnlyList<OrderedMonthSystemGroup> OrderSystemGroupsByTemplate(
            IReadOnlyList<KbMaintenanceMonthSheetSystemGroup> groups,
            WorksheetPart templateWorksheetPart,
            out int nextAppendedSequenceNumber)
        {
            if (groups.Count == 0)
            {
                nextAppendedSequenceNumber = 1;
                return Array.Empty<OrderedMonthSystemGroup>();
            }

            IReadOnlyList<TemplateSystemOrderEntry> templateOrder = ReadMonthTemplateSystemOrder(templateWorksheetPart);
            nextAppendedSequenceNumber = GetNextAppendedSequenceNumber(templateOrder);
            return groups
                .Select((group, index) =>
                {
                    TemplateSystemOrderEntry? templateEntry = ResolveTemplateSystemOrderEntry(
                        templateOrder,
                        group.SystemName,
                        group.InventoryNumber);
                    return new
                    {
                        Group = group,
                        OriginalIndex = index,
                        TemplateEntry = templateEntry,
                        Rank = templateEntry?.Rank ?? int.MaxValue
                    };
                })
                .OrderBy(static item => item.Rank)
                .ThenBy(static item => item.OriginalIndex)
                .Select(static item => new OrderedMonthSystemGroup(item.Group, item.TemplateEntry))
                .ToList();
        }

        private static IReadOnlyList<OrderedAnnualSystemGroup> OrderAnnualSystemGroupsByTemplate(
            IReadOnlyList<KbMaintenanceAnnualSystemGroup> groups,
            WorksheetPart templateWorksheetPart,
            AnnualSheetLayout templateLayout,
            out int nextAppendedSequenceNumber)
        {
            if (groups.Count == 0)
            {
                nextAppendedSequenceNumber = 1;
                return Array.Empty<OrderedAnnualSystemGroup>();
            }

            IReadOnlyList<TemplateSystemOrderEntry> templateOrder = ReadTemplateSystemOrder(
                templateWorksheetPart,
                templateLayout.DataStartRowIndex,
                templateLayout.FooterStartRowIndex - 1);
            nextAppendedSequenceNumber = GetNextAppendedSequenceNumber(templateOrder);
            return groups
                .Select((group, index) =>
                {
                    TemplateSystemOrderEntry? templateEntry = ResolveTemplateSystemOrderEntry(
                        templateOrder,
                        group.SystemName,
                        group.InventoryNumber);
                    return new
                    {
                        Group = group,
                        OriginalIndex = index,
                        TemplateEntry = templateEntry,
                        Rank = templateEntry?.Rank ?? int.MaxValue
                    };
                })
                .OrderBy(static item => item.Rank)
                .ThenBy(static item => item.OriginalIndex)
                .Select(static item => new OrderedAnnualSystemGroup(item.Group, item.TemplateEntry))
                .ToList();
        }

        private static IReadOnlyList<OrderedMonthDetailRow> OrderMonthDetailRowsByTemplate(
            IReadOnlyList<KbMaintenanceMonthSheetDetailRow> detailRows,
            TemplateSystemOrderEntry? templateEntry)
        {
            if (detailRows.Count == 0)
                return Array.Empty<OrderedMonthDetailRow>();

            IReadOnlyList<TemplateDetailOrderEntry> templateDetails = templateEntry?.DetailRows ?? Array.Empty<TemplateDetailOrderEntry>();
            return detailRows
                .Select((row, index) =>
                {
                    TemplateDetailOrderEntry? detailEntry = ResolveTemplateDetailOrderEntry(
                        templateDetails,
                        row.NodeName,
                        string.Empty);
                    return new
                    {
                        Row = row,
                        OriginalIndex = index,
                        TemplateEntry = detailEntry,
                        Rank = detailEntry?.Rank ?? int.MaxValue
                    };
                })
                .OrderBy(static item => item.Rank)
                .ThenBy(static item => item.OriginalIndex)
                .Select(static item => new OrderedMonthDetailRow(item.Row, item.TemplateEntry))
                .ToList();
        }

        private static IReadOnlyList<OrderedAnnualDetailRow> OrderAnnualDetailRowsByTemplate(
            IReadOnlyList<KbMaintenanceAnnualDetailRow> detailRows,
            TemplateSystemOrderEntry? templateEntry)
        {
            if (detailRows.Count == 0)
                return Array.Empty<OrderedAnnualDetailRow>();

            IReadOnlyList<TemplateDetailOrderEntry> templateDetails = templateEntry?.DetailRows ?? Array.Empty<TemplateDetailOrderEntry>();
            return detailRows
                .Select((row, index) =>
                {
                    TemplateDetailOrderEntry? detailEntry = ResolveTemplateDetailOrderEntry(
                        templateDetails,
                        row.NodeName,
                        row.InventoryNumber);
                    return new
                    {
                        Row = row,
                        OriginalIndex = index,
                        TemplateEntry = detailEntry,
                        Rank = detailEntry?.Rank ?? int.MaxValue
                    };
                })
                .OrderBy(static item => item.Rank)
                .ThenBy(static item => item.OriginalIndex)
                .Select(static item => new OrderedAnnualDetailRow(item.Row, item.TemplateEntry))
                .ToList();
        }

        private static void PopulateSystemHeaderRows(
            Row headerTopRow,
            Row headerBottomRow,
            int sequenceNumber,
            string systemName,
            string inventoryNumber)
        {
            SetCellNumber(headerTopRow, 1, sequenceNumber);
            SetCellText(headerTopRow, 2, NormalizeText(systemName));
            SetCellText(headerTopRow, 3, DefaultDashText);
            SetCellText(headerTopRow, 4, NormalizeText(inventoryNumber, DefaultDashText));
            ClearRowValues(headerTopRow, startColumnIndex: 5, endColumnIndex: SheetColumnSpanEndIndex);
            ClearRowValues(headerBottomRow, startColumnIndex: 1, endColumnIndex: SheetColumnSpanEndIndex);
            SetMergedRowBlockHeight(headerTopRow, headerBottomRow, systemName, MonthlySystemNameCharactersPerLine);
        }

        private static void PopulateDetailRows(
            Row planRow,
            Row factRow,
            KbMaintenanceMonthSheetDetailRow detailRow,
            string detailName)
        {
            SetCellText(planRow, 2, NormalizeText(detailName));
            SetCellText(planRow, 3, DefaultDashText);
            SetCellText(planRow, 4, DefaultDashText);
            SetCellText(planRow, 5, PlanText);
            ClearRowValues(planRow, FirstDayColumnIndex, SheetColumnSpanEndIndex);
            NormalizeMonthlyDayCellStyles(planRow);

            int maxPlanCellLineCount = 1;
            foreach (KbMaintenanceMonthSheetDayCell dayCell in detailRow.DayCells)
            {
                if (dayCell.DayOfMonth is < 1 or > 31)
                    continue;

                int dayColumnIndex = FirstDayColumnIndex + dayCell.DayOfMonth - 1;
                string planCellText = BuildPlanCellText(dayCell.WorkEntries);
                maxPlanCellLineCount = Math.Max(
                    maxPlanCellLineCount,
                    EstimateWrappedLineCount(planCellText, MonthlyPlanCellCharactersPerLine));
                SetCellText(planRow, dayColumnIndex, planCellText);
            }

            SetCellNumber(planRow, TotalHoursColumnIndex, detailRow.TotalHours);

            ClearRowValues(factRow, 1, SheetColumnSpanEndIndex);
            NormalizeMonthlyDayCellStyles(factRow);
            SetCellText(factRow, 5, FactText);
            SetMonthlyDetailRowHeights(planRow, factRow, detailName, maxPlanCellLineCount);
        }

        private static void PopulateAnnualSystemRow(
            Row row,
            string workshopName,
            int sequenceNumber,
            string systemName,
            string inventoryNumber)
        {
            ClearRowValues(row, 1, AnnualSheetColumnSpanEndIndex);
            SetCellNumber(row, 1, sequenceNumber);
            SetCellText(row, 2, NormalizeText(systemName, string.Empty));
            SetCellText(row, 3, NormalizeText(workshopName, string.Empty));
            SetCellText(row, 4, NormalizeText(inventoryNumber, string.Empty));
            SetRowHeightAtLeast(row, CalculateTextRowHeight(systemName, AnnualNameCharactersPerLine));
        }

        private static void PopulateAnnualDetailRow(
            Row row,
            KbMaintenanceAnnualDetailRow detailRow,
            string detailName,
            string inventoryNumber,
            IReadOnlyDictionary<int, int> planColumnByMonth)
        {
            ClearRowValues(row, 1, AnnualSheetColumnSpanEndIndex);
            SetCellText(row, 2, NormalizeText(detailName, string.Empty));
            SetCellText(row, 4, NormalizeText(inventoryNumber, string.Empty));

            int maxLineCount = EstimateWrappedLineCount(detailName, AnnualNameCharactersPerLine);
            foreach (KbMaintenanceAnnualMonthCell monthCell in detailRow.MonthCells)
            {
                if (!planColumnByMonth.TryGetValue(monthCell.Month, out int planColumnIndex))
                    continue;

                maxLineCount = Math.Max(
                    maxLineCount,
                    EstimateWrappedLineCount(monthCell.PlanText, AnnualPlanCellCharactersPerLine));
                SetCellText(row, planColumnIndex, monthCell.PlanText);
                SetCellNumber(row, planColumnIndex + 1, monthCell.Hours);
            }

            SetCellNumber(row, AnnualTotalHoursColumnIndex, detailRow.TotalHours);
            SetRowHeightAtLeast(row, CalculateTextRowHeight(maxLineCount));
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

            Row totalsRow = GetOrCreateRow(worksheet, totalsRowIndex);
            ClearRowValues(totalsRow, FirstDayColumnIndex, TotalHoursColumnIndex - 1);
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

        private static void PopulateAnnualTotalsRow(
            Worksheet worksheet,
            uint totalsRowIndex,
            KbMaintenanceAnnualWorkbookModel workbookModel)
        {
            Row totalsRow = GetOrCreateRow(worksheet, totalsRowIndex);
            ClearRowValues(totalsRow, 1, AnnualSheetColumnSpanEndIndex);
            SetCellText(totalsRow, 2, AnnualTotalsLabelText);
            SetCellNumber(totalsRow, AnnualTotalHoursColumnIndex, workbookModel.TotalHours);
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

        private static void UpdateAnnualDefinedRanges(
            WorkbookPart workbookPart,
            Sheet targetSheet,
            AnnualSheetLayout targetLayout,
            uint finalRowIndex)
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
                "_xlnm.Print_Titles",
                startRow: targetLayout.TitleStartRowIndex,
                endRow: targetLayout.TitleEndRowIndex,
                fallbackEndColumn: null);
            UpdateDefinedNameRange(
                definedNames,
                targetSheet.Name?.Value ?? string.Empty,
                localSheetId,
                "_xlnm.Print_Area",
                startRow: 1,
                endRow: finalRowIndex + 1,
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

        private static void PruneDefinedNamesToSingleSheet(WorkbookPart workbookPart, int originalLocalSheetId)
        {
            DefinedNames? definedNames = workbookPart.Workbook.DefinedNames;
            if (definedNames == null)
                return;

            foreach (DefinedName definedName in definedNames.Elements<DefinedName>().ToList())
            {
                if (definedName.LocalSheetId == null)
                    continue;

                if (definedName.LocalSheetId.Value == (uint)originalLocalSheetId)
                {
                    definedName.LocalSheetId = 0U;
                    continue;
                }

                definedName.Remove();
            }

            if (!definedNames.Elements<DefinedName>().Any())
                definedNames.Remove();
        }

        private static void ResetWorkbookViewToFirstSheet(WorkbookPart workbookPart)
        {
            BookViews? bookViews = workbookPart.Workbook.BookViews;
            if (bookViews == null)
                return;

            foreach (WorkbookView workbookView in bookViews.Elements<WorkbookView>())
            {
                workbookView.ActiveTab = 0U;
                workbookView.FirstSheet = 0U;
            }
        }

        private static void ResetWorksheetView(Worksheet worksheet, uint firstDataRowIndex)
        {
            SheetViews? sheetViews = worksheet.GetFirstChild<SheetViews>();
            SheetView? sheetView = sheetViews?.Elements<SheetView>().FirstOrDefault();
            if (sheetView == null)
                return;

            foreach (Pane pane in sheetView.Elements<Pane>().ToList())
            {
                pane.Remove();
            }

            foreach (Selection selection in sheetView.Elements<Selection>().ToList())
            {
                selection.Remove();
            }

            sheetView.Append(
                new Selection
                {
                    ActiveCell = $"E{firstDataRowIndex}",
                    SequenceOfReferences = new ListValue<StringValue> { InnerText = $"E{firstDataRowIndex}" }
                });
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

        private static void SetMergedRowBlockHeight(
            Row topRow,
            Row bottomRow,
            string text,
            int charactersPerLine)
        {
            double blockHeight = Math.Max(
                DefaultGeneratedRowHeight * 2,
                CalculateTextRowHeight(text, charactersPerLine));
            double rowHeight = RoundUpToQuarterPoint(blockHeight / 2);

            SetRowHeightAtLeast(topRow, rowHeight);
            SetRowHeightAtLeast(bottomRow, rowHeight);
        }

        private static void SetMonthlyDetailRowHeights(
            Row planRow,
            Row factRow,
            string detailName,
            int maxPlanCellLineCount)
        {
            double detailBlockHeight = Math.Max(
                DefaultGeneratedRowHeight * 2,
                CalculateTextRowHeight(detailName, MonthlyDetailNameCharactersPerLine));
            double mergedTextRowHeight = RoundUpToQuarterPoint(detailBlockHeight / 2);
            double planCellHeight = CalculateTextRowHeight(maxPlanCellLineCount);

            SetRowHeightAtLeast(planRow, Math.Max(mergedTextRowHeight, planCellHeight));
            SetRowHeightAtLeast(factRow, mergedTextRowHeight);
        }

        private static double CalculateTextRowHeight(string? text, int charactersPerLine) =>
            CalculateTextRowHeight(EstimateWrappedLineCount(text, charactersPerLine));

        private static double CalculateTextRowHeight(int lineCount) =>
            RoundUpToQuarterPoint(Math.Max(DefaultGeneratedRowHeight, (lineCount * TextLineHeight) + TextRowHeightPadding));

        private static int EstimateWrappedLineCount(string? text, int charactersPerLine)
        {
            if (string.IsNullOrWhiteSpace(text) || charactersPerLine <= 0)
                return 1;

            int lineCount = 0;
            foreach (string paragraph in text.Replace("\r", string.Empty).Split('\n'))
            {
                string normalizedParagraph = paragraph.Trim();
                lineCount += Math.Max(
                    1,
                    (int)Math.Ceiling(normalizedParagraph.Length / (double)charactersPerLine));
            }

            return Math.Max(1, lineCount);
        }

        private static void SetRowHeightAtLeast(Row row, double minimumHeight)
        {
            double existingHeight = row.Height?.Value ?? DefaultGeneratedRowHeight;
            row.Height = Math.Max(existingHeight, minimumHeight);
            row.CustomHeight = true;
        }

        private static double RoundUpToQuarterPoint(double value) =>
            Math.Ceiling(value * 4d) / 4d;

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

        private static void UnhideRow(Worksheet worksheet, uint rowIndex)
        {
            SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
            Row? row = sheetData?.Elements<Row>().FirstOrDefault(candidate => candidate.RowIndex?.Value == rowIndex);
            if (row == null)
                return;

            row.Hidden = null;
        }

        private static bool TryResolveHeaderCalendarStyles(
            WorkbookPart workbookPart,
            Row headerRow,
            int daysInMonth,
            out uint workingStyleIndex,
            out uint nonWorkingStyleIndex)
        {
            workingStyleIndex = 0;
            nonWorkingStyleIndex = 0;

            var styleCounts = new Dictionary<uint, int>();
            var fillByStyle = new Dictionary<uint, uint>();
            for (int dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth++)
            {
                int columnIndex = FirstDayColumnIndex + dayOfMonth - 1;
                Cell cell = GetOrCreateCell(headerRow, columnIndex);
                uint styleIndex = cell.StyleIndex?.Value ?? 0;
                uint? fillId = TryGetFillId(workbookPart, styleIndex);
                if (fillId == null)
                    continue;

                styleCounts[styleIndex] = styleCounts.TryGetValue(styleIndex, out int count) ? count + 1 : 1;
                fillByStyle[styleIndex] = fillId.Value;
            }

            if (styleCounts.Count == 0)
                return false;

            workingStyleIndex = styleCounts
                .Where(pair => fillByStyle.TryGetValue(pair.Key, out uint fillId) && fillId == 0)
                .DefaultIfEmpty(styleCounts.OrderByDescending(static pair => pair.Value).First())
                .OrderByDescending(static pair => pair.Value)
                .ThenBy(static pair => pair.Key)
                .First()
                .Key;

            uint resolvedWorkingStyleIndex = workingStyleIndex;
            nonWorkingStyleIndex = styleCounts
                .Where(pair => pair.Key != resolvedWorkingStyleIndex &&
                               fillByStyle.TryGetValue(pair.Key, out uint fillId) &&
                               fillId != 0)
                .OrderByDescending(static pair => pair.Value)
                .ThenBy(static pair => pair.Key)
                .Select(static pair => pair.Key)
                .FirstOrDefault(workingStyleIndex);

            return nonWorkingStyleIndex != workingStyleIndex;
        }

        private static uint? TryGetFillId(WorkbookPart workbookPart, uint styleIndex)
        {
            CellFormats? cellFormats = workbookPart.WorkbookStylesPart?.Stylesheet.CellFormats;
            if (cellFormats == null || styleIndex >= cellFormats.ChildElements.Count)
                return null;

            return ((CellFormat)cellFormats.ElementAt((int)styleIndex)).FillId?.Value;
        }

        private static IReadOnlyList<TemplateSystemOrderEntry> ReadTemplateSystemOrder(
            WorksheetPart worksheetPart,
            uint startRowIndex,
            uint endRowIndex)
        {
            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()
                ?? throw new InvalidOperationException("Шаблон графика ТО повреждён: отсутствует sheetData.");
            IReadOnlyList<string> sharedStrings = ReadSharedStrings(
                worksheetPart.GetParentParts().OfType<WorkbookPart>().FirstOrDefault()?.SharedStringTablePart);
            var order = new List<TemplateSystemOrderEntryBuilder>();
            TemplateSystemOrderEntryBuilder? currentSystem = null;
            foreach (Row row in sheetData.Elements<Row>())
            {
                uint rowIndex = row.RowIndex?.Value ?? 0;
                if (rowIndex < startRowIndex || rowIndex > endRowIndex)
                    continue;

                string sequenceText = ReadCellText(row, 1, sharedStrings);
                string systemName = ReadCellText(row, 2, sharedStrings);
                string inventoryNumber = ReadCellText(row, 4, sharedStrings);
                if (int.TryParse(sequenceText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sequenceNumber) &&
                    !string.IsNullOrWhiteSpace(systemName))
                {
                    if (IsTemplateColumnNumberRow(sequenceNumber, systemName, inventoryNumber))
                    {
                        currentSystem = null;
                        continue;
                    }

                    currentSystem = new TemplateSystemOrderEntryBuilder(
                        order.Count,
                        sequenceNumber,
                        systemName.Trim(),
                        inventoryNumber.Trim(),
                        BuildTemplateSystemOrderKey(systemName),
                        BuildTemplateInventoryNumberKey(inventoryNumber),
                        BuildTemplateNameMatchKey(systemName));
                    order.Add(currentSystem);
                    continue;
                }

                if (currentSystem == null || !IsTemplateDetailRowName(systemName))
                    continue;

                currentSystem.DetailRows.Add(
                    new TemplateDetailOrderEntry(
                        Rank: currentSystem.DetailRows.Count,
                        NodeName: systemName.Trim(),
                        InventoryNumber: inventoryNumber.Trim(),
                        NodeNameKey: BuildTemplateSystemOrderKey(systemName),
                        InventoryNumberKey: BuildTemplateInventoryNumberKey(inventoryNumber),
                        NodeNameMatchKey: BuildTemplateNameMatchKey(systemName)));
            }

            return order.Select(static entry => entry.Build()).ToList();
        }

        private static IReadOnlyList<TemplateSystemOrderEntry> ReadMonthTemplateSystemOrder(WorksheetPart templateWorksheetPart)
        {
            WorkbookPart templateWorkbookPart = templateWorksheetPart.GetParentParts().OfType<WorkbookPart>().FirstOrDefault()
                ?? throw new InvalidOperationException("Шаблон графика ТО повреждён: отсутствует workbook part.");
            Sheet maySheet = FindMonthSheet(templateWorkbookPart, 5);
            WorksheetPart mayWorksheetPart = GetWorksheetPart(templateWorkbookPart, maySheet);
            SheetLayout mayLayout = SheetLayout.Read(mayWorksheetPart, requireDetailTemplate: false);
            return ReadTemplateSystemOrder(
                mayWorksheetPart,
                mayLayout.DataStartRowIndex,
                mayLayout.FooterStartRowIndex - 1);
        }

        private static TemplateSystemOrderEntry? ResolveTemplateSystemOrderEntry(
            IReadOnlyList<TemplateSystemOrderEntry> templateOrder,
            string systemName,
            string inventoryNumber)
        {
            string systemNameKey = BuildTemplateSystemOrderKey(systemName);
            string inventoryNumberKey = BuildTemplateInventoryNumberKey(inventoryNumber);

            if (!string.IsNullOrWhiteSpace(systemNameKey) && !string.IsNullOrWhiteSpace(inventoryNumberKey))
            {
                TemplateSystemOrderEntry? exactEntry = templateOrder.FirstOrDefault(entry =>
                    string.Equals(entry.SystemNameKey, systemNameKey, StringComparison.Ordinal) &&
                    string.Equals(entry.InventoryNumberKey, inventoryNumberKey, StringComparison.Ordinal));
                if (exactEntry != null)
                    return exactEntry;
            }

            if (!string.IsNullOrWhiteSpace(inventoryNumberKey))
            {
                List<TemplateSystemOrderEntry> inventoryMatches = templateOrder
                    .Where(entry => string.Equals(entry.InventoryNumberKey, inventoryNumberKey, StringComparison.Ordinal))
                    .Take(2)
                    .ToList();
                if (inventoryMatches.Count == 1)
                    return inventoryMatches[0];
            }

            if (!string.IsNullOrWhiteSpace(systemNameKey))
            {
                TemplateSystemOrderEntry? nameMatch = templateOrder.FirstOrDefault(entry =>
                    string.Equals(entry.SystemNameKey, systemNameKey, StringComparison.Ordinal));
                if (nameMatch != null)
                    return nameMatch;
            }

            return ResolveTemplateSystemNameMatch(templateOrder, systemName);
        }

        private static TemplateDetailOrderEntry? ResolveTemplateDetailOrderEntry(
            IReadOnlyList<TemplateDetailOrderEntry> templateDetails,
            string nodeName,
            string inventoryNumber)
        {
            if (templateDetails.Count == 0)
                return null;

            string nodeNameKey = BuildTemplateSystemOrderKey(nodeName);
            string inventoryNumberKey = BuildTemplateInventoryNumberKey(inventoryNumber);
            if (!string.IsNullOrWhiteSpace(nodeNameKey) && !string.IsNullOrWhiteSpace(inventoryNumberKey))
            {
                TemplateDetailOrderEntry? exactEntry = templateDetails.FirstOrDefault(entry =>
                    string.Equals(entry.NodeNameKey, nodeNameKey, StringComparison.Ordinal) &&
                    string.Equals(entry.InventoryNumberKey, inventoryNumberKey, StringComparison.Ordinal));
                if (exactEntry != null)
                    return exactEntry;
            }

            if (!string.IsNullOrWhiteSpace(inventoryNumberKey))
            {
                List<TemplateDetailOrderEntry> inventoryMatches = templateDetails
                    .Where(entry => string.Equals(entry.InventoryNumberKey, inventoryNumberKey, StringComparison.Ordinal))
                    .Take(2)
                    .ToList();
                if (inventoryMatches.Count == 1)
                    return inventoryMatches[0];
            }

            if (!string.IsNullOrWhiteSpace(nodeNameKey))
            {
                TemplateDetailOrderEntry? nameMatch = templateDetails.FirstOrDefault(entry =>
                    string.Equals(entry.NodeNameKey, nodeNameKey, StringComparison.Ordinal));
                if (nameMatch != null)
                    return nameMatch;
            }

            return ResolveTemplateDetailNameMatch(templateDetails, nodeName);
        }

        private static int GetNextAppendedSequenceNumber(IReadOnlyList<TemplateSystemOrderEntry> templateOrder) =>
            templateOrder.Count == 0
                ? 1
                : templateOrder.Max(static entry => entry.SequenceNumber) + 1;

        private static TemplateSystemOrderEntry? ResolveTemplateSystemNameMatch(
            IReadOnlyList<TemplateSystemOrderEntry> templateOrder,
            string systemName)
        {
            string systemNameMatchKey = BuildTemplateNameMatchKey(systemName);
            if (string.IsNullOrWhiteSpace(systemNameMatchKey))
                return null;

            TemplateSystemOrderEntry? exactMatch = FindUnique(
                templateOrder.Where(entry => string.Equals(entry.SystemNameMatchKey, systemNameMatchKey, StringComparison.Ordinal)));
            if (exactMatch != null)
                return exactMatch;

            return templateOrder
                .Select(entry => new
                {
                    Entry = entry,
                    Score = CalculateTemplateNameMatchScore(entry.SystemName, entry.SystemNameMatchKey, systemName, systemNameMatchKey)
                })
                .Where(static item => item.Score > 0)
                .OrderByDescending(static item => item.Score)
                .ThenBy(static item => item.Entry.Rank)
                .Select(static item => item.Entry)
                .FirstOrDefault();
        }

        private static TemplateDetailOrderEntry? ResolveTemplateDetailNameMatch(
            IReadOnlyList<TemplateDetailOrderEntry> templateDetails,
            string nodeName)
        {
            string nodeNameMatchKey = BuildTemplateNameMatchKey(nodeName);
            if (string.IsNullOrWhiteSpace(nodeNameMatchKey))
                return null;

            TemplateDetailOrderEntry? exactMatch = FindUnique(
                templateDetails.Where(entry => string.Equals(entry.NodeNameMatchKey, nodeNameMatchKey, StringComparison.Ordinal)));
            if (exactMatch != null)
                return exactMatch;

            return templateDetails
                .Select(entry => new
                {
                    Entry = entry,
                    Score = CalculateTemplateNameMatchScore(entry.NodeName, entry.NodeNameMatchKey, nodeName, nodeNameMatchKey)
                })
                .Where(static item => item.Score > 0)
                .OrderByDescending(static item => item.Score)
                .ThenBy(static item => item.Entry.Rank)
                .Select(static item => item.Entry)
                .FirstOrDefault();
        }

        private static T? FindUnique<T>(IEnumerable<T> items)
            where T : class
        {
            List<T> matches = items.Take(2).ToList();
            return matches.Count == 1 ? matches[0] : null;
        }

        private static int CalculateTemplateNameMatchScore(
            string templateName,
            string templateMatchKey,
            string candidateName,
            string candidateMatchKey)
        {
            if (string.IsNullOrWhiteSpace(templateMatchKey) || string.IsNullOrWhiteSpace(candidateMatchKey))
                return 0;

            if (string.Equals(templateMatchKey, candidateMatchKey, StringComparison.Ordinal))
                return 10_000;

            if (templateMatchKey.Contains(candidateMatchKey, StringComparison.Ordinal) ||
                candidateMatchKey.Contains(templateMatchKey, StringComparison.Ordinal))
            {
                int shorterLength = Math.Min(templateMatchKey.Length, candidateMatchKey.Length);
                int longerLength = Math.Max(templateMatchKey.Length, candidateMatchKey.Length);
                return 6_000 + (shorterLength * 1_000 / Math.Max(1, longerLength));
            }

            HashSet<string> templateTokens = BuildTemplateNameTokens(templateName);
            HashSet<string> candidateTokens = BuildTemplateNameTokens(candidateName);
            if (templateTokens.Count == 0 || candidateTokens.Count == 0)
                return 0;

            int overlap = candidateTokens.Count(templateTokens.Contains);
            if (overlap == 0 ||
                (candidateTokens.Count <= 2 && overlap < candidateTokens.Count) ||
                (candidateTokens.Count > 2 && overlap < 2))
            {
                return 0;
            }

            return 1_000 + (overlap * 100) + (overlap * 100 / candidateTokens.Count) + (overlap * 100 / templateTokens.Count);
        }

        private static bool IsTemplateColumnNumberRow(int sequenceNumber, string systemName, string inventoryNumber) =>
            sequenceNumber == 1 &&
            string.Equals(BuildTemplateSystemOrderKey(systemName), "2", StringComparison.Ordinal) &&
            string.Equals(BuildTemplateInventoryNumberKey(inventoryNumber), "4", StringComparison.Ordinal);

        private static bool IsTemplateDetailRowName(string value)
        {
            string key = BuildTemplateSystemOrderKey(value);
            return !string.IsNullOrWhiteSpace(key) &&
                   !string.Equals(key, BuildTemplateSystemOrderKey(PlanText), StringComparison.Ordinal) &&
                   !string.Equals(key, BuildTemplateSystemOrderKey(FactText), StringComparison.Ordinal) &&
                   !string.Equals(key, BuildTemplateSystemOrderKey(TotalsLabelText), StringComparison.Ordinal) &&
                   !string.Equals(key, BuildTemplateSystemOrderKey(AnnualTotalsLabelText), StringComparison.Ordinal);
        }

        private static string BuildTemplateSystemOrderKey(string? systemName)
        {
            string normalized = Regex.Replace(systemName?.Trim() ?? string.Empty, @"\s+", " ");
            return normalized.ToUpperInvariant();
        }

        private static string BuildTemplateInventoryNumberKey(string? inventoryNumber)
        {
            string normalized = Regex.Replace(inventoryNumber?.Trim() ?? string.Empty, @"\s+", string.Empty);
            return normalized.ToUpperInvariant();
        }

        private static string BuildTemplateNameMatchKey(string? value)
        {
            string normalized = NormalizeTemplateNameForMatching(value);
            return Regex.Replace(normalized, @"[^\p{L}\p{Nd}]+", string.Empty).ToUpperInvariant();
        }

        private static HashSet<string> BuildTemplateNameTokens(string? value)
        {
            string normalized = NormalizeTemplateNameForMatching(value);
            return Regex.Matches(normalized.ToUpperInvariant(), @"[\p{L}\p{Nd}]+")
                .Select(static match => match.Value)
                .Where(static token => token.Length > 0)
                .ToHashSet(StringComparer.Ordinal);
        }

        private static string NormalizeTemplateNameForMatching(string? value)
        {
            string normalized = (value ?? string.Empty)
                .Replace('Ё', 'Е')
                .Replace('ё', 'е')
                .Replace('№', ' ');
            normalized = Regex.Replace(normalized, @"\bАСУТП\b", "АСУ", RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, @"\bМО\b", "медного отделения", RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, @"\bНО\b", "никелевого отделения", RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, @"\bФП\b", "фильтр пресс", RegexOptions.IgnoreCase);
            return normalized;
        }

        private sealed class TemplateSystemOrderEntryBuilder
        {
            public TemplateSystemOrderEntryBuilder(
                int rank,
                int sequenceNumber,
                string systemName,
                string inventoryNumber,
                string systemNameKey,
                string inventoryNumberKey,
                string systemNameMatchKey)
            {
                Rank = rank;
                SequenceNumber = sequenceNumber;
                SystemName = systemName;
                InventoryNumber = inventoryNumber;
                SystemNameKey = systemNameKey;
                InventoryNumberKey = inventoryNumberKey;
                SystemNameMatchKey = systemNameMatchKey;
            }

            public int Rank { get; }

            public int SequenceNumber { get; }

            public string SystemName { get; }

            public string InventoryNumber { get; }

            public string SystemNameKey { get; }

            public string InventoryNumberKey { get; }

            public string SystemNameMatchKey { get; }

            public List<TemplateDetailOrderEntry> DetailRows { get; } = new();

            public TemplateSystemOrderEntry Build() =>
                new(
                    Rank,
                    SequenceNumber,
                    SystemName,
                    InventoryNumber,
                    SystemNameKey,
                    InventoryNumberKey,
                    SystemNameMatchKey,
                    DetailRows.ToArray());
        }

        private sealed record TemplateSystemOrderEntry(
            int Rank,
            int SequenceNumber,
            string SystemName,
            string InventoryNumber,
            string SystemNameKey,
            string InventoryNumberKey,
            string SystemNameMatchKey,
            IReadOnlyList<TemplateDetailOrderEntry> DetailRows);

        private sealed record TemplateDetailOrderEntry(
            int Rank,
            string NodeName,
            string InventoryNumber,
            string NodeNameKey,
            string InventoryNumberKey,
            string NodeNameMatchKey);

        private sealed record OrderedMonthSystemGroup(
            KbMaintenanceMonthSheetSystemGroup Group,
            TemplateSystemOrderEntry? TemplateEntry);

        private sealed record OrderedAnnualSystemGroup(
            KbMaintenanceAnnualSystemGroup Group,
            TemplateSystemOrderEntry? TemplateEntry);

        private sealed record OrderedMonthDetailRow(
            KbMaintenanceMonthSheetDetailRow Row,
            TemplateDetailOrderEntry? TemplateEntry);

        private sealed record OrderedAnnualDetailRow(
            KbMaintenanceAnnualDetailRow Row,
            TemplateDetailOrderEntry? TemplateEntry);

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

        private static void NormalizeMonthlyDayCellStyles(Row row)
        {
            uint? dayCellStyleIndex = ResolveMostCommonStyleIndex(row, FirstDayColumnIndex, FirstDayColumnIndex + 30);
            if (dayCellStyleIndex == null)
                return;

            for (int columnIndex = FirstDayColumnIndex; columnIndex <= FirstDayColumnIndex + 30; columnIndex++)
            {
                Cell cell = GetOrCreateCell(row, columnIndex);
                cell.StyleIndex ??= dayCellStyleIndex.Value;
            }
        }

        private static uint? ResolveMostCommonStyleIndex(Row row, int startColumnIndex, int endColumnIndex)
        {
            return row.Elements<Cell>()
                .Select(cell => new
                {
                    Cell = cell,
                    ColumnIndex = GetColumnIndex(cell.CellReference?.Value)
                })
                .Where(item =>
                    item.ColumnIndex >= startColumnIndex &&
                    item.ColumnIndex <= endColumnIndex &&
                    item.Cell.StyleIndex?.Value != null)
                .GroupBy(item => item.Cell.StyleIndex!.Value)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => (uint?)group.Key)
                .FirstOrDefault();
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
            if (TryFindMonthSheet(workbookPart, month, out Sheet? sheet) && sheet != null)
                return sheet;

            throw new InvalidOperationException($"Лист '{expectedName}' не найден в книге графика ТО.");
        }

        private static bool TryFindMonthSheet(WorkbookPart workbookPart, int month, out Sheet? sheet)
        {
            string expectedName = $"КЦ ({month})";
            sheet = workbookPart.Workbook.Sheets?
                .Elements<Sheet>()
                .FirstOrDefault(sheet => string.Equals(sheet.Name?.Value, expectedName, StringComparison.Ordinal));
            return sheet != null;
        }

        private static bool HasUsableMonthSheets(WorkbookPart workbookPart, int firstMonth, int lastMonth)
        {
            for (int month = firstMonth; month <= lastMonth; month++)
            {
                if (!TryFindMonthSheet(workbookPart, month, out Sheet? sheet) || sheet == null)
                    return false;

                WorksheetPart worksheetPart = GetWorksheetPart(workbookPart, sheet);
                if (!IsUsableMonthSheet(worksheetPart))
                    return false;
            }

            return true;
        }

        private static bool IsUsableMonthSheet(WorksheetPart worksheetPart)
        {
            if (AnnualSheetLayout.TryRead(worksheetPart, out _))
                return false;

            try
            {
                SheetLayout.Read(worksheetPart, requireDetailTemplate: false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Sheet FindAnnualSheet(WorkbookPart workbookPart)
        {
            Sheets sheets = workbookPart.Workbook.Sheets
                ?? throw new InvalidOperationException("Книга годового графика ТО повреждена: отсутствует список листов.");
            foreach (Sheet sheet in sheets.Elements<Sheet>())
            {
                WorksheetPart worksheetPart = GetWorksheetPart(workbookPart, sheet);
                if (AnnualSheetLayout.TryRead(worksheetPart, out _))
                    return sheet;
            }

            throw new InvalidOperationException("В шаблоне годового графика ТО не найден лист установленной годовой формы.");
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

        private static MemoryStream CreateExpandableMemoryStream(byte[] sourceBytes)
        {
            var stream = new MemoryStream();
            stream.Write(sourceBytes, 0, sourceBytes.Length);
            stream.Position = 0;
            return stream;
        }

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

        private sealed record AnnualSheetLayout(
            uint PlanHeaderRowIndex,
            uint TitleStartRowIndex,
            uint TitleEndRowIndex,
            uint DataStartRowIndex,
            uint FirstSystemRowIndex,
            uint FirstDetailRowIndex,
            uint FooterStartRowIndex,
            uint LastUsedRowIndex,
            IReadOnlyDictionary<int, int> PlanColumnByMonth)
        {
            public static AnnualSheetLayout Read(WorksheetPart worksheetPart)
            {
                if (TryRead(worksheetPart, out AnnualSheetLayout? layout))
                    return layout!;

                throw new InvalidOperationException("Лист годового графика ТО повреждён: не найдена годовая таблица с 12 колонками 'план'.");
            }

            public static bool TryRead(WorksheetPart worksheetPart, out AnnualSheetLayout? layout)
            {
                layout = null;
                SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                if (sheetData == null)
                    return false;

                IReadOnlyList<string> sharedStrings = ReadSharedStrings(
                    worksheetPart.GetParentParts().OfType<WorkbookPart>().FirstOrDefault()?.SharedStringTablePart);
                List<Row> rows = sheetData.Elements<Row>().ToList();
                if (!TryFindPlanHeaderRow(rows, sharedStrings, out uint planHeaderRowIndex, out Dictionary<int, int> planColumnByMonth))
                    return false;

                uint dataStartRowIndex = planHeaderRowIndex + 2;
                uint firstDetailRowIndex = FindFirstDetailRowIndex(rows, sharedStrings, dataStartRowIndex, planColumnByMonth.Values);
                uint footerStartRowIndex = FindFooterStartRowIndex(rows, sharedStrings, dataStartRowIndex);
                uint lastUsedRowIndex = rows
                    .Select(row => row.RowIndex?.Value ?? 0)
                    .DefaultIfEmpty(footerStartRowIndex)
                    .Max();

                layout = new AnnualSheetLayout(
                    PlanHeaderRowIndex: planHeaderRowIndex,
                    TitleStartRowIndex: planHeaderRowIndex - 2,
                    TitleEndRowIndex: planHeaderRowIndex + 1,
                    DataStartRowIndex: dataStartRowIndex,
                    FirstSystemRowIndex: dataStartRowIndex,
                    FirstDetailRowIndex: firstDetailRowIndex,
                    FooterStartRowIndex: footerStartRowIndex,
                    LastUsedRowIndex: lastUsedRowIndex,
                    PlanColumnByMonth: planColumnByMonth);
                return true;
            }

            private static bool TryFindPlanHeaderRow(
                IReadOnlyList<Row> rows,
                IReadOnlyList<string> sharedStrings,
                out uint planHeaderRowIndex,
                out Dictionary<int, int> planColumnByMonth)
            {
                planHeaderRowIndex = 0;
                planColumnByMonth = new Dictionary<int, int>();

                foreach (Row row in rows)
                {
                    int[] planColumns = row.Elements<Cell>()
                        .Where(cell => string.Equals(ReadCellText(cell, sharedStrings), PlanText, StringComparison.OrdinalIgnoreCase))
                        .Select(cell => GetColumnIndex(cell.CellReference?.Value))
                        .Where(static columnIndex => columnIndex > 0)
                        .Order()
                        .ToArray();
                    if (planColumns.Length < 12)
                        continue;

                    int month = 1;
                    foreach (int columnIndex in planColumns.Take(12))
                        planColumnByMonth[month++] = columnIndex;

                    planHeaderRowIndex = row.RowIndex?.Value ?? 0;
                    return planHeaderRowIndex > 2;
                }

                return false;
            }

            private static uint FindFirstDetailRowIndex(
                IReadOnlyList<Row> rows,
                IReadOnlyList<string> sharedStrings,
                uint dataStartRowIndex,
                IEnumerable<int> planColumns)
            {
                HashSet<int> planColumnSet = planColumns.ToHashSet();
                Row? detailRow = rows.FirstOrDefault(row =>
                {
                    uint rowIndex = row.RowIndex?.Value ?? 0;
                    return rowIndex >= dataStartRowIndex &&
                           row.Elements<Cell>()
                               .Any(cell =>
                                   planColumnSet.Contains(GetColumnIndex(cell.CellReference?.Value)) &&
                                   IsAnnualPlanCellText(ReadCellText(cell, sharedStrings)));
                });

                if (detailRow?.RowIndex?.Value is uint detailRowIndex)
                    return detailRowIndex;

                throw new InvalidOperationException("Шаблон годового графика ТО повреждён: не найдена строка-шаблон с работой ТО.");
            }

            private static uint FindFooterStartRowIndex(
                IReadOnlyList<Row> rows,
                IReadOnlyList<string> sharedStrings,
                uint dataStartRowIndex)
            {
                Row? footerRow = rows.FirstOrDefault(row =>
                {
                    uint rowIndex = row.RowIndex?.Value ?? 0;
                    return rowIndex > dataStartRowIndex &&
                           string.Equals(ReadCellText(row, 2, sharedStrings), AnnualTotalsLabelText, StringComparison.OrdinalIgnoreCase);
                });

                if (footerRow?.RowIndex?.Value is uint footerRowIndex)
                    return footerRowIndex;

                throw new InvalidOperationException("Шаблон годового графика ТО повреждён: не найдена строка 'Итого'.");
            }

            private static bool IsAnnualPlanCellText(string text)
            {
                return Regex.IsMatch(
                    text?.Trim() ?? string.Empty,
                    @"^ТО[123]\s*/\s*\d+",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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
