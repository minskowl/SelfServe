﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;

namespace SelfServe
{
    public class HttpServer : IDisposable
    {
        public const string WILDCARD_PREFIX = "http://+/";
        public event EventHandler<RequestReceivedArgs> RequestReceived;   
        protected readonly string RootPath;
        private readonly HttpListener Listener;

        public HttpServer(string[] prefixes = null, string rootPath = "")
        {
            if (prefixes == null || prefixes.Length == 0)
            {
                prefixes = new string[] { WILDCARD_PREFIX };
            }

            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = Environment.CurrentDirectory;
            }

            Listener = new HttpListener();
            prefixes.ToList().ForEach(x => Listener.Prefixes.Add(x));
            RootPath = rootPath;
        }

        public bool IgnoreWriteExceptions
        {
            get { return Listener.IgnoreWriteExceptions; }

            set { Listener.IgnoreWriteExceptions = value; }
        }

        public void Start()
        {
            Listener.Start();

            new Action(ListenerLoop).BeginInvoke(null, null);

            Log("OK, server is ready!");
        }

        private void ListenerLoop()
        {
            while (Listener.IsListening)
            {
                IAsyncResult result = Listener.BeginGetContext(new AsyncCallback(ListenerCallback), Listener);
                result.AsyncWaitHandle.WaitOne();
            }
        }

        private void ListenerCallback(IAsyncResult result)
        {
            if (Listener.IsListening)
            {
                HttpListener listener = (HttpListener)result.AsyncState;
                HttpListenerContext context = listener.EndGetContext(result);

                using (HttpListenerResponse response = context.Response)
                {
                    try
                    {
                        ProccessContext(context);
                    }
                    catch (Exception ex)
                    {
                        OnException(ex);
                    }
                }
            }
        }

        protected virtual void ProccessContext(HttpListenerContext context)
        {
            RequestReceived.Fire(this, new RequestReceivedArgs(context));
        }

        protected virtual void OnException(Exception ex)
        {
            Log("Exception has been caught... {0}", ex.Message);
        }

        protected virtual void Log(string message, params object[] args)
        {
            Console.WriteLine(string.Format(message, args));
        }

        public void Dispose()
        {
            Listener.Stop();
            Listener.Close();

            Log("Server has stopped!");
        }     
    }
}
