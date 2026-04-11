using System;
using System.IO;
using System.Text;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public class KnowledgeBaseExcelExportResult
    {
        public bool IsSuccess { get; init; }

        public string? ErrorMessage { get; init; }
    }

    public class KnowledgeBaseExcelImportResult
    {
        public bool IsSuccess { get; init; }

        public SavedData? Data { get; init; }

        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Экспортирует и импортирует базу знаний в Excel-compatible SpreadsheetML 2003 workbook
    /// без внешних зависимостей. Контракт книги фиксирован:
    /// листы Meta, Levels, Workshops и Nodes.
    /// </summary>
    public class KnowledgeBaseExcelExchangeService
    {
        public const string WorkbookFormatId = "AKB5.ExcelExchange";
        public const int WorkbookFormatVersion = 1;

        private readonly KnowledgeBaseSpreadsheetMlWriter _writer = new();
        private readonly KnowledgeBaseSpreadsheetMlReader _reader = new();

        public string BuildWorkbookXml(SavedData data) => _writer.BuildWorkbookXml(data);

        public KnowledgeBaseExcelExportResult Export(SavedData data, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return new KnowledgeBaseExcelExportResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Не указан путь для экспорта."
                };
            }

            try
            {
                string xml = BuildWorkbookXml(data);
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(path, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return new KnowledgeBaseExcelExportResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new KnowledgeBaseExcelExportResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public KnowledgeBaseExcelImportResult Import(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Не указан путь для импорта."
                };
            }

            if (!File.Exists(path))
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Файл '{path}' не найден."
                };
            }

            try
            {
                return ImportFromXml(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка чтения Excel-файла: {ex.Message}"
                };
            }
        }

        public KnowledgeBaseExcelImportResult ImportFromXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Файл импорта пустой."
                };
            }

            try
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = true,
                    Data = _reader.ParseWorkbookXml(xml)
                };
            }
            catch (KnowledgeBaseExcelImportException ex)
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка разбора Excel-файла: {ex.Message}"
                };
            }
        }
    }
}
