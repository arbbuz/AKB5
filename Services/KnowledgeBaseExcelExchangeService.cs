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
        private readonly IAppLogger _logger;

        public KnowledgeBaseExcelExchangeService(IAppLogger? logger = null)
        {
            _logger = logger ?? NullAppLogger.Instance;
        }

        public byte[] BuildWorkbookPackage(SavedData data) => _writer.BuildWorkbookPackage(data);

        public KnowledgeBaseExcelExportResult Export(SavedData data, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.Log(
                    "ExcelExportFailed",
                    AppLogLevel.Warning,
                    "Excel export path is missing.",
                    properties: CreateProperties(
                        ("path", path),
                        ("formatVersion", WorkbookFormatVersion)));

                return new KnowledgeBaseExcelExportResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Не указан путь для экспорта."
                };
            }

            _logger.Log(
                "ExcelExportStarted",
                AppLogLevel.Information,
                "Starting Excel export.",
                properties: CreateProperties(
                    ("path", path),
                    ("formatVersion", WorkbookFormatVersion)));

            try
            {
                byte[] packageBytes = BuildWorkbookPackage(data);
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(path, packageBytes);

                _logger.Log(
                    "ExcelExportSucceeded",
                    AppLogLevel.Information,
                    "Excel export completed successfully.",
                    properties: CreateProperties(
                        ("path", path),
                        ("fileSizeBytes", packageBytes.LongLength),
                        ("formatVersion", WorkbookFormatVersion)));

                return new KnowledgeBaseExcelExportResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "ExcelExportFailed",
                    AppLogLevel.Error,
                    "Excel export failed.",
                    ex,
                    CreateProperties(
                        ("path", path),
                        ("formatVersion", WorkbookFormatVersion)));

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

            long? fileSizeBytes = GetFileSize(path);
            bool fileExists = File.Exists(path);
            _logger.Log(
                "ExcelImportStarted",
                AppLogLevel.Information,
                "Starting Excel import.",
                properties: CreateProperties(
                    ("path", path),
                    ("fileExists", fileExists),
                    ("fileSizeBytes", fileSizeBytes)));

            if (!File.Exists(path))
            {
                _logger.Log(
                    "ExcelImportFailed",
                    AppLogLevel.Warning,
                    "Excel import file was not found.",
                    properties: CreateProperties(
                        ("path", path),
                        ("fileExists", false),
                        ("fileSizeBytes", fileSizeBytes)));

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
                    _logger.Log(
                        "ExcelImportFailed",
                        AppLogLevel.Warning,
                        "Excel import requires an XLSX workbook.",
                        properties: CreateProperties(
                            ("path", path),
                            ("fileExists", true),
                            ("fileSizeBytes", fileBytes.LongLength)));

                    return new KnowledgeBaseExcelImportResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Поддерживается только формат Excel Workbook (*.xlsx)."
                    };
                }

                return ImportFromPackageInternal(fileBytes, path, emitStartEvent: false);
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "ExcelImportFailed",
                    AppLogLevel.Error,
                    "Excel import file could not be read.",
                    ex,
                    CreateProperties(
                        ("path", path),
                        ("fileExists", true),
                        ("fileSizeBytes", fileSizeBytes)));

                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка чтения Excel-файла: {ex.Message}"
                };
            }
        }

        public KnowledgeBaseExcelImportResult ImportFromPackage(byte[] packageBytes) =>
            ImportFromPackageInternal(packageBytes, path: null, emitStartEvent: true);

        private KnowledgeBaseExcelImportResult ImportFromPackageInternal(
            byte[] packageBytes,
            string? path,
            bool emitStartEvent)
        {
            if (emitStartEvent)
            {
                _logger.Log(
                    "ExcelImportStarted",
                    AppLogLevel.Information,
                    "Starting Excel import from workbook package.",
                    properties: CreateProperties(
                        ("path", path),
                        ("fileSizeBytes", packageBytes.LongLength)));
            }

            if (packageBytes.Length == 0)
            {
                _logger.Log(
                    "ExcelImportFailed",
                    AppLogLevel.Warning,
                    "Excel import package is empty.",
                    properties: CreateProperties(
                        ("path", path),
                        ("fileSizeBytes", packageBytes.LongLength)));

                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Файл импорта пустой."
                };
            }

            try
            {
                var data = _reader.ParseWorkbookPackage(packageBytes);
                _logger.Log(
                    "ExcelImportSucceeded",
                    AppLogLevel.Information,
                    "Excel import completed successfully.",
                    properties: CreateProperties(
                        ("path", path),
                        ("fileExists", path == null ? null : true),
                        ("fileSizeBytes", packageBytes.LongLength),
                        ("formatVersion", WorkbookFormatVersion)));

                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = true,
                    Data = data
                };
            }
            catch (KnowledgeBaseExcelImportException ex)
            {
                _logger.Log(
                    "ExcelImportParseFailed",
                    AppLogLevel.Warning,
                    "Excel import workbook validation failed.",
                    ex,
                    CreateProperties(
                        ("path", path),
                        ("fileExists", path == null ? null : true),
                        ("fileSizeBytes", packageBytes.LongLength)));

                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "ExcelImportFailed",
                    AppLogLevel.Error,
                    "Excel import failed while parsing workbook.",
                    ex,
                    CreateProperties(
                        ("path", path),
                        ("fileExists", path == null ? null : true),
                        ("fileSizeBytes", packageBytes.LongLength)));

                return new KnowledgeBaseExcelImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка разбора Excel-файла: {ex.Message}"
                };
            }
        }

        private static long? GetFileSize(string path)
        {
            try
            {
                return File.Exists(path)
                    ? new FileInfo(path).Length
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static Dictionary<string, object?> CreateProperties(params (string Key, object? Value)[] values)
        {
            var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                if (string.IsNullOrWhiteSpace(key) || value == null)
                    continue;

                properties[key] = value;
            }

            return properties;
        }

        private static bool IsZipArchive(byte[] fileBytes) =>
            fileBytes.Length >= 4 &&
            fileBytes[0] == (byte)'P' &&
            fileBytes[1] == (byte)'K' &&
            (fileBytes[2] == 3 || fileBytes[2] == 5 || fileBytes[2] == 7) &&
            (fileBytes[3] == 4 || fileBytes[3] == 6 || fileBytes[3] == 8);
    }
}
