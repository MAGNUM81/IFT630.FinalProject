using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BOMOrderManager;
using WorkOrderManager;

namespace ProductionArea
{
	class ProductionAreaManager
	{
		

		private readonly ConveyorBelt Conveyor = new ConveyorBelt();
		private readonly Machine machineA = new Machine("A", "Y", "Z"); //A machine that makes items of type A
		private readonly Machine machineB = new Machine("B", "X", "Y", "Z");
		private readonly Machine machineC = new Machine("C");

		public event EventHandler<ProductionAreaEventArgs> EventsConveyor;
		public event EventHandler<ProductionAreaEventArgs> ServerEvents;

		private readonly Queue<ProductionAreaEventArgs> ToProcess = new Queue<ProductionAreaEventArgs>();
		private readonly Dictionary<string, WorkOrder> ActiveWorkOrders = new Dictionary<string, WorkOrder>();

		private readonly List<Thread> threads = new List<Thread>();
		//This runs on the main Thread
		public ProductionAreaManager( ref ProductionAreaServer serverRef)
		{
			//An instance of the local server must exist in order for this to work, since the server is the only input source.
			Conveyor.subscribe(machineA);
			Conveyor.subscribe(machineB);
			Conveyor.subscribe(machineC);
			machineA.subscribe(Conveyor);
			machineB.subscribe(Conveyor);
			machineC.subscribe(Conveyor);
			Conveyor.subscribe(this);
			Subscribe(Conveyor);
			Subscribe(ref serverRef);
			serverRef.Subscribe(this);

		}

		

		public void Start()
		{
			threads.Add(new Thread(Conveyor.Start)); //Runs on a child thread "as slave"
			threads.Add(new Thread(machineA.Start));
			threads.Add(new Thread(machineB.Start));
			threads.Add(new Thread(machineC.Start));
			foreach(var t in threads)
			{
				t.Start();
			}
			ServerEvents?.Invoke(this, new ProductionAreaEventArgs(ProductionAction.Ready));
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
			//We receive the deliveries via e, which is a transformation of DeliveryOrder. The server did that transformation. Should it? idk.
			switch (e.action)
			{
				case ProductionAction.None: //Means we are receiving a WorkOrder fetched from the WorkOrderManager by the server
					//TODO: For efficiency's sake, one might suggest to fetch the WO from here.
					var completeWorkOrder = (WorkOrder)e.anythingElse;
					if(!ActiveWorkOrders.ContainsKey(completeWorkOrder.idWorkOrder))
					{
						//We only take the first of its kind, because the WorkOrderManager doesn't necessarily
						//know about everything that goes on in the ProductionArea

						ActiveWorkOrders[completeWorkOrder.idWorkOrder] = completeWorkOrder;
					}
					
					break;
				case ProductionAction.Prod:
					ToProcess.Enqueue(e);
					ServerEvents?.Invoke(this, new ProductionAreaEventArgs(ProductionAction.Ready));
					break;
			}
			
		}

		public void OnConveyorBeltEvent(object sender, ProductionAreaEventArgs e)
		{
			//Do something when the conveyor calls us
			switch(e.action)
			{
				case ProductionAction.Ready:
					//if there is something to send, send it.
					//else, dont send anything, it can take care of itself during that time.
					if(ToProcess.Count != 0)
					{
						var pae = ToProcess.Dequeue();   //pull the task from the storage unit
						EventsConveyor?.Invoke(this, pae); //send it to the conveyor
					}
					break;
				case ProductionAction.Done:
					//The conveyor belt has done one piece of job (might contain more than one item)!
					WorkOrder toUpdate = ActiveWorkOrders[e.idWorkOrder];
					var dictFinishedProducts = toUpdate.FinishedProducts;
					//Register the progression in the WorkOrder dictionary
					foreach(var item in e.items)
					{
						if(!toUpdate.FinishedProducts.ContainsKey(item))
						{
							dictFinishedProducts[item] = 1;
						}
						else
						{
							dictFinishedProducts[item] += 1;
						}
					}
					toUpdate.FinishedProducts = dictFinishedProducts; //Just to make sure in case these weren't references.
					if(!toUpdate.ReadyToClose())
					{
						ActiveWorkOrders[e.idWorkOrder] = toUpdate;       //Just to make sure in case these weren't references.
					}
					else
					{
						//If the WO is ready to be closed, remove it from the WO dictionary.
						ActiveWorkOrders.Remove(e.idWorkOrder);
					}

					//Send the products to the final Warehouse via the Carrier.
					var toServer = new ProductionAreaEventArgs
					{
						action = ProductionAction.Done,
						items = e.items
					};
					ServerEvents?.Invoke(this, toServer);
					break;
			}
		}

		public void Stop()
		{
			EventsConveyor?.Invoke(this, new ProductionAreaEventArgs()); //send some emergency message
			foreach(var t in threads)
			{
				t.Join();
			}
		}

		
	}


}
