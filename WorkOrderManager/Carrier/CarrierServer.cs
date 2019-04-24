using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WorkOrderManager;

namespace Carrier
{
	internal class CarrierServer
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
		public CarrierServer(string path, int port)
		{
			Initialize(path, port);
		}

		/// <summary>
		///     Construct server with suitable port.
		/// </summary>
		/// <param name="path">Directory path to serve.</param>
		public CarrierServer(string path)
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

		private void Initialize(string path, int port)
		{
			_rootDirectory = path;
			_port = port;
			_serverThread = new Thread(Listen);
			_serverThread.Start();
		}

		/// <summary>
		///     Stop server and dispose all functions.
		/// </summary>
		public void Stop()
		{
			_serverThread.Abort();
			_listener.Stop();
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
			var m = new Message();
			var strResponse = "";
			var isrc = int.MinValue;
			try
			{
				m = Message.FromJson(all);
				isrc = (int) m.source;
			}
			catch (Exception e)
			{
				Console.WriteLine("There was a problem parsing the message. Please feel free to try again later.");
				Console.WriteLine(e.Message);
			}


			if (isrc == (int) Message.ApprovedEndpoint.BOMOrderManager)
			{
				//Create a response
				var thread = new Thread(() => ProcessInitialDelivery(m));
				thread.Start();
				//If the thread fails, we will not know about it. Therefore the response has to be positive. So we don't have any fast way to notify the requester that the operation failed.
				//we could still validate some more data before starting the thread though.
				strResponse = Message.ToJson(m);
				context.Response.Headers.Add("Content", strResponse);
				context.Response.ContentLength64 = strResponse.Length;
				context.Response.StatusCode = (int) HttpStatusCode.OK;
			}
			else if (isrc == (int) Message.ApprovedEndpoint.BOMWarehouse)
			{
				//Here we are supposed to have received a valid DeliveryRequest that respects its WorkOrder's requirements
				//Create a response
				var thread = new Thread(() => ProcessAndDeliverToProductionArea(m));
				thread.Start();
				//If the thread fails, we will not know about it. Therefore the response has to be positive. So we don't have any fast way to notify the requester that the operation failed.
				//we could still validate some more data before starting the thread though.

				strResponse = Message.ToJson(m);
				context.Response.Headers.Add("Content", strResponse);
				context.Response.ContentLength64 = strResponse.Length;
				context.Response.StatusCode = (int) HttpStatusCode.OK;
			}
			else if (isrc == (int) Message.ApprovedEndpoint.ProductionArea)
			{
			}
			else
			{
				//isrc has an unknown value. Can't do anything with that.	
				var error = new Message();
				m.action = Message.NetworkAction.Error;
				m.source = Message.ApprovedEndpoint.Carrier;
				m.destination = Message.ApprovedEndpoint.Carrier;
				m.content = "Error. Your request was not formatted correctly.";
				strResponse = Message.ToJson(m);
				context.Response.ContentLength64 = strResponse.Length;
				context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
			}

			//Adding permanent http response headers
			context.Response.ContentType = "application/json";
			context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
			context.Response.ContentEncoding = encoding;
			var buffer = encoding.GetBytes(strResponse);
			context.Response.ContentLength64 = buffer.Length;
			context.Response.OutputStream.BeginWrite(buffer, 0, buffer.Length, finishedWriteCallBack, context);
		}

		private void ProcessAndDeliverToProductionArea(Message m)
		{
			//Normally called within a child thread. Do not call this in the main thread or it might block the server for an undefinite time
			//We don't need to parse anything here. Just forward the message to the ProductionArea with some new shiny flags.
			m.source = Message.ApprovedEndpoint.Carrier;
			m.destination = Message.ApprovedEndpoint.ProductionArea;

			//Simulate the processing time needed from the moment a validated order has arrived to the moment
			//	it is sent from the plant to the Production area.
			for (var i = 0; i < 3; ++i)
			{
				Console.WriteLine(i == 0 ? "Delivering validated order to Production Area ..." : "...");
				Thread.Sleep(1000);
			}


			SendToProductionArea(m);
		}

		private async void SendToProductionArea(Message m)
		{
			//Normally called within a child thread. Do not call this in the main thread or it might block the server for an undefinite time

			try
			{
				var response = await HttpClientLayer.getInstance().Post("http://127.0.0.1:8083/", m);
				Console.WriteLine("Got a response from the ProductionArea! Content : {0}", response.content);
			}
			catch (TaskCanceledException tce)
			{
				Console.WriteLine(tce.Message);
				Console.WriteLine("Something went wrong while communicating with the ProductionArea.");
				Console.WriteLine("The ProductionArea might be down or unstable right now.");
				Console.WriteLine("Feel free to try again later!");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private void finishedWriteCallBack(IAsyncResult result)
		{
			var ctx = (HttpListenerContext) result.AsyncState;
			ctx.Response.OutputStream.EndWrite(result);
			ctx.Response.OutputStream.Flush();
			ctx.Response.OutputStream.Close();
		}

		private void ProcessInitialDelivery(Message m)
		{
			//Normally called within a child thread. Do not call this in the main thread or it might block the server for an undefinite time

			//We don't need to parse anything here. Just forward the message to the BOMWarehouse with some new shiny flags
			m.destination = Message.ApprovedEndpoint.BOMWarehouse;
			m.source = Message.ApprovedEndpoint.Carrier;
			//Simulate the processing time needed from the moment an order has arrived to the moment
			//	it is sent from the plant to the warehouse.
			for (var i = 0; i < 3; ++i)
			{
				Console.WriteLine("Delivering to BOM warehouse for validation ...");
				Thread.Sleep(1000);
			}

			SendToBOMWarehouse(m);
		}

		private async void SendToBOMWarehouse(Message m)
		{
			//Normally called within a child thread. Do not call this in the main thread or it might block the server for an undefinite time

			try
			{
				var response = await HttpClientLayer.getInstance().Post("http://127.0.0.1:8081/", m);
				Console.WriteLine("Got a response from the BOMWarehouse! Content : {0}", response.content);
			}
			catch (TaskCanceledException tce)
			{
				Console.WriteLine(tce.Message);
				Console.WriteLine("Something went wrong while communicating with the BOMWarehouse.");
				Console.WriteLine("The BOMWarehouse might be down or unstable right now.");
				Console.WriteLine("Feel free to try again later!");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}
	}
}