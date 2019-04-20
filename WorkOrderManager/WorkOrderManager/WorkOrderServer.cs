using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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
			string filename = context.Request.Url.AbsolutePath;
			Console.WriteLine(filename);
			filename = filename.Substring(1);

			if (string.IsNullOrEmpty(filename))
			{
				Console.WriteLine("HELLOOOOOOOOOOOOOO!");
			}

			filename = Path.Combine(_rootDirectory, filename);

			if (File.Exists(filename))
			{
				try
				{
					Stream input = new FileStream(filename, FileMode.Open);

					//Adding permanent http response headers
					context.Response.ContentType = "application/json";
					context.Response.ContentLength64 = input.Length;
					context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
					context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));

					byte[] buffer = new byte[1024 * 16];
					int nbytes;
					while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
						context.Response.OutputStream.Write(buffer, 0, nbytes);
					input.Close();

					context.Response.StatusCode = (int)HttpStatusCode.OK;
					context.Response.OutputStream.Flush();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
					context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
				}

			}
			else
			{
				context.Response.StatusCode = (int)HttpStatusCode.NotFound;
			}

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
