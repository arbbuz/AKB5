using System.Reflection;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseMaintenanceWorkbookTemplateService
    {
        private const string TemplateResourceName = "AsutpKnowledgeBase.Core.Resources.MaintenanceYearTemplate.xlsx";
        private static readonly string[] ExpectedMonthSheetNames = Enumerable
            .Range(1, 12)
            .Select(static month => $"КЦ ({month})")
            .ToArray();

        public byte[] GetTemplatePackage()
        {
            byte[] templateBytes = ReadEmbeddedTemplateBytes();
            ValidateTemplate(templateBytes);
            return templateBytes;
        }

        public IReadOnlyList<string> GetMonthSheetNames()
        {
            byte[] templateBytes = GetTemplatePackage();
            return ReadSheetNames(templateBytes);
        }

        private static byte[] ReadEmbeddedTemplateBytes()
        {
            Assembly assembly = typeof(KnowledgeBaseMaintenanceWorkbookTemplateService).Assembly;
            using Stream? resourceStream = assembly.GetManifestResourceStream(TemplateResourceName);
            if (resourceStream == null)
            {
                throw new InvalidOperationException(
                    "Внутренний шаблон годового графика ТО не найден в ресурсах сборки.");
            }

            using var memoryStream = new MemoryStream();
            resourceStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        private static void ValidateTemplate(byte[] templateBytes)
        {
            IReadOnlyList<string> sheetNames = ReadSheetNames(templateBytes);
            if (sheetNames.Count != ExpectedMonthSheetNames.Length ||
                !sheetNames.SequenceEqual(ExpectedMonthSheetNames, StringComparer.Ordinal))
            {
                string actualNames = string.Join(", ", sheetNames);
                string expectedNames = string.Join(", ", ExpectedMonthSheetNames);
                throw new InvalidOperationException(
                    $"Внутренний шаблон годового графика ТО имеет неверную структуру листов. Ожидалось: {expectedNames}. Получено: {actualNames}.");
            }
        }

        private static IReadOnlyList<string> ReadSheetNames(byte[] templateBytes)
        {
            using var stream = new MemoryStream(templateBytes, writable: false);
            using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);
            WorkbookPart workbookPart = document.WorkbookPart
                ?? throw new InvalidOperationException(
                    "Внутренний шаблон годового графика ТО поврежден: отсутствует workbook part.");

            Sheets sheets = workbookPart.Workbook.Sheets
                ?? throw new InvalidOperationException(
                    "Внутренний шаблон годового графика ТО поврежден: отсутствует список листов.");

            return sheets
                .Elements<Sheet>()
                .Select(static sheet => sheet.Name?.Value?.Trim() ?? string.Empty)
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .ToArray();
        }
    }
}
