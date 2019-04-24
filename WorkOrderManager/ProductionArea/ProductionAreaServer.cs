using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BOMOrderManager;
using WorkOrderManager;

namespace ProductionArea
{
	internal class ProductionAreaServer
	{
		private static readonly Dictionary<string, DeliveryOrder> IngressWarehouse =
			new Dictionary<string, DeliveryOrder>();

		private HttpListener _listener;
		private int _port;
		private string _rootDirectory;
		private Thread _serverThread;

		/// <summary>
		///     Construct server with given port.
		/// </summary>
		/// <param name="path">Directory path to serve.</param>
		/// <param name="port">Port of the server.</param>
		public ProductionAreaServer(string path, int port)
		{
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
			var m = new Message();
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

			var strResponse = "";
			switch (isrc)
			{
				case (int) Message.ApprovedEndpoint.Carrier:
				{
					//Create a response
					var thread = new Thread(() => ProcessCarrierRequest(m));
					thread.Start();
					//If the thread fails, we will not know about it. Therefore the response has to be positive. So we don't have any fast way to notify the requester that the operation failed.
					//we could still validate some more data before starting the thread though.
					strResponse = Message.ToJson(m);
					context.Response.ContentLength64 = strResponse.Length;
					context.Response.StatusCode = (int) HttpStatusCode.OK;
					break;
				}
				case (int) Message.ApprovedEndpoint.WorkOrderManager:
				{
					//Here we are supposed to have received a valid DeliveryRequest that respects its WorkOrder's requirements
					var thread = new Thread(() => ProcessWorkOrderManagerRequest(m));
					thread.Start();
					//If the thread fails, we will not know about it. Therefore the response has to be positive. So we don't have any fast way to notify the requester that the operation failed.
					//we could still validate some more data before starting the thread though.
					//Create a response
					strResponse = Message.ToJson(m);
					context.Response.Headers.Add("Content", strResponse);
					context.Response.ContentLength64 = strResponse.Length;
					context.Response.StatusCode = (int) HttpStatusCode.OK;
					break;
				}
				default:
					//isrc has an unknown or invalid value for the context. Can't do anything with that.
					m.action = Message.NetworkAction.Error;
					m.source = Message.ApprovedEndpoint.Carrier;
					m.destination = Message.ApprovedEndpoint.Carrier;
					m.content = "Error. Your request was not formatted correctly.";
					strResponse = Message.ToJson(m);
					context.Response.ContentLength64 = strResponse.Length;
					context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
					break;
			}

			//Adding permanent http response headers
			context.Response.ContentType = "application/json";

			context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
			context.Response.ContentEncoding = encoding;
			var buffer = encoding.GetBytes(strResponse);
			context.Response.ContentLength64 = buffer.Length;
			context.Response.OutputStream.BeginWrite(buffer, 0, buffer.Length, FinishedWriteCallBack, context);
		}

		private void ProcessWorkOrderManagerRequest(Message m)
		{
			//should be executed in a child thread.
			//Extract the WorkOrder from the message
			var wo = new WorkOrder();
			try
			{
				wo = WorkOrder.FromJson(m.content);
			}
			catch (Exception e)
			{
				Console.WriteLine("This WorkOrder isn't formatted correctly. Cancelling order 66.");
				Console.WriteLine(e.Message);
				Thread.CurrentThread.Abort();
			}

			//Send it to the ProductionAreaManager for its records (fire "Events")
			var pae = new ProductionAreaEventArgs
			{
				action = ProductionAction
					.None, //Means no physical action in the factory is actually done. Just data passing through.
				idWorkOrder = wo.idWorkOrder,
				anythingElse = wo //This may be bad architecture-wise, but we've seen worse didn't we?
			};

			Events?.Invoke(this,
				pae); //The Manager doesn't need to be "ready" to process anything since we are sending it data for its logs
		}

