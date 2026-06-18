using System.Reflection;
using System.Runtime.Loader;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    public sealed class KnowledgeBaseProductionCalendarPdfImportPluginLoader
    {
        private const string PluginDirectoryName = "pdf-import";
        private const string PluginAssemblyFileName = "AsutpKnowledgeBase.PdfImport.dll";
        private const string ImporterTypeName = "AsutpKnowledgeBase.Services.KnowledgeBaseProductionCalendarPdfImportService";

        private readonly object _syncRoot = new();
        private PdfImportLoadContext? _loadContext;
        private IKnowledgeBaseProductionCalendarPdfImporter? _importer;

        public KnowledgeBaseProductionCalendarPdfImportPluginLoader()
            : this(AppContext.BaseDirectory)
        {
        }

        public KnowledgeBaseProductionCalendarPdfImportPluginLoader(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
                baseDirectory = AppContext.BaseDirectory;

            PluginAssemblyPath = Path.Combine(baseDirectory, PluginDirectoryName, PluginAssemblyFileName);
        }

        public string PluginAssemblyPath { get; }

        public bool IsAvailable => File.Exists(PluginAssemblyPath);

        public KnowledgeBaseProductionCalendarPdfImportResult ImportPdf(byte[]? pdfBytes)
        {
            if (!IsAvailable)
                return Failure($"Модуль импорта PDF не найден: {PluginAssemblyPath}");

            if (!TryGetImporter(out IKnowledgeBaseProductionCalendarPdfImporter? importer, out string errorMessage))
                return Failure(errorMessage);

            try
            {
                return importer.ImportPdf(pdfBytes);
            }
            catch (Exception ex)
            {
                return Failure($"Ошибка выполнения модуля импорта PDF: {ex.GetBaseException().Message}");
            }
        }

        private bool TryGetImporter(
            out IKnowledgeBaseProductionCalendarPdfImporter importer,
            out string errorMessage)
        {
            lock (_syncRoot)
            {
                if (_importer != null)
                {
                    importer = _importer;
                    errorMessage = string.Empty;
                    return true;
                }

                try
                {
                    var loadContext = new PdfImportLoadContext(PluginAssemblyPath);
                    Assembly assembly = loadContext.LoadFromAssemblyPath(PluginAssemblyPath);
                    Type? importerType = assembly.GetType(ImporterTypeName, throwOnError: false, ignoreCase: false);
                    if (importerType == null)
                    {
                        importer = null!;
                        errorMessage = $"В модуле импорта PDF не найден тип: {ImporterTypeName}";
                        return false;
                    }

                    if (!typeof(IKnowledgeBaseProductionCalendarPdfImporter).IsAssignableFrom(importerType))
                    {
                        importer = null!;
                        errorMessage = $"Тип модуля импорта PDF не реализует ожидаемый контракт: {ImporterTypeName}";
                        return false;
                    }

                    if (Activator.CreateInstance(importerType) is not IKnowledgeBaseProductionCalendarPdfImporter createdImporter)
                    {
                        importer = null!;
                        errorMessage = $"Не удалось создать экземпляр модуля импорта PDF: {ImporterTypeName}";
                        return false;
                    }

                    _loadContext = loadContext;
                    _importer = createdImporter;
                    importer = createdImporter;
                    errorMessage = string.Empty;
                    return true;
                }
                catch (Exception ex)
                {
                    importer = null!;
                    errorMessage = $"Не удалось загрузить модуль импорта PDF: {ex.GetBaseException().Message}";
                    return false;
                }
            }
        }

        private static KnowledgeBaseProductionCalendarPdfImportResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private sealed class PdfImportLoadContext : AssemblyLoadContext
        {
            private static readonly string CoreAssemblyName =
                typeof(IKnowledgeBaseProductionCalendarPdfImporter).Assembly.GetName().Name ?? string.Empty;

            private readonly AssemblyDependencyResolver _resolver;

            public PdfImportLoadContext(string pluginAssemblyPath)
                : base(nameof(PdfImportLoadContext), isCollectible: false)
            {
                _resolver = new AssemblyDependencyResolver(pluginAssemblyPath);
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                if (string.Equals(assemblyName.Name, CoreAssemblyName, StringComparison.OrdinalIgnoreCase))
                    return null;

                string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
                return assemblyPath == null ? null : LoadFromAssemblyPath(assemblyPath);
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
                return libraryPath == null ? IntPtr.Zero : LoadUnmanagedDllFromPath(libraryPath);
            }
        }
    }
}
