using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace WorkOrderManager
{
	internal class Program
	{
		public const string name = "1";
		private const string help = "This is the help manual.";
		private static readonly Dictionary<string, WorkOrder> OpenedWorkOrders = new Dictionary<string, WorkOrder>();
		private static readonly Dictionary<string, WorkOrder> ClosedWorkOrders = new Dictionary<string, WorkOrder>();
		private static string path;
		private static string folder;
		private static string smallfile;
		private static string mediumfile;
		private static string largefile;
		private static string emptyfile;

		private readonly WorkOrderServer localServer;

		private readonly Dictionary<string, KeyValuePair<string, string>> serversMap =
			new Dictionary<string, KeyValuePair<string, string>>();

		private string action = "";
		private uint complexity = 1;
		private string type = "";

		public Program()
		{
			var exeWarehouse1 = Path.Combine(path, "BOMOrderManager.exe");
			const string folderWarehouse1 = "1_BOMOrderManager";
			var exeCarrier = Path.Combine(path, "Carrier.exe");
			const string folderCarrier = "2_Carrier";
			var exeProductionArea = Path.Combine(path, "ProductionArea.exe");
			const string folderProductionArea = "3_ProductionArea";
			var exeWarehouse2 = Path.Combine(path, "FinalWarehouse.exe");
			const string folderWareHouse2 = "4_FinalWarehouse";
			serversMap.Add(exeWarehouse1,
				new KeyValuePair<string, string>(folderWarehouse1, "8081")); //Start executable with parameters
			serversMap.Add(exeCarrier, new KeyValuePair<string, string>(folderCarrier, "8082"));
			serversMap.Add(exeProductionArea,
				new KeyValuePair<string, string>(folderProductionArea, "8083")); //Start executable with parameters
			serversMap.Add(exeWarehouse2,
				new KeyValuePair<string, string>(folderWareHouse2, "8084")); //Start executable with parameters
			localServer = new WorkOrderServer(folder, 8080);
		}

		private static void Main(string[] args)
		{
			Console.Title = "WorkOrder Manager - Main Console";
			Console.WriteLine(
				"Hello, Subject!\n\tToday, we will make some painted chairs! Type \"help\" to get started.");
			path = AppDomain.CurrentDomain.BaseDirectory;
			folder = Path.Combine(path, "0_WorkOrderManager");

			smallfile = Path.Combine(folder, "small.json");
			mediumfile = Path.Combine(folder, "medium.json");
			largefile = Path.Combine(folder, "large.json");
			emptyfile = Path.Combine(folder, "empty.json");
			var p = new Program();
			try
			{
				p.Start();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
			finally
			{
				Console.Write("It is your last chance to save me: ");
				Console.ReadLine();
				p.CleanUpAndClose();
			}
		}

		private void CleanUpAndClose()
		{
			Console.WriteLine("Intenting to save the world from total destruction.");
			localServer.Stop();
		}

		private void Start()
		{
			foreach (var server in serversMap)
				try
				{
					var s = new ProcessStartInfo {Arguments = server.Value.Key + " " + server.Value.Value};
					var process = new Process
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = server.Key,
							Arguments = server.Value.Key + " " + server.Value.Value,
							UseShellExecute = true,
							RedirectStandardOutput = false,
							CreateNoWindow = false
						}
					};
					process.Start();
				}
				catch (Exception e)
				{
					Console.WriteLine("Could not start this process : {0}", Path.GetFileName(server.Key));
					Console.WriteLine(e.Message);
				}


			while (true)
			{
				var readContent = Console.ReadLine();
				if (readContent is null) continue;
				var argv = readContent.Split();
				HandleArguments(argv); //MAJ action, type, complexity
				ExecuteAction(action, type, complexity);
			}
		}

		private static async void ExecuteAction(string act, string t, uint comp)
		{
			switch (act)
			{
				case Action.Create:
					for (var i = 1; i <= comp; ++i)
					{
						Console.WriteLine("Executing action \"{0}\" of type \"{1}\", {2}", act,
							Path.GetFileName(t).Split('.')[0], i);
						var wo = WorkOrder.FromJson(File.ReadAllText(t));
						wo.idWorkOrder += i;
						wo.timeLaunched = DateTime.Now;
						while (OpenedWorkOrders.ContainsKey(wo.idWorkOrder))
							//To spare ourselves from missing or phantom WorkOrders
							wo.idWorkOrder += i;

						var strwo = WorkOrder.ToJson(wo);

						var m = new Message
						{
							action = Message.NetworkAction.Forward,
							source = Message.ApprovedEndpoint.WorkOrderManager,
							destination = Message.ApprovedEndpoint.BOMOrderManager,
							content = strwo
						};
						try
						{
							var response = await HttpClientLayer.getInstance().Post("http://127.0.0.1:8081/", m);
							Console.WriteLine("Received response: {0}", response.content);
						}
						catch (Exception e)
						{
							Console.WriteLine(e.Message);
						}
					}

					break;
				case Action.Help:
					Console.WriteLine(help);
					break;
				default:
					Console.WriteLine(help);
					break;
			}
		}

		private void HandleArguments(IReadOnlyList<string> argv)
		{
			for (var i = 0; i < argv.Count; ++i)
				switch (i)
				{
					case 0:
						switch (argv[i])
						{
							case Action.Create:
							case Action.Help:
								action = argv[i];
								break;
							default:
								action = help;
								break;
						}

						break;
					case 1:
						switch (argv[i])
						{
							case WorkOrderFileType.empty: // start an empty WO
								type = emptyfile;
								break;
							case WorkOrderFileType.small: // start a small WO - 1 item with 1 BOM
								type = smallfile;
								break;
							case WorkOrderFileType.medium: // start a medium WO - 1 item with 2 BOMs
								type = mediumfile;
								break;
							case WorkOrderFileType.large: // start a large WO - 1 item with 3 BOMs
								type = largefile;
								break;
							default:
								action = Action.Help;
								break;
						}

						break;
					case 2:
						//COMPLEXITY : number of times
						try
						{
							//Try to parse the number as an unsigned int. if not possible, send help.
							complexity = uint.Parse(argv[i]);
						}
						catch (Exception e)
						{
							Console.Error.WriteLine(e.Message);
							action = Action.Help;
							complexity = 1;
						}

						break;

					default:
						action = Action.Help;
						break;
				}
		}

		public static WorkOrder GetWorkOrder(string idWorkOrder)
		{
			if (!OpenedWorkOrders.ContainsKey(idWorkOrder))
			{
				Console.WriteLine("The requested WorkOrder is not opened in the current context.");
				return null;
			}

			var retOrder = OpenedWorkOrders[idWorkOrder];
			return retOrder;
		}

		public static void CloseWorkOrder(string idWorkOrder)
		{
			var toClose = GetWorkOrder(idWorkOrder);
			toClose.Close();
			OpenedWorkOrders.Remove(idWorkOrder);
			ClosedWorkOrders[idWorkOrder] = toClose;
		}

		private static class Action
		{
			public const string Create = "create";
			public const string Help = "help";
		}

		private static class WorkOrderFileType
		{
			public const string small = "small";
			public const string medium = "medium";
			public const string large = "large";
			public const string empty = "empty";
		}
	}
}