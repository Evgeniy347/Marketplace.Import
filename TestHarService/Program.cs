using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace TestHarService
{
    internal class Program
    {
        public static async Task HandleIncomingConnections(HttpListener listener, string baseFolder)
        {
            bool runServer = true;

            HarFile file = HarFile.Parce(@"C:\Users\GTR\Downloads\seller_ozon_ru (1).har");

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;
                Console.WriteLine(req.Url?.ToString());
                await WriteLocalFile(ctx, baseFolder, file);

                resp.Close();
            }
        }

        private static async Task WriteLocalFile(HttpListenerContext context, string baseFolder, HarFile file)
        {
            HttpListenerResponse resp = context.Response;
            string path = context.Request.Url?.LocalPath?.Replace("/", "\\").Trim('\\');

            if (string.IsNullOrEmpty(path))
                path = "default.html";

            if (string.IsNullOrEmpty(path) || !File.Exists(Path.Combine(baseFolder, path)))
            {
                HarEntry entry = file.Entries.FirstOrDefault(x => x.Request.Url.LocalPath == context.Request.Url?.LocalPath);

                if (entry != null)
                {
                    foreach (var header in entry.Responce.Headers)
                    {
                        if (header.Key.Equals("content-length", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("Date", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("Server", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.StartsWith("X-O3", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.StartsWith("access", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("vary", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("alt-svc", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.StartsWith("cf", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("server-timing", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("trailer", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("content-encoding", StringComparison.OrdinalIgnoreCase))
                            continue;

                        resp.Headers.Add(header.Key, header.Value);
                    }

                    byte[] data = entry.Responce.Content;
                    resp.ContentLength64 = data.Length;
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                }

                resp.StatusCode = 404;
                return;
            }
            else
            {
                string fileName = Path.Combine(baseFolder, path);
                string content = File.ReadAllText(fileName);

                string extenson = Path.GetExtension(fileName);

                switch (extenson)
                {
                    case ".json":
                        resp.ContentType = "application/json";
                        break;
                    case ".js":
                        resp.ContentType = "application/javascript";
                        break;
                    case ".html":
                    default:
                        resp.ContentType = "text/html";
                        break;
                }

                resp.ContentEncoding = Encoding.UTF8;

                byte[] data = resp.ContentEncoding.GetBytes(content);
                resp.ContentLength64 = data.Length;

                await resp.OutputStream.WriteAsync(data, 0, data.Length);
            }
        }

        public static void Main(string[] args)
        {
            HttpListener listener;
            string url = "http://localhost:8080/";

            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);


            // Handle requests
            Task listenTask = HandleIncomingConnections(listener, Directory.GetCurrentDirectory());
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }
    }
}
