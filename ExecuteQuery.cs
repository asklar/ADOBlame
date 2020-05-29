using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADOCLI
{
	public class ExecuteQuery
	{
		public IEnumerable<Comment> GetRelevantComments(GitPullRequestCommentThread thread)
		{
			return thread.Comments.Where((Comment x) => x.CommentType == CommentType.Text && !x.IsDeleted);
		}

		public int GetPriority(Comment c)
		{
			if (c == null)
			{
				return 100;
			}
			foreach (KeyValuePair<string, int> entry in new Dictionary<string, int>
			{
				{
					"Fix",
					0
				},
				{
					"Resolve",
					0
				},
				{
					"Fix/Resolve",
					0
				},
				{
					"Track",
					1
				},
				{
					"Nit",
					2
				}
			})
			{
				if (c.Content.StartsWith("[" + entry.Key + "]"))
				{
					return entry.Value;
				}
			}
			return 3;
		}

		public void ProcessPullRequestComments(int prID, Func<GitPullRequestCommentThread, CommentTaskAction> func)
		{
			using (AdoConfiguration.GetClient<GitHttpClient>())
			{
				IOrderedEnumerable<GitPullRequestCommentThread> orderedEnumerable = from t in AdoConfiguration.GetClient<GitHttpClient>().GetThreadsAsync(AdoConfiguration.Project, AdoConfiguration.RepositoryId, prID).Result
					where !t.IsDeleted
					orderby GetPriority(t.Comments.Where((Comment c) => c.CommentType == CommentType.Text && !c.IsDeleted).FirstOrDefault())
					select t;
				orderedEnumerable.Count();
				foreach (GitPullRequestCommentThread thread in orderedEnumerable)
				{
					func(thread)?.Do(thread);
				}
			}
		}

		private void PrintCommentThread(GitPullRequestCommentThread thread)
		{
			Comment c = GetRelevantComments(thread).FirstOrDefault();
			int priority = GetPriority(c);
			string content = c.Content.Substring(c.Content.IndexOf(']') + 1).Trim();
			Console.WriteLine(string.Format("P{0} {1}({2}). File: {3} \"{4}\"", priority, thread.Id, c.Author.DisplayName, thread.ThreadContext?.FilePath ?? "<no path>", content));
		}

		public async Task<WorkItem> CreateWorkItem(string prefix, string title, int priority, IEnumerable<string> tags, string description = null)
		{
			using (WorkItemTrackingHttpClient client = AdoConfiguration.GetClient<WorkItemTrackingHttpClient>())
			{
				JsonPatchDocument doc = new JsonPatchDocument();
				doc.Add(new JsonPatchOperation
				{
					Path = "/fields/System.Title",
					Operation = Operation.Add,
					Value = prefix + " " + title
				});
				doc.Add(new JsonPatchOperation
				{
					Path = "/fields/Assigned To",
					Operation = Operation.Add,
					Value = "Alexander Sklar"
				});
				doc.Add(new JsonPatchOperation
				{
					Path = "/fields/Area Path",
					Operation = Operation.Add,
					Value = AdoConfiguration.AreaPath
				});
				foreach (string tag in tags)
				{
					doc.Add(new JsonPatchOperation
					{
						Path = "/fields/Tags",
						Operation = Operation.Add,
						Value = tag
					});
				}
				doc.Add(new JsonPatchOperation
				{
					Path = "/fields/Product Family",
					Operation = Operation.Add,
					Value = "Applications"
				});
				doc.Add(new JsonPatchOperation
				{
					Path = "/fields/Product",
					Operation = Operation.Add,
					Value = "WinUI"
				});
				doc.Add(new JsonPatchOperation
				{
					Path = "/fields/Release",
					Operation = Operation.Add,
					Value = "Vibranium"
				});
				doc.Add(new JsonPatchOperation
				{
					Path = "/fields/Priority",
					Operation = Operation.Add,
					Value = priority
				});
				if (description != null)
				{
					doc.Add(new JsonPatchOperation
					{
						Path = "/fields/System.Description",
						Operation = Operation.Add,
						Value = description
					});
				}
				return await client.CreateWorkItemAsync(doc, "OS", "Task");
			}
		}

		public async Task<Dictionary<string, WorkItem>> RunGetBugsQueryUsingClientLib(string prefix, IEnumerable<string> tag, string customWiqlSegment = null)
		{
			string andTagsContains = string.Join(' ', tag.Select((string x) => "And [Tags] CONTAINS '" + x + "' "));
			Wiql wiql2 = new Wiql();
			wiql2.Query = "Select [State], [Id] From WorkItems Where [Work Item Type] = 'Task' And [Area Path] = '" + AdoConfiguration.AreaPath + "' " + andTagsContains + "And [Title] CONTAINS '" + prefix + "' And [System.State] <> 'Closed' " + ((customWiqlSegment != null) ? ("And " + customWiqlSegment + " ") : "") + "Order By [State] Asc, [Changed Date] Desc";
			Wiql wiql = wiql2;
			using (WorkItemTrackingHttpClient workItemTrackingHttpClient = AdoConfiguration.GetClient<WorkItemTrackingHttpClient>())
			{
				WorkItemQueryResult workItemQueryResult = await workItemTrackingHttpClient.QueryByWiqlAsync(wiql);
				if (workItemQueryResult.WorkItems.Count() != 0)
				{
					string[] fields = new string[5]
					{
						"System.Id",
						"System.Title",
						"System.State",
						"System.Description",
						"Microsoft.VSTS.Common.CustomString02"
					};
					Dictionary<string, WorkItem> result = new Dictionary<string, WorkItem>();
					for (int i = 0; i < workItemQueryResult.WorkItems.Count(); i += 200)
					{
						IEnumerable<WorkItemReference> setOfIds = workItemQueryResult.WorkItems.Skip(i).Take(200);
						foreach (WorkItem workItem in await workItemTrackingHttpClient.GetWorkItemsAsync(setOfIds.Select((WorkItemReference x) => x.Id), fields, workItemQueryResult.AsOf))
						{
							string project = (workItem.Fields["System.Title"] as string).Substring(prefix.Length).Trim();
							result[project] = workItem;
						}
					}
					return result;
				}
				return new Dictionary<string, WorkItem>();
			}
		}

		public async Task<WorkItem> UpdateWorkItemField(WorkItem item, string field, string value)
		{
			using (WorkItemTrackingHttpClient client = AdoConfiguration.GetClient<WorkItemTrackingHttpClient>())
			{
				JsonPatchDocument update = new JsonPatchDocument();
				update.Add(new JsonPatchOperation
				{
					Path = "/fields/" + field,
					Operation = Operation.Add,
					Value = value
				});
				return await client.UpdateWorkItemAsync(update, item.Id.Value);
			}
		}

		public async Task<WorkItem> EnsureWorkItemState(WorkItem item, string v)
		{
			return await UpdateWorkItemField(item, "System.State", v);
		}
	}
}
