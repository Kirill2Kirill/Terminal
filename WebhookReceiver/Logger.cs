
using Serilog;
using Serilog.Events;
using System.Collections.Concurrent;

namespace WebhookReceiver
{
    public class LogEntry
    {
        public string Date { get; set; }
        public string Time { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
    }

    public static class Logger 
    {
        private static readonly ConcurrentStack<LogEntry> _logStack = new();
        private static readonly Serilog.ILogger _fileLogger;

        static Logger()
        {
            _fileLogger = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Information)
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31)
                .CreateLogger();
        }

        public static void Information(string message) => Write("Information", message);
        public static void Warning(string message) => Write("Warning", message);
        public static void Error(string message, Exception? exception = null) => Write("Error", message, exception);

        private static void Write(string level, string message, Exception? exception = null)
        {
            var logEntry = new LogEntry
            {
                Date = DateTime.Now.ToString("dd-MM-yyyy"),
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Level = level,
                Message = message
            };

            _logStack.Push(logEntry);

            if (_logStack.Count > 1000)
            {
                var logsToRemove = _logStack.Reverse().Take(500).ToList();
                foreach (var log in logsToRemove)
                {
                    _logStack.TryPop(out _);
                }
            }

            _fileLogger.Write((LogEventLevel)Enum.Parse(typeof(LogEventLevel), level, true), exception, message);
        }

        public static IEnumerable<LogEntry> GetLogs() => _logStack.ToList();
    }
}

