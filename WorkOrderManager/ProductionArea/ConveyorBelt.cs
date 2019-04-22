using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductionArea
{
	class ConveyorBelt
	{
		private string state;
		public event EventHandler<EventArgs> Events;


		public void Start()
		{
			throw new NotImplementedException();
		}

		public void StartMachine(string FinishedProductType)
		{
			
		}

		public void subscribe(Machine machine)
		{
			machine.Events += OnMachineEvent;
		}

		public void subscribe(ProductionAreaManager manager)
		{
			manager.Events += OnManagerEvent;
		}

		public void OnMachineEvent(object sender, EventArgs e)
		{

		}

		public void OnManagerEvent(object sender, EventArgs e)
		{

		}
	}
}
