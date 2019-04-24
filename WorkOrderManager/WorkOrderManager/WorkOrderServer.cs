using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

//Strongly inspired from https://gist.github.com/aksakalli/9191056

namespace WorkOrderManager
{
	internal class WorkOrderServer
	{
		private HttpListener _listener;
		private int _port;
		private string _rootDirectory;
		private Thread _serverThread;

		/// <summary>
		///     Construct server with given port.
		/// </summary>
		/// <param name="path">Directory path to serve.</param>
		/// <param name="port">Port of the server.</param>
		public WorkOrderServer(string path, int port)
		{
			Initialize(path, port);
		}

		/// <summary>
		///     Construct server with suitable port.
		/// </summary>
		/// <param name="path">Directory path to serve.</param>
		public WorkOrderServer(string path)
		{
			//get an empty port
			var l = new TcpListener(IPAddress.Loopback, 0);
			l.Start();
			var port = ((IPEndPoint) l.LocalEndpoint).Port;
			l.Stop();
			Initialize(path, port);
		}

		public int Port
		{
			get => _port;
			private set { }
		}

		/// <summary>
		///     Stop server and dispose all functions.
		/// </summary>
		public void Stop()
		{
			_serverThread.Abort();
			_listener?.Stop();
		}

		private void Listen()
		{
			_listener = new HttpListener();
			_listener.Prefixes.Add("http://127.0.0.1:" + _port + "/");
			_listener.Start();
			while (true)
				try
				{
					var context = _listener.GetContext();
					Process(context);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
		}

		private void Process(HttpListenerContext context)
		{
			var head = context.Request.Headers;
			var request = context.Request;
			var body = request.InputStream;
			var encoding = request.ContentEncoding;
			var reader = new StreamReader(body, encoding);

			if (request.ContentType != null) Console.WriteLine("Client data content type {0}", request.ContentType);
			Console.WriteLine("Client data content length {0}", request.ContentLength64);

			Console.WriteLine("::Start of client data::");
			// Convert the data to a string and display it on the console.
			var all = reader.ReadToEnd();
			Console.WriteLine(all);
			Console.WriteLine("::End of client data::");
			var strResponse = all; //The basic way to respond is by echo'ing the content received.
			var received = Message.FromJson(all);
			var isrc = (int) received.source;
			if (isrc == (int) Message.ApprovedEndpoint.ProductionArea)
				if (received.action == Message.NetworkAction.Fetch)
				{
					//Extract the empty WO from the content
					var wo = WorkOrder.FromJson(received.content);
					//Start a job fetching the complete WO from the Manager and sending it to the ProductionArea
					new Thread(() => ProcessProductionAreaFetch(wo)).Start();
				}

			//Adding permanent http response headers
			context.Response.ContentType = "application/json";
			context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
			context.Response.ContentEncoding = encoding;
			var buffer = encoding.GetBytes(strResponse);
			context.Response.OutputStream.BeginWrite(buffer, 0, buffer.Length, FinishedWriteCallBack, context);
		}

		private static async void ProcessProductionAreaFetch(WorkOrder wo)
		{
			var idWO = wo.idWorkOrder;
			//Get the work order
			wo = Program.GetWorkOrder(idWO);
			//Create a message and package the work order in it.
			var m = new Message
			{
				action = Message.NetworkAction.Fetch,
				source = Message.ApprovedEndpoint.WorkOrderManager,
				destination = Message.ApprovedEndpoint.ProductionArea,
				content = WorkOrder.ToJson(wo)
			};

			//Send the message to the ProductionArea
			try
			{
				var response = await HttpClientLayer.getInstance().Post("http://127.0.0.1:8083/", m);
				Console.WriteLine("Received response: {0}", response.content);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private static async void ProcessFinalWarehouseFetch(WorkOrder wo)
		{
			var idWO = wo.idWorkOrder;
			//Close the work order
			Program.CloseWorkOrder(idWO);
			//Create a message and package the work order in it.
			var m = new Message
			{
				action = Message.NetworkAction.Validate,
				source = Message.ApprovedEndpoint.WorkOrderManager,
				destination = Message.ApprovedEndpoint.FinalWarehouse,

				content = WorkOrder.ToJson(wo)
			};

			//Send the message to the FinalWarehouse
			try
			{
				var response = await HttpClientLayer.getInstance().Post("http://127.0.0.1:8084/", m);
				Console.WriteLine("Received response: {0}", response.content);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private static async void ProcessFinalWarehouseClosingOrder(WorkOrder wo)
		{
			var idWO = wo.idWorkOrder;
			//Close the work order
			Program.CloseWorkOrder(idWO);
			//Create a message and package the work order in it.
			var m = new Message
			{
				action = Message.NetworkAction.Validate,
				source = Message.ApprovedEndpoint.WorkOrderManager,
				destination = Message.ApprovedEndpoint.FinalWarehouse,

				content = WorkOrder.ToJson(wo)
			};

			//Send the message to the FinalWarehouse
			try
			{
				var response = await HttpClientLayer.getInstance().Post("http://127.0.0.1:8084/", m);
				Console.WriteLine("Received response: {0}", response.content);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private static void FinishedWriteCallBack(IAsyncResult result)
		{
			var ctx = (HttpListenerContext) result.AsyncState;
			ctx.Response.OutputStream.EndWrite(result);
			ctx.Response.OutputStream.Flush();
			ctx.Response.OutputStream.Close();
		}

		private void Initialize(string path, int port)
		{
			_rootDirectory = path;
			_port = port;
			_serverThread = new Thread(Listen);
			_serverThread.Start();
		}
	}
}