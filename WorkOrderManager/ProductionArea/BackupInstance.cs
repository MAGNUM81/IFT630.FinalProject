using System.Collections.Generic;
using Newtonsoft.Json;

namespace ProductionArea
{
	internal class BackupInstance
	{
		public string componentType = "";
		public int errorCode = 0; //If any. 0 being OK.
		public string errorMessage = ""; //If any. informative only.
		public string idComponent = "";
		public string idTask = "";
		public Dictionary<string, uint> itemsHandled = new Dictionary<string, uint>(); //ID and quantity
		public string state = "";

		public static BackupInstance FromJson(string json)
		{
			return JsonConvert.DeserializeObject<BackupInstance>(json);
		}

		public static string ToJson(BackupInstance bi)
		{
			return JsonConvert.SerializeObject(bi);
		}
	}
}