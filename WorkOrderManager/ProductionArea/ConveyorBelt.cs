using System;
using System.Collections.Generic;

namespace ProductionArea
{
	public enum ComponentState
	{
		//There is not a big difference between Idle and Stopped.
		//Stopped just means that the Stop function has been called.
		Idle = 0,
		Running = 1,
		Stopped = 2,
		Error = 3,
		Busy = 4
	}

	internal class ConveyorBelt
	{

		private ComponentState state;

		public event EventHandler<ProductionAreaEventArgs> EventsMachine;
		public event EventHandler<ProductionAreaEventArgs> EventsManager;
		private readonly Queue<ProductionAreaEventArgs> ToTransform = new Queue<ProductionAreaEventArgs>();

		public ConveyorBelt()
		{
			state = ComponentState.Idle;
		}

		public void Start()
		{
			state = ComponentState.Running;
			Console.WriteLine("The Conveyor Belt is up and running.");
			EventsManager?.Invoke(this, new ProductionAreaEventArgs(ProductionAction.Ready));
		}

		public void Stop()
		{
			state = ComponentState.Stopped;
		}

		public void subscribe(Machine machine)
		{
			machine.Events += OnMachineEvent;
		}

		public void subscribe(ProductionAreaManager manager)
		{
			manager.EventsConveyor += OnManagerEvent;
		}

		public void OnMachineEvent(object sender, ProductionAreaEventArgs e)
		{
			//All the calls from the Machines will land here
			switch (e.action)
			{
				case ProductionAction.Ready:
					if(ToTransform.Count != 0)
					{
						var args = ToTransform.Dequeue();
						EventsMachine?.Invoke(this, args);
					}
					break;
			}
		}

		public void OnManagerEvent(object sender, ProductionAreaEventArgs e)
		{
			//All the calls from the Manager will land here
			switch (e.action)
			{
				case ProductionAction.Prod:
					ToTransform.Enqueue(e);
					break;

			}
		}
	}
}
