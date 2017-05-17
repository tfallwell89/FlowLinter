using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowLinter
{
	public static class FlowRunner
	{
		private static StringBuilder _sb = new StringBuilder();
		private static string CONFIG = ".flowconfig";
		private static string RUN_GLOBAL = "/C flow --json";
		private static string RUN_FILE = "flow check-contents";


		public static void Run(string path)
		{
			Run(path, RUN_GLOBAL);
		}

		internal static void RunOnOpenFile(string filePath, IEnumerable<ITextSnapshotLine> lines)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var line in lines)
				sb.Append(line.GetText());

			var file = sb.ToString();
			Run(filePath, string.Format("/C echo {2} | {0} --json", RUN_FILE, filePath, file));
		}

		private static void Run(string path, string command)
		{
			try
			{
				_sb.Clear();
				var proc = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						WorkingDirectory = GetProjectDirFromFile(path),
						WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
						FileName = "cmd.exe",
						Arguments = command,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						CreateNoWindow = true
					}
				};

				proc.Start();
				while (!proc.StandardOutput.EndOfStream)
				{
					_sb.Append(proc.StandardOutput.ReadLine());
				}

				FlowParser.Json = _sb.ToString();
			}
			catch (Exception e)
			{
				//log error
				Console.WriteLine("exec exception");
			}
			finally
			{
				_sb.Clear();
			}
		}

		private static string GetProjectDirFromFile(string path)
		{
			DirectoryInfo dr = new DirectoryInfo(path).Parent;
			DirectoryInfo info = null;
			int count = 100;
			bool found = false;

			while (!found && count-- > 0)
			{
				foreach (var file in dr.GetFiles())
				{
					if (file.Extension.Equals(CONFIG))
					{
						info = file.Directory;
						found = true;
					}
				}
				dr = dr.Parent;
			}

			return info.FullName;
		}
	}
}
