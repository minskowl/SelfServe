﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SelfServe
{
    public class HttpServer
    {
        public const string DEFAULT_PREFIX = "http://+/";

        HttpListener Listener;
        string StartUpPath;
        bool IsRunning;

        public HttpServer(string[] prefixes)
        {
            if (prefixes == null || prefixes.Length == 0)
            {
                throw new ArgumentException("The prefixes argument cannot be empty! Use the default constructor if you are stuck.");
            }

            Listener = new HttpListener();
            StartUpPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            IsRunning = false;

            foreach (string prefix in prefixes)
            {
                Listener.Prefixes.Add(prefix);
            } 
        }

        public HttpServer()
            : this(new string[] { DEFAULT_PREFIX })
        {
        }

        public void Start()
        {
            Listener.Start();
            IsRunning = true;
            new TaskFactory().StartNew(BeginLoop);

            Console.WriteLine("OK, server is ready!");
        }

        private void BeginLoop()
        {
            while (IsRunning)
            {
                ThreadPool.QueueUserWorkItem(Process, Listener.GetContext());
            }
        }

        public void Stop()
        {
            IsRunning = false;

            Console.WriteLine("Server has stopped!");
        }

        protected virtual void Process(object o)
        {
            var context = o as HttpListenerContext;
            string filename = Path.GetFileName(HttpUtility.UrlDecode(context.Request.RawUrl));
            string path = Path.Combine(StartUpPath, filename);

            if (!File.Exists(path))
            {
                Console.WriteLine(string.Format("Client requested file ({0})... Not found", filename));

                var error = "<!DOCTYPE HTML><html><head></head><body><h1>File Not Found</h1></body></html>";
                WriteError(context, error);
            }
            else
            {
                Console.WriteLine(string.Format("Client requested file ({0})... Found", filename));

                var file = File.ReadAllBytes(path);
                WriteFile(context, file);
            }
        }

        protected virtual void WriteFile(HttpListenerContext context, byte[] file, HttpStatusCode status = HttpStatusCode.OK)
        {
            context.Response.StatusCode = (int)status;
            context.Response.ContentLength64 = file.Length;
            using (Stream s = context.Response.OutputStream)
            {
                s.Write(file, 0, file.Length);
            }

            context.Response.Close();
        }

        protected virtual void WritePage(HttpListenerContext context, string output, HttpStatusCode status = HttpStatusCode.OK)
        {
            context.Response.KeepAlive = true;
            context.Response.ContentType = "text/html; charset=UTF-8";
            WriteFile(context, GetBytes(output), status);
        }

        protected virtual void WriteError(HttpListenerContext context, string error)
        {
            WritePage(context, error, HttpStatusCode.InternalServerError);
        }

        protected virtual byte[] GetBytes(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str); ;
            return bytes;
        }
    }
}