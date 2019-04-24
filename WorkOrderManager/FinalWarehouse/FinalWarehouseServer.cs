using BOMOrderManager;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WorkOrderManager;

namespace FinalWarehouse
{
	internal class FinalWarehouseServer
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
		public FinalWarehouseServer(string path, int port)
		{
			Initialize(path, port);
		}

		/// <summary>
		///     Construct server with suitable port.
		/// </summary>
		/// <param name="path">Directory path to serve.</param>
		public FinalWarehouseServer(string path)
		{
			//get an empty port
			var l = new TcpListener(IPAddress.Loopback, 0);
			l.Start();
			var port = ((IPEndPoint)l.LocalEndpoint).Port;
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

		private static void Process(HttpListenerContext context)
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
			var isrc = (int)m.source;
			Console.WriteLine("***FROM : {0}", m.source.ToString());
			if (isrc == (int)Message.ApprovedEndpoint.WorkOrderManager)
			{
				//Create a response


				var thread = new Thread(() => ProcessWorkOrderManagerRequest(m));
				thread.Start();

				m.action = Message.NetworkAction.Echo;
				strResponse = Message.ToJson(m);

				context.Response.ContentLength64 = strResponse.Length;
				context.Response.StatusCode = (int)HttpStatusCode.OK;
			}
			else if (isrc == (int)Message.ApprovedEndpoint.Carrier)
			{
				//When we get the delivery from the Carrier

				var thread = new Thread(() => ProcessCarrierRequest(m));
				thread.Start();

				m.action = Message.NetworkAction.Echo;
				strResponse = Message.ToJson(m);
				context.Response.ContentLength64 = strResponse.Length;
				context.Response.StatusCode = (int)HttpStatusCode.OK;
			}

			//Adding permanent http response headers
			context.Response.ContentType = "application/json";
			context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
			context.Response.ContentEncoding = encoding;
			var buffer = encoding.GetBytes(strResponse);
			context.Response.ContentLength64 = buffer.Length;
			context.Response.OutputStream.BeginWrite(buffer, 0, buffer.Length, FinishedWriteCallBack, context);
		}

		private static void ProcessCarrierRequest(Message message)
		{
			//When we get the delivery from the Carrier, we must store the Delivery temporarily
			//in another storage unit so it can be processed when the WorkOrder associated to that
			//Delivery has been retrieved.

			//Extract the DeliveryOrder from the message
			var deliveryOrder = DeliveryOrder.FromJson(message.content);
			//Extract the id from it: we will need it a lot!
			var idWorkOrder = deliveryOrder.idWorkOrder;
			//if our workOrder doesn't exist in our storageUnit, temporarily store the WorkOrder and poke the WorkOrderManager for data.
			if (!FinalWarehouseManager.ExistWorkOrder(idWorkOrder))
			{
				Console.WriteLine("NEW WORK ORDER INCOMING!");
				if (!FinalWarehouseManager.temporaryStorageUnit.ContainsKey(idWorkOrder))
				{
					FinalWarehouseManager.temporaryStorageUnit[idWorkOrder] = new DeliveryOrder(idWorkOrder);
				}
				foreach (var item in deliveryOrder.items)
				{
					if(!FinalWarehouseManager.temporaryStorageUnit[idWorkOrder].items.ContainsKey(item.Key))
					{
						FinalWarehouseManager.temporaryStorageUnit[idWorkOrder].items[item.Key] = 0;
					}
					FinalWarehouseManager.temporaryStorageUnit[idWorkOrder].items[item.Key] += item.Value;
				}

				//Send a new request to retrieve the WorkOrder from the WorkOrderManager, 

				var req = new Message();
				var emptyWorkOrder = new WorkOrder { idWorkOrder = idWorkOrder };
				req.action = Message.NetworkAction.Fetch;
				req.source = Message.ApprovedEndpoint.FinalWarehouse;
				req.destination = Message.ApprovedEndpoint.WorkOrderManager;
				req.content = WorkOrder.ToJson(emptyWorkOrder);
				Console.WriteLine("SENDING FETCH TO MANAGER");
				SendToWorkOrderManager(req);
			}
			else
			{
				//Else, if the work order already exists, update its items counts from the delivery

				FinalWarehouseManager.UpdateWorkOrderItems(idWorkOrder, deliveryOrder.items);

				//Then, check if the workOrder is ready to close
				//If it is, send a "Closed" message with the completed workOrder to the Manager
				//If not, return
				Console.WriteLine("CHECKING IF READY TO CLOSE");
				if (!FinalWarehouseManager.storageUnit[idWorkOrder].ReadyToClose()) return;
				Console.WriteLine("***********Sending Close Notification to WorkOrderManager**********");
				var closeNotification = new Message {action = Message.NetworkAction.CloseWorkOrder};
				var wo = FinalWarehouseManager.storageUnit[idWorkOrder];
				var strWO = WorkOrder.ToJson(wo);
				closeNotification.content = strWO;
				closeNotification.source = Message.ApprovedEndpoint.FinalWarehouse;
				closeNotification.destination = Message.ApprovedEndpoint.WorkOrderManager;
				SendToWorkOrderManager(closeNotification);
			}

		}