		private static async void SendToCarrier(Message m)
		{
			//Normally called within a child thread. Do not call this in the main thread or it might block the server for an undefinite time
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

		private static void FinishedWriteCallBack(IAsyncResult result)
		{
			var ctx = (HttpListenerContext) result.AsyncState;
			ctx.Response.OutputStream.EndWrite(result);
			ctx.Response.OutputStream.Flush();
			ctx.Response.OutputStream.Close();
		}

		private static void ProcessCarrierRequest(Message m)
		{
			//Normally called within a child thread. Do not call this in the main thread or it might block the server for an undefinite time
			//Send a request to the WOManager that will contain the WorkOrder associated with the idWorkOrder in the DeliveryOrder
			Console.WriteLine("Fetching the work order from the WorkOrderManager...");
			DeliveryOrder deliveryOrder = null;
			try
			{
				deliveryOrder = DeliveryOrder.FromJson(m.content);
			}
			catch (Exception)
			{
				Console.WriteLine(
					"The delivery order was not formatted correctly. We have to cancel the Winter Contingency Protocol.");
				Thread.CurrentThread.Abort();
			}

			if (deliveryOrder == null)
			{
				Console.WriteLine("The JSON parsing of the DeliveryOrder failed silently " +
				                  "OR The DeliveryOrder did not contain enough data. Either way we must end the show.");
				return;
			}

			//Pile up the delivery order until the ProductionAreaManager's body is ready
			var strIdWorkOrder = deliveryOrder.idWorkOrder;
			if (!IngressWarehouse.ContainsKey(strIdWorkOrder))
				IngressWarehouse[strIdWorkOrder] = deliveryOrder;
			else
				foreach (var item in deliveryOrder.items)
					IngressWarehouse[strIdWorkOrder].items[item.Key] += 1;

			var toWOManager = new Message {action = Message.NetworkAction.Fetch};
			var wo = new WorkOrder {idWorkOrder = strIdWorkOrder};
			toWOManager.content =
				WorkOrder.ToJson(
					wo); //We send a WorkOrder skeleton to the WorkOrderManager so it can fill it with juicy data.
			SendToWorkOrderManager(toWOManager);

			//Our work here is done. We might receive something one day, but at least we are not blocking anyone on the port.
		}

		private static async void SendToWorkOrderManager(Message m)
		{
			//Normally called within a child thread. Do not call this in the main thread or it might block the server for an undefinite time
			try
			{
				var response = await HttpClientLayer.getInstance().Post("http://127.0.0.1:8080/", m);
				Console.WriteLine("Got a response from the WorkOrderManager! Content : {0}", response.content);
			}
			catch (TaskCanceledException tce)
			{
				Console.WriteLine(tce.Message);
				Console.WriteLine("Something went wrong while communicating with the WorkOrderManager.");
				Console.WriteLine("The WorkOrderManager might be down or unstable right now.");
				Console.WriteLine("Feel free to try again later!");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public event EventHandler<ProductionAreaEventArgs> Events;

		public void OnProductionAreaManager(object sender, ProductionAreaEventArgs e)
		{
			switch (e.action)
			{
				case ProductionAction.Ready:
					//Send some items this way
					//Pick all the items for a single WorkOrder, and create an IEnumerable<string> with them.
					Console.WriteLine("Preparing to send a bunch of items to the ProductionAreaManager.");
					var itemsToPickFrom = IngressWarehouse[e.idWorkOrder].items;
					var eventArgsItems = new List<string>();
					foreach (var item in itemsToPickFrom)
						while (item.Value > 0)
						{
							eventArgsItems.Add(item.Key);
							itemsToPickFrom[item.Key] -= 1;
						}

					var eventArgs = new ProductionAreaEventArgs
					{
						action = ProductionAction.Prod,
						idWorkOrder = e.idWorkOrder,
						items = eventArgsItems
					};
					Console.WriteLine("Sending a bunch of items to the ProductionManager");
					Events?.Invoke(this, eventArgs);
					break;
				case ProductionAction.Done:
					//Got some items to send to FinalWarehouse via Carrier
					var deliveryOrder = new DeliveryOrder(e.idWorkOrder);
					foreach (var toSend in e.items)
					{
						if (!deliveryOrder.items.ContainsKey(toSend)) deliveryOrder.items[toSend] = 0;

						//Increment the count of items of that type by 1
						deliveryOrder.items[toSend] += 1;
					}

					var toCarrier = new Message
					{
						content = deliveryOrder.ToString(),
						action = Message.NetworkAction.Delivery,
						source = Message.ApprovedEndpoint.ProductionArea,
						destination = Message.ApprovedEndpoint.FinalWarehouse
					};
					var sendingJob =
						new Thread(() => SendToCarrier(toCarrier)); //Just to make sure we don't block on this one.
					sendingJob.Start();
					break;
				case ProductionAction.Error:
					//Maybe we should do something, but it's no time!
					break;
				case ProductionAction.None:
					//TBD
					break;
				case ProductionAction.Prod:
					//Nothing is to be produced by the server
					break;
				case ProductionAction.Stop:
					//It would be kind of meaningless for the Manager to stop its own server, but we could do it if we wanted to.
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public void Subscribe(ProductionAreaManager pam)
		{
			Console.WriteLine("Server Subscribed to Manager");
			pam.EventsServer += OnProductionAreaManager;
		}
	}
}