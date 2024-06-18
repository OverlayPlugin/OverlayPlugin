using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.OverlayPlugin
{
    public interface ILogger
    {
        event EventHandler<IReadOnlyCollection<LogEntry>> OnLog;
        void Log(LogLevel level, string message);
        void Log(LogLevel level, string format, params object[] args);
        void RefreshLogs();
    }

    public class LogEntry
    {
        public long ID { get; set; }
        public string Message { get; set; }
        public LogLevel Level { get; set; }
        public DateTime Time { get; set; }

        public LogEntry(long id, LogLevel level, DateTime time, string message)
        {
            ID = id;
            Message = message;
            Level = level;
            Time = time;
        }

        public string ToRtfString()
        {
            var cf = 1 + ((int)Level);
            if (Level == LogLevel.Info) cf = 0;
            if (Level == LogLevel.Warning || Level == LogLevel.Error) --cf;

            if (Level == LogLevel.Error)
            {
                return $"{{\\v{ID}}}\\cf{cf}[{Time}] {Level}: {EscapeRtf(Message)}\\cf0\\\n";
            }
            else
            {
                return $"{{\\v{ID}}}\\cf0[{Time}] \\cf{cf}{Level}\\cf0: {EscapeRtf(Message)}\\\n";
            }
        }

        private static string EscapeRtf(string msg)
        {
            return msg
                .Replace("\\", "\\\\")
                .Replace("{", "\\{")
                .Replace("}", "\\}")
                .Replace("\n", "\\\n");
        }

        public const string DefaultColorTable = @"{\colortbl"
                    + @";" // Default
                    + @"\red128\green128\blue128;" // Trace
                    + @"\red192\green192\blue192;" // Debug
                    + @"\red255\green165\blue0;" // Warning
                    + @"\red255\green0\blue0;}"; // Error
    }

    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error
    }
}
