using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ProductionArea
{
	internal class Machine
	{
		private readonly string FinishedProductType;
		private readonly Dictionary<string, uint> BOMsNeeded = new Dictionary<string, uint>(); //Recipe
		private readonly Dictionary<string, uint> WorkingMemory = new Dictionary<string, uint>(); //Progression
		public event EventHandler<ProductionAreaEventArgs> Events;
		private ComponentState state;
		public Machine(string FProductType, params string[] BOMs)
		{
			state = ComponentState.Idle;
			FinishedProductType = FProductType;
			foreach (var item in BOMs)
			{
				if(!BOMsNeeded.ContainsKey(item))
				{
					BOMsNeeded[item] = 0;
					WorkingMemory[item] = 0;
				}

				BOMsNeeded[item] += 1;
			}
		}

		public void Start()
		{
			state = ComponentState.Running;
			Console.WriteLine("The machine of type {0} is up and running.", FinishedProductType);
			Events?.Invoke(this, new ProductionAreaEventArgs(ProductionAction.Ready));
		}

		public void Stop()
		{
			state = ComponentState.Stopped;
		}

		public void subscribe(ConveyorBelt conveyor)
		{
			conveyor.EventsMachine += OnSubscribedEventMachine;
		}

		public void OnSubscribedEventMachine(object sender, ProductionAreaEventArgs e)
		{
		
			switch (e.action)
			{
				case ProductionAction.Prod:
				{
					if (e.typeProd == FinishedProductType)
					{
						ProcessTranformationRequest(e);
					}

					break;
				}
				case ProductionAction.Stop:
					if(state == ComponentState.Busy)
					{
						//If we're busy, save progress, then stop.
						//TODO: save progress
					}
					//Else, just stop.
					Stop();
					break;
			}

		
		}

		private void ProcessTranformationRequest(ProductionAreaEventArgs e)
		{
			state = ComponentState.Busy;
			//Execute the transformation, using all the items from e.items and producing the final product from it
			foreach(var i in e.items)
			{
				if(BOMsNeeded.ContainsKey(i))
				{
					//If this item is in our recipe, add it to the working Memory
					WorkingMemory[i] += 1;
				}
				//else, if it's not in our recipe, scrap it and optionnally tell the conveyor belt to get its **** together.
				//In other words, this incident will be reflected the "waste" section of the yearly statistics report.
			}

			//If every key in the Working memory has a value superior to 0, then we can go and do the thing
			//else, if at least one key has a value of 0, save progress, go in Error state and notify the ConveyorBelt
			//Because we do not have all the ingredients needed to start the operation.
			if(!WorkingMemoryOK())
			{
				state = ComponentState.Error;
				//save progress
				Events?.Invoke(this, new ProductionAreaEventArgs(ProductionAction.Error));
			}

			//DO The Thing...
			var result = Transform();

			//Prepare the finishing message
			var args = new ProductionAreaEventArgs(
				ProductionAction.Done, 
				e.idWorkOrder, 
				new[] {result});
			//Fire a Ready Event containing the items produced
			Events?.Invoke(this, args);
		}

		private string Transform()
		{
			foreach(var item in WorkingMemory)
			{
				var maxIter = item.Value;
				for(var i = 0; i < maxIter; ++i)
				{
					WorkingMemory[item.Key] -= 1; //We Empty the working memory one item at a time.
								//The processing time will Depend on the number of elements to process
				}
			}

			return FinishedProductType;
		}

		private bool WorkingMemoryOK()
		{
			foreach(var keyval in WorkingMemory)
			{
				if(keyval.Value == 0)
				{
					return false;
				}
			}
			return true;
		}

	}
}
