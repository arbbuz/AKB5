using System.Reflection;
using System.Runtime.Loader;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    public sealed class KnowledgeBaseActDocxPluginLoader
    {
        private const string PluginDirectoryName = "act-docx";
        private const string PluginAssemblyFileName = "AsutpKnowledgeBase.ActDocx.dll";
        private const string PluginTypeName = "AsutpKnowledgeBase.Services.KnowledgeBaseActDocxGeneratorPlugin";

        private readonly object _syncRoot = new();
        private ActDocxLoadContext? _loadContext;
        private IKnowledgeBaseActDocxGenerator? _generator;

        public KnowledgeBaseActDocxPluginLoader()
            : this(AppContext.BaseDirectory)
        {
        }

        public KnowledgeBaseActDocxPluginLoader(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
                baseDirectory = AppContext.BaseDirectory;

            PluginAssemblyPath = Path.Combine(baseDirectory, PluginDirectoryName, PluginAssemblyFileName);
        }

        public string PluginAssemblyPath { get; }

        public bool IsAvailable => File.Exists(PluginAssemblyPath);

        public KnowledgeBaseActDocxGenerationResult Generate(KnowledgeBaseActDocxGenerationRequest request)
        {
            if (!IsAvailable)
                return Failure($"Модуль DOCX актов не найден: {PluginAssemblyPath}");

            if (!TryGetGenerator(out IKnowledgeBaseActDocxGenerator? generator, out string errorMessage))
                return Failure(errorMessage);

            try
            {
                return generator.Generate(request);
            }
            catch (Exception ex)
            {
                return Failure($"Ошибка выполнения модуля DOCX актов: {ex.GetBaseException().Message}");
            }
        }

        private bool TryGetGenerator(
            out IKnowledgeBaseActDocxGenerator generator,
            out string errorMessage)
        {
            lock (_syncRoot)
            {
                if (_generator != null)
                {
                    generator = _generator;
                    errorMessage = string.Empty;
                    return true;
                }

                try
                {
                    var loadContext = new ActDocxLoadContext(PluginAssemblyPath);
                    Assembly assembly = loadContext.LoadFromAssemblyPath(PluginAssemblyPath);
                    Type? pluginType = assembly.GetType(PluginTypeName, throwOnError: false, ignoreCase: false);
                    if (pluginType == null)
                    {
                        generator = null!;
                        errorMessage = $"В модуле DOCX актов не найден тип: {PluginTypeName}";
                        return false;
                    }

                    if (!typeof(IKnowledgeBaseActDocxGenerator).IsAssignableFrom(pluginType))
                    {
                        generator = null!;
                        errorMessage = $"Тип модуля DOCX актов не реализует ожидаемый контракт: {PluginTypeName}";
                        return false;
                    }

                    if (Activator.CreateInstance(pluginType) is not IKnowledgeBaseActDocxGenerator createdGenerator)
                    {
                        generator = null!;
                        errorMessage = $"Не удалось создать экземпляр модуля DOCX актов: {PluginTypeName}";
                        return false;
                    }

                    _loadContext = loadContext;
                    _generator = createdGenerator;
                    generator = createdGenerator;
                    errorMessage = string.Empty;
                    return true;
                }
                catch (Exception ex)
                {
                    generator = null!;
                    errorMessage = $"Не удалось загрузить модуль DOCX актов: {ex.GetBaseException().Message}";
                    return false;
                }
            }
        }

        private static KnowledgeBaseActDocxGenerationResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };

        private sealed class ActDocxLoadContext : AssemblyLoadContext
        {
            private static readonly string CoreAssemblyName =
                typeof(IKnowledgeBaseActDocxGenerator).Assembly.GetName().Name ?? string.Empty;

            private readonly AssemblyDependencyResolver _resolver;

            public ActDocxLoadContext(string pluginAssemblyPath)
                : base(nameof(ActDocxLoadContext), isCollectible: false)
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
