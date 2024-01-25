using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;

namespace RainbowMage.OverlayPlugin.Updater
{
    public static class HttpClientWrapper
    {
        public delegate bool ProgressInfoCallback(long resumed, long dltotal, long dlnow, long ultotal, long ulnow);
        private static readonly HttpClient client = new HttpClient();
        static HttpClientWrapper()
        {
            client.DefaultRequestHeaders.Add("User-Agent", "OverlayPlugin/OverlayPlugin v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }

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
            var completionLock = new object();
            string result = null;
            Exception error = null;
            var retry = false;

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get,
            };

            foreach (var key in headers.Keys)
            {
                request.Headers.Add(key, headers[key]);
            }

            Action action = (async () =>
            {
                try
                {
                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    if (downloadDest == null)
                    {
                        result = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        var buffer = new byte[40 * 1024];
                        long length = 0;

                        long? nLength = response.Content.Headers.ContentLength;
                        if (nLength.HasValue)
                        {
                            length = nLength.Value;
                        }

                        using (var writer = File.Open(downloadDest, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                        using (var body = await response.Content.ReadAsStreamAsync())
                        {
                            var stop = false;
                            var pos = 0;
                            while (!stop)
                            {
                                var read = await body.ReadAsync(buffer, 0, buffer.Length);
                                if (read != 0)
                                {
                                    writer.Write(buffer, 0, read);
                                    pos += read;
                                }
                                else
                                    break;

                                if (infoCb != null && infoCb(0, length, pos, 0, 0))
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
                throw new HttpClientException(retry, error.Message, error);
            }
            return result;
        }
    }

    [Serializable]
    public class HttpClientException : Exception
    {
        public readonly bool Retry;

        public HttpClientException(bool retry, string message) : base(message)
        {
            this.Retry = retry;
        }

        public HttpClientException(bool retry, string message, Exception innerException) : base(message, innerException)
        {
            this.Retry = retry;
        }
    }
}
