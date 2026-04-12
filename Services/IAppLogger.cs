namespace AsutpKnowledgeBase.Services
{
    public interface IAppLogger
    {
        void Log(
            string eventName,
            AppLogLevel level,
            string message,
            Exception? exception = null,
            IReadOnlyDictionary<string, object?>? properties = null);
    }
}
