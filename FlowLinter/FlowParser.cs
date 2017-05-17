using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowLinter
{
	public static class FlowParser
	{

		public static Dictionary<int, dynamic> ErrorLineNumbers = new Dictionary<int, dynamic>();
		public static string Json = "";

		public static void Parse()
		{
			try
			{
				ErrorLineNumbers = new Dictionary<int, dynamic>();
				//returns an array of errors
				if (string.IsNullOrEmpty(Json)) return;

				dynamic jsonObj = JObject.Parse(Json);

				foreach (dynamic error in jsonObj.errors)
				{
					//errors has an array of messages
					foreach (dynamic message in error.message)
					{
						if (!ErrorLineNumbers.ContainsKey((int)message.line))
							ErrorLineNumbers.Add((int)message.line, message);
					}
				}
			}
			catch (Exception e)
			{
				//log error
				Console.WriteLine("exception!");
			}
		}
	}
}
