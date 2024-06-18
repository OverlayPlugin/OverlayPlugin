using System;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin
{
    /// <summary>
    /// ログを記録する機能を提供するクラス。
    /// </summary>
    public class Logger : ILogger, ITinyIoCAutoConstructPreInit<ILogger>, ITinyIoCAutoConstructPreInit<Logger>
    {
        public const int MaxEntries = 1000;

        public event EventHandler<IReadOnlyCollection<LogEntry>> OnLog;

        private long CurrentID = 0;

        /// <summary>
        /// 記録されたログを取得します。
        /// </summary>
        private List<LogEntry> Logs = new List<LogEntry>();

        public Logger()
        {
        }

        /// <summary>
        /// メッセージを指定してログを記録します。
        /// </summary>
        /// <param name="level">ログレベル。</param>
        /// <param name="message">メッセージ。</param>
        public void Log(LogLevel level, string message)
        {
#if !DEBUG
            if (level == LogLevel.Trace || level == LogLevel.Debug)
            {
                return;
            }
#endif
#if DEBUG
            System.Diagnostics.Trace.WriteLine(string.Format("{0}: {1}: {2}", level, DateTime.Now, message));
#endif

            var entry = new LogEntry(CurrentID++, level, DateTime.Now, message);

            Logs.Add(entry);

            while (Logs.Count > MaxEntries)
            {
                Logs.RemoveAt(0);
            }

            RefreshLogs();
        }

        /// <summary>
        /// 書式指定子を用いたメッセージを指定してログを記録します。
        /// </summary>
        /// <param name="level">ログレベル。</param>
        /// <param name="format">複合書式指定文字列。</param>
        /// <param name="args">書式指定するオブジェクト。</param>
        public void Log(LogLevel level, string format, params object[] args)
        {
            Log(level, string.Format(format, args));
        }

        public void RefreshLogs()
        {
            OnLog?.Invoke(this, new List<LogEntry>(Logs));
        }
    }
}
