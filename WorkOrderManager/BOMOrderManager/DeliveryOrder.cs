using System.Collections.Generic;
using Newtonsoft.Json;

namespace BOMOrderManager
{
	internal class DeliveryOrder
	{
		public string idWorkOrder;
		public Dictionary<string, uint> items = new Dictionary<string, uint>();

		public DeliveryOrder(string idWO)
		{
			idWorkOrder = idWO;
		}

		public static string ToJson(DeliveryOrder wo)
		{
			return JsonConvert.SerializeObject(wo);
		}

		public static DeliveryOrder FromJson(string jsonString)
		{
			return JsonConvert.DeserializeObject<DeliveryOrder>(jsonString);
		}
	}
}