using Newtonsoft.Json;

namespace WorkOrderManager
{
	internal class Message
	{
		public enum ApprovedEndpoint
		{
			WorkOrderManager = 0,
			BOMOrderManager = 1,
			Carrier = 2,
			ProductionArea = 3,
			FinalWarehouse = 4,
			BOMWarehouse = 5
		}

		public enum NetworkAction
		{
			Error =
				-1, //Lorsqu'on reçoit ceci, une erreur s'est produite. Ne pas tenter de parse le contenu du message.
			Echo = 0, //Lorsqu'on reçoit ceci, on va echo la requête
			Forward = 1, //Lorsqu'on reçoit ceci, on va forwarder la requête à qui de droit.
			Stop = 2, //Lorsqu'on recoit ceci, on va arrêter TOUT.
			Validate = 3, //Lorsqu'on recoit ceci, on va retourner un booleen
			Delivery = 4, //Lorsqu'on recoit ceci, on va effectuer un traitement autre sur le contenu

			Fetch =
				5 //Lorsqu'on recoit ceci, on va éventuellement devoir renvoyer de l'information déduite du contenu du message à l'exppéditeur.
		}

		public NetworkAction action;
		public string content = "";
		public ApprovedEndpoint destination;
		public ApprovedEndpoint source;
		public bool valide = false;

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