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
		public static ConcurrentQueue<DeliveryOrder> containers = new ConcurrentQueue<DeliveryOrder>();

		private ConveyorBelt Conveyor = new ConveyorBelt();
		private Machine machineA = new Machine("A"); //A machine that makes items of type A
		private Machine machineB = new Machine("B");
		private Machine machineC = new Machine("C");

		private List<Thread> threads = new List<Thread>();
		//This runs on the main Thread
		public ProductionAreaManager()
		{
			Conveyor.subscribe(machineA);
			Conveyor.subscribe(machineB);
			Conveyor.subscribe(machineC);
			machineA.subscribe(Conveyor);
			machineB.subscribe(Conveyor);
			machineC.subscribe(Conveyor);
			Conveyor.subscribe(this);
			Subscribe(Conveyor);
		}

		public void Start()
		{
			threads.Add(new Thread(Conveyor.Start)); //Runs on a child thread "as slave"
			threads.Add(new Thread(machineA.Start));
			threads.Add(new Thread(machineB.Start));
			threads.Add(new Thread(machineC.Start));			
		}

		public void Subscribe(ConveyorBelt cb)
		{
			cb.Events += OnConveyorBeltEvent;
		}

		public void OnConveyorBeltEvent(object sender, EventArgs e)
		{
			//Do something when the conveyor calls us
		}

		public event EventHandler<EventArgs> Events;

		public void Stop()
		{
			Events.Invoke(this, EventArgs.Empty); //send some emergency message
			foreach(var t in threads)
			{
				t.Join();
			}
		}
	}


}
