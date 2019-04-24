using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WorkOrderManager;

namespace BOMOrderManager
{
	internal class BOMOrderManagerServer
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
		public BOMOrderManagerServer(string path, int port)
		{
			Initialize(path, port);
		}

		/// <summary>
		///     Construct server with suitable port.
		/// </summary>
		/// <param name="path">Directory path to serve.</param>
		public BOMOrderManagerServer(string path)
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
			var request = context.Request;
			var body = request.InputStream;
			var encoding = request.ContentEncoding;
			var reader = new StreamReader(body, encoding);
			if (request.ContentType != null) Console.WriteLine("Client data content type {0}", request.ContentType);
			Console.WriteLine("Client data content length {0}", request.ContentLength64);

			Console.WriteLine("Start of client data:");
			// Convert the data to a string and display it on the console.
			var all = reader.ReadToEnd();
			Console.WriteLine(all);
			Console.WriteLine("End of client data:");
			var m = Message.FromJson(all);
			var strResponse = "";
			var isrc = (int) m.source;
			if (isrc == (int) Message.ApprovedEndpoint.WorkOrderManager)
			{
				//Create a response


				var thread = new Thread(() => ProcessBOMOrderAndForwardToCarrier(m));
				thread.Start();

				m.action = Message.NetworkAction.Echo;
				strResponse = Message.ToJson(m);

				context.Response.Headers.Add("Content", strResponse);
				context.Response.ContentLength64 = strResponse.Length;
				context.Response.StatusCode = (int) HttpStatusCode.OK;
			}
			else if (isrc == (int) Message.ApprovedEndpoint.Carrier)
			{
				//When we get the delivery
				//We Validate the package with the WorkOrderManager to see if it corresponds to the WorkOrder
				//We send it back to the Carrier, but this time to deliver it to the ProductionArea
			}

			//Adding permanent http response headers
			context.Response.ContentType = "application/json";
			context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
			context.Response.ContentEncoding = encoding;
			var buffer = encoding.GetBytes(strResponse);
			context.Response.ContentLength64 = buffer.Length;
			context.Response.OutputStream.BeginWrite(buffer, 0, buffer.Length, finishedWriteCallBack, context);
		}

		private static void finishedWriteCallBack(IAsyncResult result)
		{
			var ctx = (HttpListenerContext) result.AsyncState;
			ctx.Response.OutputStream.EndWrite(result);
			ctx.Response.OutputStream.Flush();
			ctx.Response.OutputStream.Close();
		}

		private void ProcessBOMOrderAndForwardToCarrier(Message m)
		{
			//Parse the content of the message to a WorkOrder
			var wo = new WorkOrder();
			try
			{
				wo = WorkOrder.FromJson(m.content);
			}
			catch (Exception e)
			{
				Console.WriteLine(
					"The content of the received message probably did not match the format of a WorkOrder");
				Console.WriteLine("Your message will not be sent. E v e r. Get your **** together fam.");
				Console.WriteLine("Error msg: {0}", e.Message);
				Thread.CurrentThread.Abort();
			}

			var deliveryOrder = makeDeliveryOrderFromWorkOrder(wo);

			//Parse the DeliveryOrder as JSON
			var strDelivery = DeliveryOrder.ToJson(deliveryOrder);

			//Pack it in a new Message
			//And set Message flags
			var getSent = new Message
			{
				content = strDelivery,
				action = Message.NetworkAction.Delivery,
				source = Message.ApprovedEndpoint.BOMWarehouse,
				destination = Message.ApprovedEndpoint.Carrier
			};

			//GET SENT (#POSTED)
			SendToCarrier(getSent);
		}

		private static DeliveryOrder makeDeliveryOrderFromWorkOrder(WorkOrder wo)
		{
			//Open the recipe file
			var RefFileContent = File.ReadAllText(Program.refPath);
			var RefRecipes =
				JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, uint>>>(RefFileContent);

			var deliveryOrder = new DeliveryOrder(wo.idWorkOrder);

			//Fetch all products required
			foreach (var product in wo.RequiredProducts)
				//For each product, fetch its recipe from the reference JSON file
				//	If there is no recipe for that product in the reference file, assume the product is a BOM and forward it anyways
				if (RefRecipes.ContainsKey(product.Key))
					foreach (var recipe in RefRecipes[product.Key])
						if (deliveryOrder.items.ContainsKey(recipe.Key))
							deliveryOrder.items[recipe.Key] +=
								recipe.Value; //We use += because some items may need a certain amount of the same BOM
						else
							deliveryOrder.items[recipe.Key] = recipe.Value;
				else
					deliveryOrder.items[product.Key] += product.Value;

			return deliveryOrder;
		}

		private static async void SendToCarrier(Message m)
		{
			try
			{
				var response = await HttpClientLayer.getInstance().Post("http://127.0.0.1:8082/", m);
				Console.WriteLine("Got a response from the Carrier! Content : {0}", response.content);
			}
			catch (TaskCanceledException tce)
			{
				Console.WriteLine(tce.Message);
				Console.WriteLine("Something went wrong while communicating with the Carrier.");
				Console.WriteLine("The Carrier might be down or unstable right now.");
				Console.WriteLine("Feel free to try again later!");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
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