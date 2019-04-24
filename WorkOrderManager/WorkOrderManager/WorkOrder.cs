using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WorkOrderManager
{
	internal class WorkOrder
	{
		public Dictionary<string, uint> FinishedProducts = new Dictionary<string, uint>();
		public string idWorkOrder = "";
		public string Name = "";
		public Dictionary<string, uint> RequiredProducts = new Dictionary<string, uint>();
		public DateTime timeClosed;
		public DateTime timeLaunched;

		public bool ReadyToClose()
		{
			foreach (var item in RequiredProducts)
				if (FinishedProducts.ContainsKey(item.Key))
				{
					if (FinishedProducts[item.Key] != item.Value) return false;
				}
				else
				{
					return false;
				}

			return true;
		}

		public void Launch()
		{
			timeLaunched = DateTime.Now;
		}

		public void Close()
		{
			timeClosed = DateTime.Now;
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