using System;
using System.IO;
using System.Threading;

namespace ProductionArea
{
	class Program
	{
		private static string path;
		private static string folder;
		public static string backupFilePath;

		private static void Main(string[] args)
		{


			ProductionAreaServer server = null;
			BackupManager backupManager = null;
			int port = 8083;
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
				Console.WriteLine("Something went wrong initializing the BackupManager. We must abort the mission.");
			}
			try
			{

				server = new ProductionAreaServer(folder, port);
				Console.WriteLine("HELLO! SERVER RUNNING ON 127.0.0.1:{0}", port.ToString());

			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				server?.Stop();
				backupManager?.Stop();
				Console.WriteLine("HELLO! SERVER STOPPED ON 127.0.0.1:{0}", port.ToString());
			}
		}
	}
}
