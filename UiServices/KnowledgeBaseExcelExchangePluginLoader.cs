using System.Reflection;
using System.Runtime.Loader;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    public sealed class KnowledgeBaseExcelExchangePluginLoader
    {
        private const string PluginDirectoryName = "excel-exchange";
        private const string PluginAssemblyFileName = "AsutpKnowledgeBase.ExcelExchange.dll";
        private const string PluginTypeName = "AsutpKnowledgeBase.Services.KnowledgeBaseExcelExchangePlugin";

        private readonly object _syncRoot = new();
        private readonly IAppLogger _logger;
        private ExcelExchangeLoadContext? _loadContext;
        private object? _plugin;

        public KnowledgeBaseExcelExchangePluginLoader(IAppLogger? logger = null)
            : this(AppContext.BaseDirectory, logger)
        {
        }

        public KnowledgeBaseExcelExchangePluginLoader(string baseDirectory, IAppLogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
                baseDirectory = AppContext.BaseDirectory;

            _logger = logger ?? NullAppLogger.Instance;
            PluginAssemblyPath = Path.Combine(baseDirectory, PluginDirectoryName, PluginAssemblyFileName);
        }

        public string PluginAssemblyPath { get; }

        public bool IsAvailable => File.Exists(PluginAssemblyPath);

        public bool TryEnsureAvailable(out string errorMessage) =>
            TryGetPlugin<IKnowledgeBaseExcelExchangeService>(out _, out errorMessage);

        public byte[] BuildWorkbookPackage(SavedData data)
        {
            if (!TryGetPlugin<IKnowledgeBaseExcelExchangeService>(out var service, out string errorMessage))
                throw new InvalidOperationException(errorMessage);

            return service.BuildWorkbookPackage(data);
        }

        public KnowledgeBaseExcelExportResult Export(SavedData data, string path)
        {
            if (!TryGetPlugin<IKnowledgeBaseExcelExchangeService>(out var service, out string errorMessage))
                return ExcelExportFailure(errorMessage);

            try
            {
                return service.Export(data, path);
            }
            catch (Exception ex)
            {
                return ExcelExportFailure($"Ошибка выполнения модуля Excel: {ex.GetBaseException().Message}");
            }
        }

        public KnowledgeBaseExcelImportResult Import(string path)
        {
            if (!TryGetPlugin<IKnowledgeBaseExcelExchangeService>(out var service, out string errorMessage))
                return ExcelImportFailure(errorMessage);

            try
            {
                return service.Import(path);
            }
            catch (Exception ex)
            {
                return ExcelImportFailure($"Ошибка выполнения модуля Excel: {ex.GetBaseException().Message}");
            }
        }

        public KnowledgeBaseExcelImportResult ImportFromPackage(byte[] packageBytes)
        {
            if (!TryGetPlugin<IKnowledgeBaseExcelExchangeService>(out var service, out string errorMessage))
                return ExcelImportFailure(errorMessage);

            try
            {
                return service.ImportFromPackage(packageBytes);
            }
            catch (Exception ex)
            {
                return ExcelImportFailure($"Ошибка выполнения модуля Excel: {ex.GetBaseException().Message}");
            }
        }

        public KnowledgeBaseMaintenanceScheduleNormImportResult ImportMaintenanceScheduleNormWorkbook(
            byte[] packageBytes,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? existingProfiles)
        {
            if (!TryGetPlugin<IKnowledgeBaseMaintenanceScheduleNormImporter>(out var importer, out string errorMessage))
                return MaintenanceScheduleNormFailure(errorMessage);

            try
            {
                return importer.ImportWorkbook(packageBytes, roots, existingProfiles);
            }
            catch (Exception ex)
            {
                return MaintenanceScheduleNormFailure($"Ошибка выполнения модуля Excel: {ex.GetBaseException().Message}");
            }
        }

        public KnowledgeBaseMaintenanceWorkbookGenerationResult GenerateSingleMonthWorkbook(
            int year,
            int month,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default)
        {
            if (!TryGetPlugin<IKnowledgeBaseMaintenanceWorkbookGenerator>(out var generator, out string errorMessage))
                return WorkbookGenerationFailure(errorMessage);

            try
            {
                return generator.GenerateSingleMonthWorkbook(
                    year,
                    month,
                    totalMonthlyHourBudget,
                    roots,
                    maintenanceScheduleProfiles,
                    productionCalendarYears,
                    planningMode);
            }
            catch (Exception ex)
            {
                return WorkbookGenerationFailure($"Ошибка выполнения модуля Excel: {ex.GetBaseException().Message}");
            }
        }

        public KnowledgeBaseMaintenanceAnnualWorkbookGenerationResult GenerateAnnualWorkbook(
            int year,
            string workshopName,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears)
        {
            if (!TryGetPlugin<IKnowledgeBaseMaintenanceWorkbookGenerator>(out var generator, out string errorMessage))
                return AnnualWorkbookGenerationFailure(errorMessage);

            try
            {
                return generator.GenerateAnnualWorkbook(
                    year,
                    workshopName,
                    roots,
                    maintenanceScheduleProfiles,
                    productionCalendarYears);
            }
            catch (Exception ex)
            {
                return AnnualWorkbookGenerationFailure($"Ошибка выполнения модуля Excel: {ex.GetBaseException().Message}");
            }
        }

        public KnowledgeBaseMaintenanceYearWorkbookGenerationResult GenerateYearWorkbook(
            byte[]? existingWorkbookPackage,
            int year,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default)
        {
            if (!TryGetPlugin<IKnowledgeBaseMaintenanceWorkbookGenerator>(out var generator, out string errorMessage))
                return YearWorkbookGenerationFailure(errorMessage);

            try
            {
                return generator.GenerateYearWorkbook(
                    existingWorkbookPackage,
                    year,
                    totalMonthlyHourBudget,
                    roots,
                    maintenanceScheduleProfiles,
                    productionCalendarYears,
                    planningMode);
            }
            catch (Exception ex)
            {
                return YearWorkbookGenerationFailure($"Ошибка выполнения модуля Excel: {ex.GetBaseException().Message}");
            }
        }

        public KnowledgeBaseMaintenanceYearWorkbookGenerationResult GenerateYearWorkbookFromMonth(
            byte[]? existingWorkbookPackage,
            int year,
            int startMonth,
            int totalMonthlyHourBudget,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles,
            IReadOnlyList<KbProductionCalendarYear>? productionCalendarYears,
            KnowledgeBaseMaintenancePlanningMode planningMode = KnowledgeBaseMaintenancePlanningMode.Default)
        {
            if (!TryGetPlugin<IKnowledgeBaseMaintenanceWorkbookGenerator>(out var generator, out string errorMessage))
                return YearWorkbookGenerationFailure(errorMessage);

            try
            {
                return generator.GenerateYearWorkbookFromMonth(
                    existingWorkbookPackage,
                    year,
                    startMonth,
                    totalMonthlyHourBudget,
                    roots,
                    maintenanceScheduleProfiles,
                    productionCalendarYears,
                    planningMode);
            }
            catch (Exception ex)
            {
                return YearWorkbookGenerationFailure($"Ошибка выполнения модуля Excel: {ex.GetBaseException().Message}");
            }
        }

        public List<KnowledgeBaseMaintenanceYearScheduleSourceRow> BuildMaintenanceYearScheduleSourceRows(
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (!TryGetPlugin<IKnowledgeBaseMaintenanceYearScheduleSourceService>(out var service, out _))
                return new List<KnowledgeBaseMaintenanceYearScheduleSourceRow>();

            return service.BuildRows(roots, maintenanceScheduleProfiles);
        }

        public KnowledgeBaseMaintenanceYearScheduleSourceApplyResult ApplyMaintenanceYearScheduleSourceRows(
            IReadOnlyList<KnowledgeBaseMaintenanceYearScheduleSourceRow>? rows,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (!TryGetPlugin<IKnowledgeBaseMaintenanceYearScheduleSourceService>(out var service, out string errorMessage))
                return YearScheduleSourceApplyFailure(errorMessage);

            try
            {
                return service.ApplyRows(rows, roots, maintenanceScheduleProfiles);
            }
            catch (Exception ex)
            {
                return YearScheduleSourceApplyFailure($"Ошибка выполнения модуля Excel: {ex.GetBaseException().Message}");
            }
        }

        public KnowledgeBaseMaintenanceYearScheduleSourceExportResult ExportMaintenanceYearScheduleSourceWorkbook(
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (!TryGetPlugin<IKnowledgeBaseMaintenanceYearScheduleSourceExchange>(out var exchange, out string errorMessage))
                return YearScheduleSourceExportFailure(errorMessage);

            try
            {
                return exchange.ExportWorkbook(roots, maintenanceScheduleProfiles);
            }
            catch (Exception ex)
            {
                return YearScheduleSourceExportFailure($"Ошибка выполнения модуля Excel: {ex.GetBaseException().Message}");
            }
        }

        public KnowledgeBaseMaintenanceYearScheduleSourceImportResult ImportMaintenanceYearScheduleSourceWorkbook(
            byte[] workbookPackage,
            IReadOnlyList<KbNode>? roots,
            IReadOnlyList<KbMaintenanceScheduleProfile>? maintenanceScheduleProfiles)
        {
            if (!TryGetPlugin<IKnowledgeBaseMaintenanceYearScheduleSourceExchange>(out var exchange, out string errorMessage))
                return YearScheduleSourceImportFailure(errorMessage);

            try
            {
                return exchange.ImportWorkbook(workbookPackage, roots, maintenanceScheduleProfiles);
            }
            catch (Exception ex)
            {
                return YearScheduleSourceImportFailure($"Ошибка выполнения модуля Excel: {ex.GetBaseException().Message}");
            }
        }

        private bool TryGetPlugin<TService>(out TService service, out string errorMessage)
            where TService : class
        {
            lock (_syncRoot)
            {
                if (!TryGetPluginCore(out object? plugin, out errorMessage))
                {
                    service = null!;
                    return false;
                }

                if (plugin is TService typedService)
                {
                    service = typedService;
                    errorMessage = string.Empty;
                    return true;
                }

                service = null!;
                errorMessage = $"Модуль Excel не реализует ожидаемый контракт: {typeof(TService).Name}";
                return false;
            }
        }

        private bool TryGetPluginCore(out object? plugin, out string errorMessage)
        {
            if (_plugin != null)
            {
                plugin = _plugin;
                errorMessage = string.Empty;
                return true;
            }

            if (!IsAvailable)
            {
                plugin = null;
                errorMessage = $"Модуль Excel не найден: {PluginAssemblyPath}";
                return false;
            }

            try
            {
                var loadContext = new ExcelExchangeLoadContext(PluginAssemblyPath);
                Assembly assembly = loadContext.LoadFromAssemblyPath(PluginAssemblyPath);
                Type? pluginType = assembly.GetType(PluginTypeName, throwOnError: false, ignoreCase: false);
                if (pluginType == null)
                {
                    plugin = null;
                    errorMessage = $"В модуле Excel не найден тип: {PluginTypeName}";
                    return false;
                }

                object? createdPlugin = CreatePluginInstance(pluginType);
                if (createdPlugin == null)
                {
                    plugin = null;
                    errorMessage = $"Не удалось создать экземпляр модуля Excel: {PluginTypeName}";
                    return false;
                }

                _loadContext = loadContext;
                _plugin = createdPlugin;
                plugin = createdPlugin;
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                plugin = null;
                errorMessage = $"Не удалось загрузить модуль Excel: {ex.GetBaseException().Message}";
                return false;
            }
        }

        private object? CreatePluginInstance(Type pluginType)
        {
            ConstructorInfo? loggerConstructor = pluginType.GetConstructor(new[] { typeof(IAppLogger) });
            if (loggerConstructor != null)
                return loggerConstructor.Invoke(new object?[] { _logger });

            return Activator.CreateInstance(pluginType);
        }

        private static KnowledgeBaseExcelExportResult ExcelExportFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseExcelImportResult ExcelImportFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseMaintenanceScheduleNormImportResult MaintenanceScheduleNormFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseMaintenanceWorkbookGenerationResult WorkbookGenerationFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseMaintenanceAnnualWorkbookGenerationResult AnnualWorkbookGenerationFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseMaintenanceYearWorkbookGenerationResult YearWorkbookGenerationFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseMaintenanceYearScheduleSourceApplyResult YearScheduleSourceApplyFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseMaintenanceYearScheduleSourceExportResult YearScheduleSourceExportFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseMaintenanceYearScheduleSourceImportResult YearScheduleSourceImportFailure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private sealed class ExcelExchangeLoadContext : AssemblyLoadContext
        {
            private static readonly string CoreAssemblyName =
                typeof(IKnowledgeBaseExcelExchangeService).Assembly.GetName().Name ?? string.Empty;

            private readonly AssemblyDependencyResolver _resolver;

            public ExcelExchangeLoadContext(string pluginAssemblyPath)
                : base(nameof(ExcelExchangeLoadContext), isCollectible: false)
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
