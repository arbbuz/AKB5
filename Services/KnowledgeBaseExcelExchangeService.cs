using System;
using System.IO;
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
    /// Экспортирует и импортирует базу знаний в xlsx workbook.
    /// Актуальный export-контракт: Meta, Levels, Workshops и отдельные листы узлов по цехам.
    /// </summary>
    public class KnowledgeBaseExcelExchangeService
    {
        public const string WorkbookFormatId = "AKB5.ExcelExchange";
        public const int WorkbookFormatVersion = 3;

        private readonly KnowledgeBaseXlsxWriter _writer = new();
        private readonly KnowledgeBaseXlsxReader _reader = new();

        public byte[] BuildWorkbookPackage(SavedData data) => _writer.BuildWorkbookPackage(data);

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
                byte[] packageBytes = BuildWorkbookPackage(data);
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(path, packageBytes);
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
                byte[] fileBytes = File.ReadAllBytes(path);
                if (!IsZipArchive(fileBytes))
                {
                    return new KnowledgeBaseExcelImportResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Поддерживается только формат Excel Workbook (*.xlsx)."
                    };
                }

                return ImportFromPackage(fileBytes);
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

        public KnowledgeBaseExcelImportResult ImportFromPackage(byte[] packageBytes)
        {
            if (packageBytes.Length == 0)
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
                    Data = _reader.ParseWorkbookPackage(packageBytes)
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

        private static bool IsZipArchive(byte[] fileBytes) =>
            fileBytes.Length >= 4 &&
            fileBytes[0] == (byte)'P' &&
            fileBytes[1] == (byte)'K' &&
            (fileBytes[2] == 3 || fileBytes[2] == 5 || fileBytes[2] == 7) &&
            (fileBytes[3] == 4 || fileBytes[3] == 6 || fileBytes[3] == 8);
    }
}
