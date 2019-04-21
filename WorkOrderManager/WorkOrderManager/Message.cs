using Newtonsoft.Json;

namespace WorkOrderManager
{
	internal class Message
	{
		public enum NetworkAction
		{
			Error = -1,		//Lorsqu'on reçoit ceci, une erreur s'est produite. Ne pas tenter de parse le contenu du message.
			Echo = 0,		//Lorsqu'on reçoit ceci, on va echo la requête
			Forward = 1,    //Lorsqu'on reçoit ceci, on va forwarder la requête à qui de droit.
			Stop = 2,       //Lorsqu'on recoit ceci, on va arrêter TOUT.
			Validate = 3,   //Lorsqu'on recoit ceci, on va retourner un booleen
			Delivery = 4    //Lorsqu'on recoit ceci, on va effectuer un traitement autre sur le contenu

		}
		public enum ApprovedEndpoint
		{
			WorkOrderManager = 0,
			BOMOrderManager = 1,
			Carrier = 2,
			ProductionArea = 3,
			FinalWarehouse = 4,
			BOMWarehouse = 5
		}
		public NetworkAction action;
		public bool valide = false;
		public string content = "";
		public ApprovedEndpoint source;
		public ApprovedEndpoint destination;

		public override string ToString()
		{
			return ToJson(this);
		}

		internal static string ToJson(Message message)
		{
			var m = JsonConvert.SerializeObject(message);
			return m;
		}

		internal static Message FromJson(string jsonMessage)
		{
			var m = JsonConvert.DeserializeObject<Message>(jsonMessage);
			return m;
		}
	}
}
