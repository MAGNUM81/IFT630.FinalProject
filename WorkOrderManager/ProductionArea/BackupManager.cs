using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ProductionArea
{
	internal class BackupManager
	{
		//TODO : all this is a bit dumb. Fix it asap
		private static string backupFilePath;
		private static Queue<string> toWrite;
		private readonly Thread backupThread;
		private static bool aborting;
		public BackupManager(string filepath)
		{
			backupFilePath = filepath;
			backupThread = new Thread(() => Run(this));
			toWrite = new Queue<string>();
			backupThread.Start();
		}

		private static void Run(BackupManager bm)
		{
			while (true)
			{
				if (aborting)
				{
					//if the BackupManager is closing, we still want it to write everything it has in its buffer.
					while (toWrite.Count != 0)
					{
						var s = toWrite.Dequeue();
						_write(s);
					}
					break;
				}

				if (toWrite.Count == 0) continue;
				var c = toWrite.Dequeue();
				_write(c);
			}
		}

		private static void _write(string s)
		{
			using (var newTask = new StreamWriter("test.txt", false)){ 
				newTask.WriteLine(s);
			}
		}

		private static void OverwriteAll(string content)
		{
			toWrite.Enqueue(content);
		}

		private static string Read()
		{
			return File.ReadAllText(backupFilePath);
		}

		public void Stop()
		{
			Console.WriteLine("The BackupManager is closing down.");
			aborting = true;
			backupThread.Join(); //Force the writing queue to be emptied. TODO : We could put a timeout here, just in case.
		}

		public void queueBackupInstances(IEnumerable<BackupInstance> backupInstances)
		{
			var strToOverwrite = backupInstances.Aggregate("", (current, bi) => current + BackupInstance.ToJson(bi));
			OverwriteAll(strToOverwrite);
		}

	}
}
