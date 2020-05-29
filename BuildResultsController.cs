using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ADOCLI
{
	internal class BuildResultsController
	{
		private static readonly string TitlePrefix = "[DCPPBuild]";

		private Dictionary<string, bool> projectStatus = new Dictionary<string, bool>();

		private ExecuteQuery query = new ExecuteQuery();

		public string builtOkPath
		{
			get;
			set;
		}

		public string buildFailedPath
		{
			get;
			set;
		}

		internal string Prefix
		{
			get;
			set;
		}

		private string RepoRoot
		{
			get
			{
				string labSourcePath = Environment.GetEnvironmentVariable("BUILD_REPOSITORY_LOCALPATH");
				if (!string.IsNullOrEmpty(labSourcePath))
				{
					return labSourcePath;
				}
				string location = Assembly.GetExecutingAssembly().Location;
				return location.Substring(0, location.IndexOf("\\Obj"));
			}
		}

		private string Sanitize(string input)
		{
			if (Prefix != null)
			{
				input = input.Replace(Prefix, "");
			}
			return input.Trim();
		}

		private void ProcessFile(string path, bool succeeded)
		{
			try
			{
				foreach (string line in File.ReadLines(path))
				{
					string project = Sanitize(line);
					projectStatus[project] = succeeded;
				}
			}
			catch (FileNotFoundException)
			{
				Console.WriteLine("File " + path + " not found, ignoring.");
			}
		}

		public void Process()
		{
			Console.WriteLine("Parsing built and failed directory logs");
			ProcessFile(buildFailedPath, succeeded: false);
			ProcessFile(builtOkPath, succeeded: true);
			Console.WriteLine("Querying ADO for existing tracking tasks");
			Dictionary<string, WorkItem> allItems = query.RunGetBugsQueryUsingClientLib(TitlePrefix, new string[1]
			{
				"DCPP"
			}).Result;
			Console.WriteLine("Updating items");
			int created = 0;
			int regressions = 0;
			int fixes = 0;
			int processed = 0;
			foreach (string project in projectStatus.Keys)
			{
				if (allItems.ContainsKey(project))
				{
					string adoState = allItems[project].Fields["System.State"] as string;
					bool changed = false;
					if (adoState == "Completed" && !projectStatus[project])
					{
						Console.WriteLine("REGRESSION: " + project);
						changed = true;
						regressions++;
						_ = query.UpdateWorkItemField(allItems[project], "Custom String 01", DateTime.Now.ToString()).Result;
					}
					else if (adoState == "Proposed" && projectStatus[project])
					{
						changed = true;
						Console.WriteLine("FIXED: " + project);
						fixes++;
						_ = query.UpdateWorkItemField(allItems[project], "Custom String 01", "").Result;
						_ = query.UpdateWorkItemField(allItems[project], "Custom String 02", DateTime.Now.ToString()).Result;
					}
					if (changed)
					{
						_ = query.EnsureWorkItemState(allItems[project], projectStatus[project] ? "Completed" : "Proposed").Result;
					}
				}
				else if (!projectStatus[project])
				{
					WorkItem wi = query.CreateWorkItem(TitlePrefix, project, 0, new string[1]
					{
						"DCPP"
					}).Result;
					Console.WriteLine($"({++created}) Created task {wi.Id} for project {project}");
					allItems[project] = wi;
				}
				if (!projectStatus[project])
				{
					UpdateTaskDescriptionWithMSBuildLog(project, allItems[project]);
				}
				Console.WriteLine($"Processed item {++processed} of {projectStatus.Count}");
			}
			Console.WriteLine($"Failed to build {projectStatus.Count((KeyValuePair<string, bool> x) => !x.Value)} projects.");
			Console.WriteLine($"{created} new tasks created, {regressions} regressions, {fixes} fixes.");
		}

		private WorkItem UpdateTaskDescriptionWithMSBuildLog(string project, WorkItem wi)
		{
			string buildLogFile = Path.Combine(RepoRoot, Path.GetDirectoryName(project), "msbuild.log");
			string buildLogContent = "";
			if (File.Exists(buildLogFile))
			{
				buildLogContent = File.ReadAllText(buildLogFile);
			}
			else if (File.Exists(Path.Combine(RepoRoot, "dxaml", Path.GetDirectoryName(project), "msbuild.log")))
			{
				buildLogContent = File.ReadAllText(Path.Combine(RepoRoot, "dxaml", Path.GetDirectoryName(project), "msbuild.log"));
			}
			else
			{
				Console.WriteLine("Couldn't find msbuild.log for project " + project);
			}
			wi = query.UpdateWorkItemField(wi, "System.Description", buildLogContent).Result;
			return wi;
		}
	}
}
