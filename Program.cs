using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            IAppLogger appLogger = CreateLogger();
            SubscribeToUnhandledExceptions(appLogger);

            appLogger.Log(
                "AppStartup",
                AppLogLevel.Information,
                "AKB5 application startup.");

            try
            {
                ApplicationConfiguration.Initialize();
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
