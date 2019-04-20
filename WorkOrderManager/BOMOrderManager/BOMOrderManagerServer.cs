using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using WorkOrderManager;

namespace BOMOrderManager
{
	internal class BOMOrderManagerServer
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
		public BOMOrderManagerServer(string path, int port)
		{
			this.Initialize(path, port);
		}

		/// <summary>
		/// Construct server with suitable port.
		/// </summary>
		/// <param name="path">Directory path to serve.</param>
		public BOMOrderManagerServer(string path)
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
			_listener.Stop();
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
			if(int.Parse(context.Request.Headers["source"]) == (int)Message.ApprovedEndpoint.WorkOrderManager)
			{
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
				string s = reader.ReadToEnd();
				Console.WriteLine(s);
				Console.WriteLine("End of client data:");
			}
			//Adding permanent http response headers
			context.Response.ContentType = "application/json";
			context.Response.ContentLength64 = 0;
			context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
			
			

			context.Response.StatusCode = (int)HttpStatusCode.OK;
			context.Response.OutputStream.Flush();
			context.Response.OutputStream.Close();
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
