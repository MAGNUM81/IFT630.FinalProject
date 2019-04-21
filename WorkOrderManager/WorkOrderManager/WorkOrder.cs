using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WorkOrderManager
{
	internal class WorkOrder
	{
		public string idWorkOrder = "";
		public string Name = "";
		public Dictionary<string, uint> RequiredProducts = new Dictionary<string, uint>();
		public Dictionary<string, uint> FinishedProducts = new Dictionary<string, uint>();
		public DateTime timeLaunched = DateTime.Now;
		public DateTime timeClosed;

		public WorkOrder()
		{

		}

		public bool ReadyToClose()
		{
			foreach (var item in RequiredProducts)
			{
				if (FinishedProducts.ContainsKey(item.Key))
				{
					if (FinishedProducts[item.Key] != item.Value)
					{
						return false;
					}
				}
				else return false;
			}
			return true;
		}

		public void close()
		{
			this.timeClosed = DateTime.Now;
		}

		public static string ToJson(WorkOrder wo)
		{
			return JsonConvert.SerializeObject(wo);
		}

		public static WorkOrder FromJson(string jsonString)
		{
			return JsonConvert.DeserializeObject<WorkOrder>(jsonString);
		}

		public override string ToString()
		{
			return base.ToString();
		}

	}
}
