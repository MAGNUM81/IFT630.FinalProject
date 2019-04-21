using System;
using System.IO;

namespace ProductionArea
{
	internal class Program
	{
		public const string refFileName = "Recipes.json"; //Acts as a "reference" Database
		private static string path;
		private static string folder;
		public static string refPath;

		private static void Main(string[] args)
		{


			ProductionAreaServer server = null;
			int port = 8081;
			path = AppDomain.CurrentDomain.BaseDirectory;
			folder = Path.Combine(path, "1_BOMOrderManager");
			refPath = Path.Combine(folder, refFileName);
			Console.WriteLine(folder);
			try
			{

				server = new ProductionAreaServer(folder, port);
				Console.WriteLine("HELLO! SERVER RUNNING ON 127.0.0.1:{0}", port.ToString());

			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				server?.Stop();
				Console.WriteLine("HELLO! SERVER STOPPED ON 127.0.0.1:{0}", port.ToString());
			}
		}
	}
}
