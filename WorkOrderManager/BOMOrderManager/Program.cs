using System;
using System.IO;

namespace BOMOrderManager
{
	class Program
	{
		static void Main(string[] args)
		{
			
			
			BOMOrderManagerServer server = null;
			int port = 8081;
			var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "1_BOMOrderManager");
			Console.WriteLine(folder);
			try
			{
				
				server = new BOMOrderManagerServer(folder, port);
				Console.WriteLine("HELLO! SERVER RUNNING ON 127.0.0.1:{0}", port.ToString());
				
			}
			catch(Exception e)
			{
				Console.WriteLine(e.Message);
				server?.Stop();
				Console.WriteLine("HELLO! SERVER STOPPED ON 127.0.0.1:{0}", args[1]);
			}
		}
	}
}
