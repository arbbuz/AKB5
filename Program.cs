using System.Diagnostics;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            var startupStopwatch = Stopwatch.StartNew();
            IAppLogger appLogger = CreateLogger();
            LogStartupTiming(
                appLogger,
                "program-logger-created",
                startupStopwatch,
                ("loggerType", appLogger.GetType().Name));
            SubscribeToUnhandledExceptions(appLogger);

            using Mutex singleInstanceMutex = new(
                initiallyOwned: true,
                name: @"Local\AKB5.AsutpKnowledgeBase.SingleInstance",
                createdNew: out bool isFirstInstance);
            LogStartupTiming(
                appLogger,
                "program-single-instance-checked",
                startupStopwatch,
                ("isFirstInstance", isFirstInstance));

            if (!isFirstInstance)
            {
                MessageBox.Show(
                    "Программа АКБ5 уже запущена. Закройте текущий экземпляр перед повторным запуском.",
                    "АКБ5 уже запущена",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            appLogger.Log(
                "AppStartup",
                AppLogLevel.Information,
                "AKB5 application startup.");

            try
            {
                ApplicationConfiguration.Initialize();
                LogStartupTiming(appLogger, "program-application-configured", startupStopwatch);
                Application.Run(new MainForm(appLogger));
            }
            finally
            {
                appLogger.Log(
                    "AppShutdown",
                    AppLogLevel.Information,
                    "AKB5 application shutdown.");
            }
        }

        private static void LogStartupTiming(
            IAppLogger logger,
            string stage,
            Stopwatch stopwatch,
            params (string Key, object? Value)[] values)
        {
            var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["stage"] = stage,
                ["elapsedMs"] = stopwatch.ElapsedMilliseconds
            };

            foreach ((string key, object? value) in values)
            {
                if (!string.IsNullOrWhiteSpace(key) && value != null)
                    properties[key] = value;
            }

            logger.Log(
                "StartupTiming",
                AppLogLevel.Information,
                "AKB5 startup timing checkpoint.",
                properties: properties);
        }

        private static IAppLogger CreateLogger()
        {
            try
            {
                return new FileAppLogger();
            }
            catch
            {
                return NullAppLogger.Instance;
            }
        }

        private static void SubscribeToUnhandledExceptions(IAppLogger appLogger)
        {
            Application.ThreadException += (_, args) =>
                appLogger.Log(
                    "UnhandledThreadException",
                    AppLogLevel.Critical,
                    "Unhandled UI thread exception.",
                    args.Exception);

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                appLogger.Log(
                    "UnhandledDomainException",
                    AppLogLevel.Critical,
                    "Unhandled AppDomain exception.",
                    args.ExceptionObject as Exception,
                    new Dictionary<string, object?>
                    {
                        ["isTerminating"] = args.IsTerminating
                    });

            TaskScheduler.UnobservedTaskException += (_, args) =>
                appLogger.Log(
                    "UnobservedTaskException",
                    AppLogLevel.Critical,
                    "Unobserved task exception.",
                    args.Exception);
        }
    }
}
