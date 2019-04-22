using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProductionArea
{
	class Machine 
	{
		private string FinishedProductType;
		public event EventHandler<EventArgs> Events;
		public Machine(string FProductType)
		{
			FinishedProductType = FProductType;
		}

		public void Start()
		{

		}

		public void subscribe(ConveyorBelt conveyor)
		{
				conveyor.Events += OnSubscribedEvent;
		}

		public void OnSubscribedEvent(object sender, EventArgs e)
		{

		}

		
	}
}
