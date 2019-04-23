using System;
using System.IO;
using System.Threading;

namespace ProductionArea
{
	internal class Program
	{
		private static string path;
		private static string folder;
		public static string backupFilePath;
		private static ProductionAreaServer server = null;
		private static BackupManager backupManager = null;
		private static ProductionAreaManager prodManager = null;


		private static void Main(string[] args)
		{
			int port = 8083;
			Console.Title = "Production Area";
			path = AppDomain.CurrentDomain.BaseDirectory;
			folder = Path.Combine(path, "3_ProductionArea");
			backupFilePath = Path.Combine(folder, "Backup.json");
			if(!File.Exists(backupFilePath))
			{
				File.Create(backupFilePath);
			}
			Console.WriteLine(folder);
			try
			{
				backupManager = new BackupManager(backupFilePath); //runs on its own thread
			}catch(Exception e)
			{
				AbortInit();
				Console.WriteLine("Something went wrong initializing the BackupManager. We must abort the mission.");
				Console.WriteLine(e.Message);
			}
			
			try
			{

				server = new ProductionAreaServer(folder, port);
				Console.WriteLine("HELLO! PRODUCTION AREA SERVER RUNNING ON 127.0.0.1:{0}", port.ToString());

			}
			catch (Exception e)
			{
				AbortInit();
				Console.WriteLine("HELLO! SERVER STOPPED ON 127.0.0.1:{0}", port.ToString());
				Console.WriteLine(e.Message);
			}

			try
			{
				prodManager = new ProductionAreaManager(ref server); //This runs on the main thread... should it though? like. everything else has its own thread so...
				Console.WriteLine("HELLO! PRODUCTION AREA MANAGER UP AND RUNNING.");
			}catch(Exception e)
			{
				Console.WriteLine("Something went wrong initializing and/or running the Production Area Manager. We must abort the mission.");				
				Console.WriteLine(e.Message);
				AbortInit();
			}

			try
			{
				prodManager.Start();
			}catch(Exception e)
			{
				Console.WriteLine("Something went wrong while running the Production Area Manager. We must abort the mission.");
				Console.WriteLine(e.Message);
				AbortInit();
			}
			
		}
		private static void AbortInit()
		{
			//This is the kill switch.
			prodManager?.Stop();
			server?.Stop();
			backupManager?.Stop();
		}
	}
}
