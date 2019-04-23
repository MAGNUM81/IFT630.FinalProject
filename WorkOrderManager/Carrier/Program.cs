using System;
using System.IO;

namespace Carrier
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			Console.Title = "Carrier Service.";
			CarrierServer server = null;
			int port = 8082;
			var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "2_Carrier");
			Console.WriteLine(folder);
			try
			{

				server = new CarrierServer(folder, port);
				Console.WriteLine("HELLO! SERVER RUNNING ON 127.0.0.1:{0}", port.ToString());

			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				server?.Stop();
				Console.WriteLine("HELLO! SERVER STOPPED ON 127.0.0.1:{0}", args[1]);
			}

		}
	}
}
