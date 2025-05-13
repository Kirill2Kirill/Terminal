using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;

namespace WebhookReceiver
{

    public class MemoryLogSink : ILogEventSink
    {
        private static readonly ConcurrentStack<string> _logStack = new();

        public void Emit(LogEvent logEvent)
        {
            string message = $"{logEvent.Timestamp:dd-MM-yyyy HH:mm:ss} | {logEvent.RenderMessage()}";
            _logStack.Push(message); // Добавляем лог наверх

            // Если логов больше 1000, удаляем 500 самых старых
            if (_logStack.Count > 1000)
            {
                var logsToRemove = _logStack.Reverse().Take(500).ToList(); // Берем 500 старых
                foreach (var log in logsToRemove)
                {
                    _logStack.TryPop(out _); // Удаляем их
                }
            }
        }

        public static IEnumerable<string> GetLogs() => _logStack;
    }
}
