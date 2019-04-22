using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ProductionArea
{
	class BackupInstance
	{
		public string idComponent = "";
		public string componentType = "";
		public string state = "";
		public string idTask = "";
		public Dictionary<string, uint> itemsHandled= new Dictionary<string, uint>();	 //ID and quantity
		public int errorCode = 0; //If any. 0 being OK.
		public string errorMessage = ""; //If any. informative only.

		public BackupInstance()
		{

		}

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
