using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.EventSources
{
    using FilesList = Dictionary<string, Dictionary<string, string>>;

    partial class FileSystemEventSource : EventSourceBase
    {
        private const string ChangedEvent = "FileSystemChanged";
        private const string WatchChangesEvent = "FileSystemWatchChanges";
        private const string SaveFilesEvent = "FileSystemSaveFiles";
        private const string LoadFilesEvent = "FileSystemLoadFiles";

        public BuiltinEventConfig Config { get; set; }

        private readonly string AbsoluteBasePath;

        // TODO: Maybe we need to whitelist more extensions?
        private static readonly List<string> AllowedFileExtensions = new List<string>() {
            ".txt",
            ".js",
            ".json",
            ".mp3",
            ".mp4",
            ".wav",
            ".png",
            ".jpg",
            ".webp",
            ".webm",
            ".yml",
            ".xml",
            ".ogg",
            ".html",
            ".css",
            ".ogg",
        };

        public string GetSafePath(string inPath)
        {
            var absolutePath = Path.GetFullPath(inPath);
            if (!absolutePath.StartsWith(AbsoluteBasePath))
                return null;

            return absolutePath;
        }

        public FileSystemEventSource(TinyIoCContainer container) : base(container)
        {
            AbsoluteBasePath = Path.GetFullPath(Path.Combine(
                ActGlobals.oFormActMain.AppDataFolder.FullName,
                "Config",
                "OverlayPluginFileSystem"));

            Name = "FileSystem";

            RegisterEventType(ChangedEvent);
            RegisterEventHandler(WatchChangesEvent, HandleWatchChangesEvent);

            RegisterEventHandler(SaveFilesEvent, HandleSaveFilesEvent);

            RegisterEventHandler(LoadFilesEvent, HandleLoadFilesEvent);
        }

        private readonly Dictionary<string, KeyValuePair<string, FileSystemWatcher>> Watchers = new Dictionary<string, KeyValuePair<string, FileSystemWatcher>>();

        private void RegisterWatch(string dir, string path)
        {
            if (Watchers.ContainsKey(dir))
            {
                return;
            }

            var watcher = new FileSystemWatcher(path);
            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            Watchers.Add(dir, new KeyValuePair<string, FileSystemWatcher>(path, watcher));

            // Prevent spamming events
            CancellationTokenSource cancellationToken = null;

            FileSystemEventHandler handler = (object s, FileSystemEventArgs e) =>
            {
                if (cancellationToken != null)
                {
                    cancellationToken.Cancel();
                }
                cancellationToken = new CancellationTokenSource();

                Task.Delay(5000, cancellationToken.Token).ContinueWith((t) =>
                {
                    DispatchFileSystemChangedEvent();
                }, cancellationToken.Token);
            };

            watcher.Changed += handler;
            watcher.Created += handler;
            watcher.Deleted += handler;
            watcher.Renamed += (s, e) => handler(s, null);

            watcher.Filter = "*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        private void DispatchFileSystemChangedEvent()
        {
            var msg = new ChangedEventMessage()
            {
                directories = new FilesList(),
            };

            foreach (var entry in Watchers)
            {
                var fileList = new Dictionary<string, string>();

                var path = entry.Value.Key;

                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    if (!AllowedFileExtensions.Contains(Path.GetExtension(file)))
                    {
                        continue;
                    }

                    var relFile = file.Replace(path, "").TrimStart(Path.DirectorySeparatorChar);
                    fileList.Add(relFile, File.ReadAllText(file));
                }

                msg.directories.Add(entry.Key, fileList);
            }

            DispatchEvent(JObject.FromObject(msg));
        }

        private JToken HandleWatchChangesEvent(JObject msg)
        {
            var dirs = msg.ToObject<WatchChangesRequestMessage>();

            foreach (var dir in dirs.directories)
            {
                var absolutePath = GetSafePath(Path.Combine(AbsoluteBasePath, dir));
                if (absolutePath == null)
                {
                    return JToken.FromObject(new WatchChangesResponseMessage()
                    {
                        success = false,
                        message = $"Invalid base path {dir}",
                    });
                }
                if (!Directory.Exists(absolutePath))
                {
                    return JToken.FromObject(new WatchChangesResponseMessage()
                    {
                        success = false,
                        message = $"Directory does not exist: {dir}",
                    });
                }

                RegisterWatch(dir, absolutePath);
            }

            return JToken.FromObject(new WatchChangesResponseMessage()
            {
                success = true,
            });
        }

        private JToken HandleSaveFilesEvent(JObject msg)
        {
            var map = msg.ToObject<SaveFilesRequestMessage>();

            foreach (var entry in map.directories)
            {
                var basepath = entry.Key;
                var files = entry.Value;
                var absoluteBasePath = GetSafePath(Path.Combine(AbsoluteBasePath, basepath));
                if (absoluteBasePath == null)
                {
                    return JToken.FromObject(new SaveFilesResponseMessage()
                    {
                        success = false,
                        message = $"Invalid base path {basepath}",
                    });
                }

                foreach (var filePair in files)
                {
                    var filePath = filePair.Key;
                    var fileData = filePair.Value;
                    var absoluteFilePath = GetSafePath(Path.Combine(absoluteBasePath, filePath));
                    if (absoluteFilePath == null)
                    {
                        return JToken.FromObject(new SaveFilesResponseMessage()
                        {
                            success = false,
                            message = $"Invalid file path {filePath}",
                        });
                    }

                    var fileExtension = Path.GetExtension(absoluteFilePath);

                    if (!AllowedFileExtensions.Contains(fileExtension))
                    {
                        return JToken.FromObject(new SaveFilesResponseMessage()
                        {
                            success = false,
                            message = $"Invalid file extension for file {filePath}",
                        });
                    }

                    var parentDir = Path.GetDirectoryName(absoluteFilePath);

                    if (!Directory.Exists(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }

                    File.WriteAllText(absoluteFilePath, fileData);
                }
            }

            return JToken.FromObject(new SaveFilesResponseMessage()
            {
                success = true,
            });
        }

        private JToken HandleLoadFilesEvent(JObject msg)
        {
            var dirs = msg.ToObject<LoadFilesRequestMessage>();

            var respMsg = new LoadFilesResponseMessage();
            respMsg.success = true;
            respMsg.directories = new FilesList();

            foreach (var dir in dirs.directories)
            {
                var dirEntries = new Dictionary<string, string>();
                var absolutePath = GetSafePath(Path.Combine(AbsoluteBasePath, dir));
                if (absolutePath == null)
                {
                    return JToken.FromObject(new LoadFilesResponseMessage()
                    {
                        success = false,
                        message = $"Invalid base path {dir}",
                    });
                }
                if (!Directory.Exists(absolutePath))
                {
                    return JToken.FromObject(new LoadFilesResponseMessage()
                    {
                        success = false,
                        message = $"Directory does not exist: {dir}",
                    });
                }

                foreach (var file in Directory.EnumerateFiles(absolutePath, "*", SearchOption.AllDirectories))
                {
                    if (!AllowedFileExtensions.Contains(Path.GetExtension(file)))
                    {
                        continue;
                    }

                    var relFile = file.Replace(absolutePath, "").TrimStart(Path.DirectorySeparatorChar);
                    dirEntries.Add(relFile, File.ReadAllText(file));
                }

                respMsg.directories.Add(dir, dirEntries);
            }

            return JToken.FromObject(respMsg);
        }

        public override Control CreateConfigControl()
        {
            return null;
        }

        public override void LoadConfig(IPluginConfig config)
        {
        }

        public override void SaveConfig(IPluginConfig config)
        {
        }

        public override void Start()
        {
        }

        protected override void Update()
        {
        }

        public class SaveFilesResponseMessage
        {
            public string type = SaveFilesEvent;
            public string message;
            public bool success;
        }

        public class LoadFilesResponseMessage
        {
            public string type = LoadFilesEvent;
            public string message;
            public bool success;
            public FilesList directories;
        }

        public class WatchChangesResponseMessage
        {
            public string type = WatchChangesEvent;
            public string message;
            public bool success;
        }

        public class ChangedEventMessage
        {
            public string type = ChangedEvent;
            public FilesList directories;
        }

        public class WatchChangesRequestMessage
        {
            public string call = LoadFilesEvent;
#pragma warning disable 0649
            public List<string> directories;
#pragma warning restore 0649
        }

        public class LoadFilesRequestMessage
        {
            public string call = LoadFilesEvent;
#pragma warning disable 0649
            public List<string> directories;
#pragma warning restore 0649
        }

        public class SaveFilesRequestMessage
        {
            public string call = SaveFilesEvent;
#pragma warning disable 0649
            public FilesList directories;
#pragma warning restore 0649
        }
    }
}
