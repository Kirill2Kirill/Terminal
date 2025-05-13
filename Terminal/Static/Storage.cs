using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Models;

namespace Terminal.Static
{
    public static class Storage
    {

        public static readonly SynchronizedBindingList<LogEntry> Logs = new SynchronizedBindingList<LogEntry>();
        public static readonly SynchronizedBindingList<LogEntry> ErrorLogs = new SynchronizedBindingList<LogEntry>();

        private static readonly object LogsLock = new object();
        private static readonly object ErrorLogsLock = new object();

        public static void AddLog(LogEntry logEntry)
        {
            lock (LogsLock)
            {
                Logs.Insert(0, logEntry);
            }
        }

        public static void AddErrorLog(LogEntry logEntry)
        {
            lock (ErrorLogsLock)
            {
                ErrorLogs.Insert(0, logEntry);
            }
        }

        public static List<LogEntry> GetLogsSnapshot()
        {
            lock (LogsLock)
            {
                return new List<LogEntry>(Logs);
            }
        }

        public static List<LogEntry> GetErrorLogsSnapshot()
        {
            lock (ErrorLogsLock)
            {
                return new List<LogEntry>(ErrorLogs);
            }
        }
    }
}
        
