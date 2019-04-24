using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOMOrderManager;
using WorkOrderManager;

namespace FinalWarehouse
{
	class FinalWarehouseManager
	{
		public const string refFileName = "Recipes.json"; //Acts as a "reference" Database
		private static string path;
		private static string folder;
		public static string refPath;

		public static Dictionary<string, DeliveryOrder> temporaryStorageUnit = new Dictionary<string, DeliveryOrder>();
		public static Dictionary<string, WorkOrder> storageUnit = new Dictionary<string, WorkOrder>();

		static void Main(string[] args)
		{
			Console.Title = "Final Warehouse";
			FinalWarehouseServer server = null;
			var port = 8084;
			path = AppDomain.CurrentDomain.BaseDirectory;
			folder = Path.Combine(path, "4_FinalWarehouse");
			refPath = Path.Combine(folder, refFileName);
			Console.WriteLine(folder);
			try
			{
				server = new FinalWarehouseServer(folder, port);
				Console.WriteLine("HELLO! SERVER RUNNING ON 127.0.0.1:{0}", port.ToString());
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				server?.Stop();
				Console.WriteLine("HELLO! SERVER STOPPED ON 127.0.0.1:{0}", port.ToString());
			}
		}

		public static bool ExistWorkOrder(string idWorkOrder)
		{
			return storageUnit.ContainsKey(idWorkOrder);
		}

		public static void AddWorkOrder(WorkOrder wo)
		{
			storageUnit[wo.idWorkOrder] = wo;
		}

		public static void UpdateWorkOrderItems(string idWorkOrder, Dictionary<string, uint> items)
		{
			if(!storageUnit.ContainsKey(idWorkOrder))
			{
				Console.WriteLine("The work order could not be updated : there is no key assigned to it in the storage unit");
			}
			else
			{
				foreach(var product in items)
				{
					if(!storageUnit[idWorkOrder].FinishedProducts.ContainsKey(product.Key))
					{
						storageUnit[idWorkOrder].FinishedProducts[product.Key] = 0;
					}
					storageUnit[idWorkOrder].FinishedProducts[product.Key] += product.Value;
				}
			}

		}
	}
}
