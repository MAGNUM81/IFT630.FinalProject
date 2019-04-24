using System;
using System.Collections.Generic;
using System.Threading;
using WorkOrderManager;

namespace ProductionArea
{
	internal class ProductionAreaManager
	{
		private readonly Dictionary<string, WorkOrder> ActiveWorkOrders = new Dictionary<string, WorkOrder>();
		private readonly ConveyorBelt Conveyor = new ConveyorBelt();
		private readonly Machine machineA = new Machine("A", "Y", "Z"); //A machine that makes items of type A
		private readonly Machine machineB = new Machine("A", "Y", "Z");
		private readonly Machine machineC = new Machine("A", "Y", "Z");
		private readonly Machine machineD = new Machine("A", "Y", "Z");

		private readonly List<Thread> threads = new List<Thread>();

		private readonly Queue<ProductionAreaEventArgs> ToProcess = new Queue<ProductionAreaEventArgs>();

		//This runs on the main Thread
		public ProductionAreaManager(ref ProductionAreaServer serverRef)
		{
			//An instance of the local server must exist in order for this to work, since the server is the only input source.
			Conveyor.Subscribe(machineA);
			Conveyor.Subscribe(machineB);
			Conveyor.Subscribe(machineC);
			Conveyor.Subscribe(machineD);
			machineA.subscribe(Conveyor);
			machineB.subscribe(Conveyor);
			machineC.subscribe(Conveyor);
			machineD.subscribe(Conveyor);
			Conveyor.Subscribe(this);
			Subscribe(Conveyor);
			Subscribe(ref serverRef);
			serverRef.Subscribe(this);
		}

		public event EventHandler<ProductionAreaEventArgs> EventsConveyor;
		public event EventHandler<ProductionAreaEventArgs> EventsServer;


		public void Start()
		{
			threads.Add(new Thread(Conveyor.Start)); //Runs on a child thread as "client"
			threads.Add(new Thread(machineA.Start));
			threads.Add(new Thread(machineB.Start));
			threads.Add(new Thread(machineC.Start));
			foreach (var t in threads) t.Start();
			while (true)
			{
				if (ToProcess.Count > 0)
				{
					EventsConveyor?.Invoke(this, ToProcess.Dequeue());
				}
			}
		}

		private void Subscribe(ref ProductionAreaServer serverRef)
		{
			Console.WriteLine("Manager subscribed to Server");
			serverRef.Events += OnProductionAreaServerEvent;
		}

		public void Subscribe(ConveyorBelt cb)
		{
			Console.WriteLine("Manager subscribed to Conveyor Belt");
			cb.EventsManager += OnConveyorBeltEvent;
		}

		public void OnProductionAreaServerEvent(object sender, ProductionAreaEventArgs e)
		{
			//We receive the deliveries via e.anythingelse, which here is a DeliveryOrder. The server did that . Should it? idk.
			switch (e.action)
			{
				case ProductionAction.None
					: //Means we are receiving a WorkOrder fetched from the WorkOrderManager by the server
					  //TODO: For efficiency's sake, one might suggest to fetch the WO from here instead the server. Anyways.
					var completeWorkOrder = (WorkOrder)e.anythingElse;
					if (!ActiveWorkOrders.ContainsKey(completeWorkOrder.idWorkOrder))
					{
						//We only take the first of its kind, because the WorkOrderManager doesn't necessarily
						//know about everything that goes on in the ProductionArea

						ActiveWorkOrders[completeWorkOrder.idWorkOrder] = completeWorkOrder;
						Console.WriteLine("Succesfully added WorkOrder #{0} to the working set.", completeWorkOrder.idWorkOrder);
						//Now that we are assured the WorkOrder exists, we can notify the Server that we're ready for some action.
						var toServer = new ProductionAreaEventArgs
						{
							action = ProductionAction.Ready,
							idWorkOrder = completeWorkOrder.idWorkOrder
						};
						EventsServer?.Invoke(this, toServer);
					}

					break;
				case ProductionAction.Prod:

					//Determine the typeProd from e
					if(e.items.Contains("Y") && e.items.Contains("Z"))
					{
						e.typeProd = "A";
						if(e.items.Contains("X"))
						{
							e.typeProd = "B";
							if(e.items.Contains("W"))
							{
								e.typeProd = "C";
							}
						}
					}

					Console.WriteLine("A new Task has been queued!");
					ToProcess.Enqueue(e);
					EventsServer?.Invoke(this, new ProductionAreaEventArgs(ProductionAction.Ready));
					break;
			}
		}

		public void OnConveyorBeltEvent(object sender, ProductionAreaEventArgs e)
		{
			//Do something when the conveyor calls us
			switch (e.action)
			{
				case ProductionAction.Ready:
					//if there is something to send, send it.
					//else, dont send anything, it can take care of itself during that time.
					if (ToProcess.Count != 0)
					{
						var pae = ToProcess.Dequeue(); //pull the task from the storage unit
						Console.WriteLine("A Task has been dequeued by the Conveyor belt!");
						EventsConveyor?.Invoke(this, pae); //send it to the conveyor
					}
					else
					{
						Console.WriteLine("The Conveyor is ready, but nothing is coming...");
						EventsConveyor?.Invoke(this, new ProductionAreaEventArgs(ProductionAction.None));
					}

					break;
				case ProductionAction.Done:
					//The conveyor belt has done one piece of job (might contain more than one item)!
					Console.WriteLine("The conveyor belt has finished a task on WO #{0}!", e.idWorkOrder);
					var toUpdate = ActiveWorkOrders[e.idWorkOrder];
					var dictFinishedProducts = toUpdate.FinishedProducts;
					//Register the progression in the WorkOrder dictionary
					foreach (var item in e.items)
						if (!toUpdate.FinishedProducts.ContainsKey(item))
							dictFinishedProducts[item] = 1;
						else
							dictFinishedProducts[item] += 1;
					toUpdate.FinishedProducts =
						dictFinishedProducts; //Just to make sure in case these weren't references.
					if (!toUpdate.ReadyToClose())
						ActiveWorkOrders[e.idWorkOrder] =
							toUpdate; //Just to make sure in case these weren't references.
					else
						ActiveWorkOrders.Remove(e.idWorkOrder);

					//Send the products to the final Warehouse via the Carrier.
					var toServer = new ProductionAreaEventArgs
					{
						action = ProductionAction.Done,
						idWorkOrder = e.idWorkOrder,
						items = e.items
					};
					Console.WriteLine("Sending the items producted to the Prod. Area Server");
					EventsServer?.Invoke(this, toServer);
					break;
			}
		}

		public void Stop()
		{
			EventsConveyor?.Invoke(this, new ProductionAreaEventArgs()); //send some emergency message
			foreach (var t in threads) t.Join();
		}
	}
}