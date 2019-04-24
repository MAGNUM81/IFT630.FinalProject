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

		private Dictionary<string, Machine> machinesList = new Dictionary<string, Machine>();
		private readonly Queue<ProductionAreaEventArgs> ToTransform = new Queue<ProductionAreaEventArgs>();

		private ComponentState state;

		public ConveyorBelt()
		{
			state = ComponentState.Idle;
		}

		public event EventHandler<ProductionAreaEventArgs> EventsMachine;
		public event EventHandler<ProductionAreaEventArgs> EventsManager;

		public void Start()
		{
			state = ComponentState.Running;
			Console.WriteLine("The Conveyor Belt is up and running.");
			
			while(true)
			{
				if(state == ComponentState.Running)
					EventsManager?.Invoke(this, new ProductionAreaEventArgs(ProductionAction.Ready));
				if(ToTransform.Count > 0)
				{
					Console.WriteLine("Sending job item to machine");
					var e = ToTransform.Dequeue();
					foreach (var machine in machinesList)
					{
						if (machine.Value.state != ComponentState.Running) continue;
						e.anythingElse = machine.Key;
						break;
					}
					EventsMachine?.Invoke(this, e);
				}
			}
		}

		public void Stop()
		{
			state = ComponentState.Stopped;
		}

		public void Subscribe(Machine machine)
		{
			machine.Events += OnMachineEvent;
			machinesList.Add(machine.ID, machine);
		}

		public void Subscribe(ProductionAreaManager manager)
		{
			manager.EventsConveyor += OnManagerEvent;
		}

		public void OnMachineEvent(object sender, ProductionAreaEventArgs e)
		{
			//All the calls from the Machines will land here
			switch (e.action)
			{
				case ProductionAction.Ready:
					if (ToTransform.Count != 0)
					{
						var args = ToTransform.Dequeue();
						foreach (var machine in machinesList)
						{
							if (machine.Value.state != ComponentState.Running) continue;
							e.anythingElse = machine.Key;
							break;
						}
						EventsMachine?.Invoke(this, args);
					}
					else
					{
						EventsMachine?.Invoke(this, new ProductionAreaEventArgs(ProductionAction.None));
					}

					break;
				case ProductionAction.Done:
					//TODO : check if another machine needs that item and proceed accordingly.
					//We forward the result to the Production Area Manager
					Console.WriteLine("**********A Machine has finished its job! Forwarding to Manager...");
					EventsManager?.Invoke(this, e);
					break;
				case ProductionAction.Error:
					//Check the state of the Machine
					//If it's effectively in error, retain the task, or redirect it, and notify the ProductionAreaManager
					break;
				case ProductionAction.None:
					// ... not used int this context
					break;
				case ProductionAction.Prod:
					// ... not used int this context
					break;
				case ProductionAction.Stop:
					// ... not used int this context
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public void OnManagerEvent(object sender, ProductionAreaEventArgs e)
		{
			//All the calls from the Manager will land here
			switch (e.action)
			{
				case ProductionAction.Prod:
					state = ComponentState.Running;
					Console.WriteLine("**********The Conveyor has received and queued a work item");
					ToTransform.Enqueue(e);
					break;
				case ProductionAction.Error:
					break;
				case ProductionAction.None:
					state = ComponentState.Idle;
					break;
				case ProductionAction.Done:
					break;
				case ProductionAction.Ready:
					break;
				case ProductionAction.Stop:
					Stop();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}