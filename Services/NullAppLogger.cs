namespace AsutpKnowledgeBase.Services
{
    public sealed class NullAppLogger : IAppLogger
    {
        public static NullAppLogger Instance { get; } = new();

        private NullAppLogger()
        {
        }

        public void Log(
            string eventName,
            AppLogLevel level,
            string message,
            Exception? exception = null,
            IReadOnlyDictionary<string, object?>? properties = null)
        {
        }
    }
}
