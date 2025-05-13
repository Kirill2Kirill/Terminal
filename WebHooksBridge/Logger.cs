using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebHooksBridge
{
    public static class Logger
    {
        private static readonly string logFilePath = "logs.txt";

        public static async Task LogErrorAsync(string message, Exception? ex = null)
        {
            string logMessage = $"[ERROR] {DateTime.UtcNow}: {message}";
            if (ex != null)
                logMessage += $"\nException: {ex.Message}\nStackTrace: {ex.StackTrace}";

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(logMessage);
            Console.ResetColor();

            SaveLogToFile(logMessage);
            await TelegramNotifier.SendMessageAsync(logMessage);
        }

        public static async Task LogSuccessAsync(string message)
        {
            string logMessage = $"[SUCCESS] {DateTime.UtcNow}: {message}";

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(logMessage);
            Console.ResetColor();

            SaveLogToFile(logMessage);
            await TelegramNotifier.SendMessageAsync(logMessage);
        }

        private static void SaveLogToFile(string logMessage)
        {
            //File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }
    }

}
