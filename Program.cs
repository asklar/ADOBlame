using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Principal;

namespace ADOCLI
{
	internal class Program
	{
		private void Diagnostics()
		{
			Console.WriteLine(string.Format("Token length = {0}", Environment.GetEnvironmentVariable("OAUTH_TOKEN").Length));
			ExecuteQuery executeQuery = new ExecuteQuery();
			Console.WriteLine("Identity: " + WindowsIdentity.GetCurrent().Name.Split('\\')[1] + "@microsoft.com");
			Dictionary<string, WorkItem> result = executeQuery.RunGetBugsQueryUsingClientLib("[DCPPBuild]", new string[1]
			{
				"DCPP"
			}).Result;
			Console.WriteLine($"{result.Count} elements");
			foreach (string title in result.Keys)
			{
				Console.WriteLine($"Workitem {title} -> {result[title].Id}");
			}
		}

		private static void Main(string[] args)
		{
			Program p = new Program();
			Console.WriteLine("ADOBlame - built on " + File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToShortDateString() + " - Send feedback to asklar@microsoft.com");
			LoggerCallbackHandler.UseDefaultLogging = (Environment.GetEnvironmentVariable("BUILD_REPOSITORY_LOCALPATH") != null);
			string tag = "DEP-AppCompat-Triaged";
			int id = -1;
			if (args.Length == 1 && args[0] == "/diag")
			{
				p.Diagnostics();
				return;
			}
			if (args.Length == 1 && (args[0] == "/h" || args[0] == "/?"))
			{
				Console.WriteLine("Usage: ");
				Console.WriteLine("\tBlameADOTag [/tag <tagname>] [/id <bugid>]");
				Console.WriteLine("\tBlameADOTag [/tag \"<field=value>\"] [/id <bugid>]");
				Console.WriteLine();
				return;
			}
			if (args.Length >= 2)
			{
				for (int i = 0; i < args.Length; i += 2)
				{
					switch (args[i])
					{
					case "/tag":
						tag = args[i + 1];
						break;
					case "/id":
						id = int.Parse(args[i + 1]);
						break;
					}
				}
			}
			if (tag.StartsWith('"') && tag.EndsWith('"'))
			{
				tag = tag.Substring(1, tag.Length - 2);
			}
			ChangeBlameController controller = new ChangeBlameController(tag);
			if (id != -1)
			{
				controller.SetBugId(id);
			}
			controller.Process();
		}
	}
}
