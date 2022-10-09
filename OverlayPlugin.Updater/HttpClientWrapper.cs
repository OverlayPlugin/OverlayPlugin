using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Http;
using System.Reflection;
using System.IO;

namespace RainbowMage.OverlayPlugin.Updater
{
    public static class HttpClientWrapper
    {
        public delegate bool ProgressInfoCallback(long resumed, long dltotal, long dlnow, long ultotal, long ulnow);

        public static void Init(string pluginDirectory)
        {
            // Nothing to do here
        }

        public static string Get(string url)
        {
            return Get(url, new Dictionary<string, string>(), null, null, false);
        }

        public static string Get(string url, Dictionary<string, string> headers, string downloadDest,
            ProgressInfoCallback infoCb, bool resume)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "OverlayPlugin/OverlayPlugin v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());

            foreach (var key in headers.Keys)
            {
                client.DefaultRequestHeaders.Add(key, headers[key]);
            }

            var completionLock = new object();
            string result = null;
            Exception error = null;
            var retry = false;

            Action action = (async () =>
            {
                try
                {
                    var response = await client.GetAsync(url);

                    if (downloadDest == null)
                    {
                        result = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        var buffer = new byte[40 * 1024];
                        var length = 0;

                        IEnumerable<string> lengthValues;
                        if (response.Headers.TryGetValues("Content-Length", out lengthValues))
                        {
                            int.TryParse(lengthValues.GetEnumerator().Current, out length);
                        }

                        using (var writer = File.Open(downloadDest, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                        // FIXME: ReadAsStreamAsync() waits until the download finishes before returning.
                        //        This breaks progress reporting and makes it impossible to abort running downloads.
                        using (var body = await response.Content.ReadAsStreamAsync())
                        {
                            var stop = false;
                            while (!stop)
                            {
                                var read = body.Read(buffer, 0, buffer.Length);
                                if (read == 0)
                                    break;

                                writer.Write(buffer, 0, read);
                                if (infoCb(0, length, body.Position, 0, 0))
                                    break;
                            }
                        }
                    }
                }
                catch (IOException e)
                {
                    error = e;
                    retry = true;
                }
                catch (UnauthorizedAccessException e)
                {
                    error = e;
                    retry = true;
                }
                catch (HttpRequestException e)
                {
                    error = e;
                    retry = true;
                }
                catch (Exception e)
                {
                    error = e;
                }

                lock (completionLock)
                {
                    Monitor.Pulse(completionLock);
                }
            });
            action();

            lock (completionLock)
            {
                Monitor.Wait(completionLock);
            }

            if (error != null)
            {
                throw new CurlException(retry, error.Message, error);
            }
            return result;
        }
    }
}
