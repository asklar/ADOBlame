using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ADOCLI
{
	internal class PullRequestCommentController
	{
		private ExecuteQuery query = new ExecuteQuery();

		private Dictionary<string, WorkItem> tasks;

		public readonly string TitlePrefix = "[DCPP PR Bot]";

		public bool ReflectCommentStatusOntoWorkItemState
		{
			get;
			set;
		}

		public bool ReflectWorkItemStateOntoCommentStatus
		{
			get;
			set;
		}

		public int PullRequestId
		{
			get;
		}

		public int MaxPriorityToFile
		{
			get;
			set;
		} = 3;


		public PullRequestCommentController(int prID)
		{
			PullRequestId = prID;
		}

		public void Process()
		{
			tasks = query.RunGetBugsQueryUsingClientLib(TitlePrefix, new string[1]
			{
				"DCPP"
			}, $" [Microsoft.VSTS.Common.CustomString01] == '{PullRequestId}' ").Result;
			query.ProcessPullRequestComments(PullRequestId, EnsureTaskForCommentThread);
		}

		private CommentTaskAction EnsureTaskForCommentThread(GitPullRequestCommentThread arg)
		{
			string title = $"PR ID {PullRequestId}, Thread {arg.Id}";
			int priority;
			string description = GetDescriptionForComment(arg, out priority);
			if (priority > MaxPriorityToFile)
			{
				return null;
			}
			WorkItem workItem3;
			if (tasks.Keys.Any((string t) => tasks[t].Fields.ContainsKey("Microsoft.VSTS.Common.CustomString02") && tasks[t].Fields["Microsoft.VSTS.Common.CustomString02"] as string== arg.Id.ToString()))
			{
				workItem3 = tasks.Where((KeyValuePair<string, WorkItem> kv) => tasks[kv.Key].Fields["Microsoft.VSTS.Common.CustomString02"] as string== arg.Id.ToString()).First().Value;
			}
			else if (tasks.ContainsKey(title))
			{
				workItem3 = tasks[title];
			}
			else
			{
				workItem3 = query.CreateWorkItem(TitlePrefix, title, priority, new string[1]
				{
					"DCPP"
				}, description).Result;
				workItem3 = query.UpdateWorkItemField(workItem3, "Custom String 01", PullRequestId.ToString()).Result;
				tasks[title] = workItem3;
			}
			if (!workItem3.Fields.ContainsKey("Microsoft.VSTS.Common.CustomString02"))
			{
				workItem3 = query.UpdateWorkItemField(tasks[title], "Custom String 02", arg.Id.ToString()).Result;
			}
			string comment = $"{TitlePrefix}Created task: #{tasks[title].Id}";
			UpdateCommentStatusTaskAction task;
			if (!query.GetRelevantComments(arg).Any((Comment x) => x.Content.StartsWith(TitlePrefix)))
			{
				task = new CreateCommentTaskAction(PullRequestId)
				{
					Comment = comment
				};
			}
			else
			{
				Dictionary<string, CommentThreadStatus> workItemCommentStateMapping = new Dictionary<string, CommentThreadStatus>
				{
					{
						"Proposed",
						CommentThreadStatus.Pending
					},
					{
						"Cut",
						CommentThreadStatus.WontFix
					},
					{
						"Completed",
						CommentThreadStatus.Fixed
					}
				};
				Dictionary<CommentThreadStatus, string> commentWorkItemStateMapping = new Dictionary<CommentThreadStatus, string>();
				foreach (KeyValuePair<string, CommentThreadStatus> entry in workItemCommentStateMapping)
				{
					commentWorkItemStateMapping.Add(entry.Value, entry.Key);
				}
				task = new UpdateCommentStatusTaskAction(PullRequestId);
				if (ReflectWorkItemStateOntoCommentStatus && (arg.Status == CommentThreadStatus.Active || arg.Status == CommentThreadStatus.Pending))
				{
					task.Status = workItemCommentStateMapping.GetValueOrDefault(tasks[title].Fields["System.State"] as string, CommentThreadStatus.Active);
				}
				else if (ReflectCommentStatusOntoWorkItemState)
				{
					string newWorkItemState = commentWorkItemStateMapping.GetValueOrDefault(arg.Status, "Proposed");
					_ = query.UpdateWorkItemField(tasks[title], "System.State", newWorkItemState).Result;
				}
				_ = query.UpdateWorkItemField(tasks[title], "System.Description", description).Result;
				if (arg.Status == task.Status || task.Status == CommentThreadStatus.Unknown)
				{
					task = null;
				}
			}
			return task;
		}

		public static string GetThreadContextDescription(CommentThreadContext context)
		{
			if (context == null)
			{
				return "<no context>";
			}
			int start = context.RightFileStart?.Line ?? context.LeftFileStart.Line;
			int end = context.RightFileEnd?.Line ?? context.LeftFileEnd.Line;
			return $"{context.FilePath} ({start}-{end})";
		}

		private string GetDescriptionForComment(GitPullRequestCommentThread arg, out int priority)
		{
			IEnumerable<Comment> comments = query.GetRelevantComments(arg);
			priority = query.GetPriority(comments.FirstOrDefault());
			string commentHeader = "<div><h3 style=\"display:inline;\">File:</h3>" + GetThreadContextDescription(arg.ThreadContext) + "</div><br><br>";
			string commentText = string.Join(new string('=', 20) + "\n\n", comments.Select((Comment x) => "<b>" + x.Author.DisplayName + "</b>: " + HttpUtility.HtmlEncode(x.Content.Substring(x.Content.IndexOf(']') + 1).Trim()) + "<br>"));
			string linkToComment = string.Format("https://microsoft.visualstudio.com/{0}/_git/{1}/pullrequest/{2}?_a=files&path={3}&discussionId={4}", AdoConfiguration.Project, AdoConfiguration.RepositoryId, PullRequestId, HttpUtility.UrlEncode(arg.ThreadContext?.FilePath ?? ""), arg.Id);
			return commentHeader + "<div>" + commentText + "</div><div><a href=\"" + linkToComment + "\">View comment</a></div>";
		}
	}
}
