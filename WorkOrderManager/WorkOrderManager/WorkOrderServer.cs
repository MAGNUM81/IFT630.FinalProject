﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

//Strongly inspired from https://gist.github.com/aksakalli/9191056

namespace WorkOrderManager
{
	class WorkOrderServer
	{
		private Thread _serverThread;
		private string _rootDirectory;
		private HttpListener _listener;
		private int _port;

		public int Port
		{
			get { return _port; }
			private set { }
		}

		/// <summary>
		/// Construct server with given port.
		/// </summary>
		/// <param name="path">Directory path to serve.</param>
		/// <param name="port">Port of the server.</param>
		public WorkOrderServer(string path, int port)
		{
			this.Initialize(path, port);
		}

		/// <summary>
		/// Construct server with suitable port.
		/// </summary>
		/// <param name="path">Directory path to serve.</param>
		public WorkOrderServer(string path)
		{
			//get an empty port
			TcpListener l = new TcpListener(IPAddress.Loopback, 0);
			l.Start();
			int port = ((IPEndPoint)l.LocalEndpoint).Port;
			l.Stop();
			this.Initialize(path, port);
		}

		/// <summary>
		/// Stop server and dispose all functions.
		/// </summary>
		public void Stop()
		{
			_serverThread.Abort();
			_listener?.Stop();
		}

		private void Listen()
		{
			_listener = new HttpListener();
			_listener.Prefixes.Add("http://127.0.0.1:" + _port.ToString() + "/");
			_listener.Start();
			while (true)
			{
				try
				{
					HttpListenerContext context = _listener.GetContext();
					Process(context);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
		}

		private void Process(HttpListenerContext context)
		{
			var head = context.Request.Headers;
			var src = context.Request.Headers["source"];
			int isrc = int.Parse(src);
			var request = context.Request;
			System.IO.Stream body = request.InputStream;
			System.Text.Encoding encoding = request.ContentEncoding;
			System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);
			if (request.ContentType != null)
			{
				Console.WriteLine("Client data content type {0}", request.ContentType);
			}
			Console.WriteLine("Client data content length {0}", request.ContentLength64);

			Console.WriteLine("Start of client data:");
			// Convert the data to a string and display it on the console.
			string all = reader.ReadToEnd();
			Console.WriteLine(all);
			Console.WriteLine("End of client data:");

			if (isrc == (int)Message.ApprovedEndpoint.Carrier)
			{
			}

			//Adding permanent http response headers
			context.Response.ContentType = "application/json";
			
			context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
			byte[] buffer = new byte[1024 * 16];
			context.Response.OutputStream.BeginWrite(buffer, 0, buffer.Length, finishedWriteCallBack, context);
		}

		private static void finishedWriteCallBack(IAsyncResult result)
		{
			var ctx = (HttpListenerContext) result.AsyncState;
			ctx.Response.OutputStream.EndWrite(result);
			ctx.Response.OutputStream.Flush();
			ctx.Response.OutputStream.Close();
		}

		private void Initialize(string path, int port)
		{
			this._rootDirectory = path;
			this._port = port;
			_serverThread = new Thread(this.Listen);
			_serverThread.Start();
		}
	}
}