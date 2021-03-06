﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace ProductionArea
{
	internal class Machine
	{
		private readonly Dictionary<string, uint> BOMsNeeded = new Dictionary<string, uint>(); //Recipe
		private readonly string FinishedProductType;
		private readonly Dictionary<string, uint> WorkingMemory = new Dictionary<string, uint>(); //Progression
		public ComponentState state;
		public string ID = Guid.NewGuid().ToString();

		public Machine(string FProductType, params string[] BOMs)
		{
			state = ComponentState.Idle;
			FinishedProductType = FProductType;
			foreach (var item in BOMs)
			{
				if (!BOMsNeeded.ContainsKey(item))
				{
					BOMsNeeded[item] = 0;
					WorkingMemory[item] = 0;
				}

				BOMsNeeded[item] += 1;
			}
		}

		public event EventHandler<ProductionAreaEventArgs> Events;

		public void Start()
		{
			state = ComponentState.Running;
			Console.WriteLine("The machine of type {0} is up and running.", FinishedProductType);
			while(true)
			{
				if(state == ComponentState.Running)
					Events?.Invoke(this, new ProductionAreaEventArgs(ProductionAction.Ready));
			}
			
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
					state = ComponentState.Running;
					if (e.typeProd == FinishedProductType && (string)e.anythingElse == ID)
					{
						Console.WriteLine("Machine #{0} of type {1} has received a job.", ID, FinishedProductType);
						ProcessTranformationRequest(e);
					}

					break;
				}
				case ProductionAction.Stop:
					if (state == ComponentState.Busy)
					{
						//If we're busy, save progress, then stop.
						//TODO: save progress
					}

					//Else, just stop.
					Stop();
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
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ProcessTranformationRequest(ProductionAreaEventArgs e)
		{
			state = ComponentState.Busy;
			//Execute the transformation, using all the items from e.items and producing the final product from it
			foreach (var i in e.items)
				if (BOMsNeeded.ContainsKey(i))
					WorkingMemory[i] += 1;
			//else, if it's not in our recipe, scrap it and optionnally tell the conveyor belt to get its **** together.
			//In other words, this incident will be reflected the "waste" section of the yearly statistics report.

			//If every key in the Working memory has a value superior to 0, then we can go and do the thing
			//else, if at least one key has a value of 0, save progress, go in Error state and notify the ConveyorBelt
			//Because we do not have all the ingredients needed to start the operation.
			if (!WorkingMemoryOK())
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
			//Fire a Done Event containing the items produced
			Events?.Invoke(this, args);
			state = ComponentState.Running;
			//Then a Ready event
			Events?.Invoke(this, new ProductionAreaEventArgs(ProductionAction.Ready));
		}

		private string Transform()
		{
			Console.WriteLine("************PROCESSING TYPE {0}************", FinishedProductType);
			for(int i = 0; i < 3; ++i)
			{
				Thread.Sleep(1000);
				Console.Write(".....\t");
			}
			Console.WriteLine("\n**********END OF PROCESSING {0}************", FinishedProductType);
			

			return FinishedProductType;
		}

		private bool WorkingMemoryOK()
		{
			foreach (var keyval in WorkingMemory)
				if (keyval.Value == 0)
					return false;
			return true;
		}
	}
}