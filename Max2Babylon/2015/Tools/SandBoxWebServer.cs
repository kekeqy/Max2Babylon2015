using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Max2Babylon
{
    public class SandBoxWebServer
    {
        private static Dictionary<string, string> mimeTypes = new Dictionary<string, string>();
        private static readonly HttpListener listener;
        private static Task runningTask;

        public const int Port = 45477;
        public const string prefix = "http://localhost:45477/";

        public static bool IsSupported { get; private set; }
        static string GetMime(string ext)
        {
            if (mimeTypes.ContainsKey(ext)) return mimeTypes[ext];
            return "application/octet-stream";
        }

        static SandBoxWebServer()
        {
            mimeTypes.Add(".html", "text/html");
            mimeTypes.Add(".ico", "image/vnd.microsoft.icon");
            mimeTypes.Add(".js", "text/javascript");
            mimeTypes.Add(".png", "image/png");
            mimeTypes.Add(".svg", "image/svg+xml");
            mimeTypes.Add(".woff", "font/woff");
            mimeTypes.Add(".css", "text/css");
            try
            {
                listener = new HttpListener();

                if (!HttpListener.IsSupported)
                {
                    IsSupported = false;
                    return;
                }

                listener.Prefixes.Add(prefix);
                listener.Start();


                runningTask = Task.Run(() => Listen());

                IsSupported = true;
            }
            catch
            {
                IsSupported = false;
            }
        }

        public static string SceneFolder
        {
            get
            {
                return string.Format("{0}\\bin\\assemblies\\sandbox\\", Application.StartupPath);
            }
        }
        static Random r = new Random();
        
        static void Listen()
        {
            try
            {
                while (listener.IsListening)
                {
                    var context = listener.GetContext();
                    var request = context.Request;
                    var url = request.Url;

                    context.Response.AddHeader("Cache-Control", "no-cache");
                    context.Response.AppendHeader("Access-Control-Allow-Origin", "*");  // Allow CROS

                    try
                    {
                        var path = Path.Combine(SceneFolder, HttpUtility.UrlDecode(url.PathAndQuery.Substring(1)));
                        var questionMarkIndex = path.IndexOf("?");
                        if (questionMarkIndex != -1)
                        {
                            path = path.Substring(0, questionMarkIndex);
                        }
                        var hashIndex = path.IndexOf("#");
                        if (hashIndex != -1)
                        {
                            path = path.Substring(0, hashIndex);
                        }
                        string ext = Path.GetExtension(path).ToLower();
                        context.Response.ContentType = GetMime(ext);
                        var buffer = File.ReadAllBytes(path);
                        WriteResponse(context, buffer);
                    }
                    catch
                    {
                        context.Response.StatusCode = 404;
                        context.Response.Close();
                    }
                }
            }
            catch
            {
            }
        }

        static void WriteResponse(HttpListenerContext context, string s)
        {
            WriteResponse(context.Response, s);
        }

        static void WriteResponse(HttpListenerContext context, byte[] buffer)
        {
            WriteResponse(context.Response, buffer);
        }

        static void WriteResponse(HttpListenerResponse response, string s)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(s);
            WriteResponse(response, buffer);
        }

        static void WriteResponse(HttpListenerResponse response, byte[] buffer)
        {
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }
    }
}