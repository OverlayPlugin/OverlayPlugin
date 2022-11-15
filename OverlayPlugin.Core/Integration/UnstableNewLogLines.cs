using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Configuration;
using System.Text;
using System.Threading;
using Advanced_Combat_Tracker;
using Markdig.Helpers;
using RainbowMage.OverlayPlugin.EventSources;
using RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;
using RainbowMage.OverlayPlugin.NetworkProcessors;
using static RainbowMage.OverlayPlugin.MemoryProcessors.InCombat.LineInCombat;

namespace RainbowMage.OverlayPlugin.Integration
{
    public class UnstableNewLogLines
    {
        private bool inCutscene = false;
        private FFXIVRepository repo = null;
        private NetworkParser parser = null;
        private EnmityEventSource enmitySource = null;
        private ILogger logger = null;
        private string logPath = null;
        private ConcurrentQueue<string> logQueue = null;
        private Thread logThread = null;
        private LineInCombat lineInCombat = null;

        public UnstableNewLogLines(TinyIoCContainer container)
        {
            repo = container.Resolve<FFXIVRepository>();
            parser = container.Resolve<NetworkParser>();
            enmitySource = container.Resolve<EnmityEventSource>();
            logger = container.Resolve<ILogger>();
            logPath = Path.GetDirectoryName(ActGlobals.oFormActMain.LogFilePath) + "_OverlayPlugin.log";
            lineInCombat = container.Resolve<LineInCombat>();


            var config = container.Resolve<BuiltinEventConfig>();
            config.LogLinesChanged += (o, e) =>
            {
                if (config.LogLines)
                {
                    Enable();
                }
                else
                {
                    Disable();
                }
            };

            if (config.LogLines)
            {
                Enable();
            }
        }

        public void Enable()
        {
            parser.OnOnlineStatusChanged += OnOnlineStatusChange;
            lineInCombat.OnInCombatChanged += OnCombatStatusChange;

            logThread = new Thread(new ThreadStart(WriteBackgroundLog));
            logThread.IsBackground = true;
            logThread.Start();
        }

        public void Disable()
        {
            parser.OnOnlineStatusChanged -= OnOnlineStatusChange;
            lineInCombat.OnInCombatChanged -= OnCombatStatusChange;
            logQueue?.Enqueue(null);
        }

        private void WriteBackgroundLog()
        {
            try
            {
                logger.Log(LogLevel.Info, "LogWriter: Opening log file {0}.", logPath);
                var logFile = File.Open(logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                logQueue = new ConcurrentQueue<string>();

                while (true)
                {
                    if (logQueue.TryDequeue(out string line))
                    {
                        if (line == null) break;

                        var data = Encoding.UTF8.GetBytes(line + "\n");
                        logFile.Write(data, 0, data.Length);
                    }
                    else
                    {
                        Thread.Sleep(500);
                    }
                }

                logger.Log(LogLevel.Info, "LogWriter: Closing log.");
                logFile.Close();
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "LogWriter: {0}", ex);
                logQueue = null;
            }
        }

        public void WriteLogMessage(string msg)
        {
            var time = DateTime.Now;
            var lineParts = new string[] { "00", time.ToString(), "c0fe", "", "OPLine: " + msg, "" };
            var line = string.Join("|", lineParts);

            ActGlobals.oFormActMain.ParseRawLogLine(false, time, line);
            logQueue?.Enqueue(line);
        }

        private void OnOnlineStatusChange(object sender, OnlineStatusChangedArgs ev)
        {
            if (ev.Target != repo.GetPlayerID())
                return;

            var cutsceneStatus = ev.Status == 15;
            if (cutsceneStatus != inCutscene)
            {
                inCutscene = cutsceneStatus;
                string msg;

                if (cutsceneStatus)
                {
                    msg = "Entered cutscene";
                }
                else
                {
                    msg = "Left cutscene";
                }

                WriteLogMessage(msg);
            }
        }

        private void OnCombatStatusChange(object sender, InCombatArgs ev)
        {
            if (!ev.InGameCombatChanged)
            {
                return;
            }

            string msg;
            if (ev.InGameCombat)
            {
                msg = "Entered combat";
            }
            else
            {
                msg = "Left combat";
            }

            WriteLogMessage(msg);
        }
    }
}
