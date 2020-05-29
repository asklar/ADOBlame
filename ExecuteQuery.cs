using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
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