		private static void ProcessWorkOrderManagerRequest(Message message)
		{
			switch (message.action)
			{
				case Message.NetworkAction.Echo:
				case Message.NetworkAction.Fetch:
					//Receiving the WorkOrder for a precedent Fetch request
					//Extract the WO from the content
					var wo = WorkOrder.FromJson(message.content);
					//Store the workOrder in the storage unit if it doesn't already exist.
					if (!FinalWarehouseManager.ExistWorkOrder(wo.idWorkOrder))
					{
						FinalWarehouseManager.AddWorkOrder(wo);
					}
					//Check if we have associated pending items in the temporary storage unit
					//If so, merge the items contained in it and remove the key completely.
					if(FinalWarehouseManager.temporaryStorageUnit.ContainsKey(wo.idWorkOrder))
					{
						FinalWarehouseManager.UpdateWorkOrderItems(wo.idWorkOrder, FinalWarehouseManager.temporaryStorageUnit[wo.idWorkOrder].items);
						FinalWarehouseManager.temporaryStorageUnit.Remove(wo.idWorkOrder);
					}
					Console.WriteLine("CHECKING IF READY TO CLOSE");
					if (!FinalWarehouseManager.storageUnit[wo.idWorkOrder].ReadyToClose()) return;
					Console.WriteLine("***********Sending Close Notification to WorkOrderManager**********");
					var closeNotification = new Message {action = Message.NetworkAction.CloseWorkOrder};
					var updatedWorkOrder = FinalWarehouseManager.storageUnit[wo.idWorkOrder];
					var strWO = WorkOrder.ToJson(updatedWorkOrder);
					closeNotification.content = strWO;
					closeNotification.source = Message.ApprovedEndpoint.FinalWarehouse;
					closeNotification.destination = Message.ApprovedEndpoint.WorkOrderManager;
					SendToWorkOrderManager(closeNotification);
					break;
				case Message.NetworkAction.CloseWorkOrder:
					Console.WriteLine("I am done with a WO! DONE!");
					break;
				case Message.NetworkAction.Error:
					break;
				
				case Message.NetworkAction.Forward:
					break;
				case Message.NetworkAction.Stop:
					break;
				case Message.NetworkAction.Delivery:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static void FinishedWriteCallBack(IAsyncResult result)
		{
			var ctx = (HttpListenerContext)result.AsyncState;
			ctx.Response.OutputStream.EndWrite(result);
			ctx.Response.OutputStream.Flush();
			ctx.Response.OutputStream.Close();
		}

		private static async void SendToWorkOrderManager(Message m)
		{
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

		private void Initialize(string path, int port)
		{
			_rootDirectory = path;
			_port = port;
			_serverThread = new Thread(Listen);
			_serverThread.Start();
		}
	}
}
