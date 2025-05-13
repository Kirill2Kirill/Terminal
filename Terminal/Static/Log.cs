using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Terminal.Models;
using Serilog.Core;
using System.Reflection.Emit;
using CryptoExchange.Net;
using Serilog.Events;

namespace Terminal.Static
{
    public static class Log
    {
        private static Serilog.ILogger _logger; // Собственный экземпляр логгера
        private static int _logIndex = 0; // Общий индекс для всех записей

        static Log()
        {        
            // Конфигурация нашего логгера
            _logger = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Information)
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", 
                    rollingInterval: RollingInterval.Day, 
                    retainedFileCountLimit: 10) // Логи пишутся в файл с ротацией
                .CreateLogger();
        }

        public static void SetLogLevel(LogEventLevel level)
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10)
                .CreateLogger();
        }


        public static void Message(string text)
        {
            _logger.Information(text);
            Storage.AddLog(new LogEntry { Index = ++_logIndex, Level = "Message", Text = text, Timestamp = DateTime.Now });
        }

        public static void Error(string text)
        {
            _logger.Error(text);
            var entry = new LogEntry
            {
                Index = ++_logIndex,
                Level = "Error",
                Text = text,
                Timestamp = DateTime.Now
            };
            Storage.AddLog(entry);
            Storage.AddErrorLog(entry);
        }

        public static void Ok(string text)
        {
            _logger.Information($"SUCCESS: {text}");
            Storage.AddLog(new LogEntry { Index = ++_logIndex, Level = "Ok", Text = text, Timestamp = DateTime.Now });
        }
    }

}
