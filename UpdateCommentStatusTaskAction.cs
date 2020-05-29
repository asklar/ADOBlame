using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace ADOCLI
{
	public class UpdateCommentStatusTaskAction : CommentTaskAction
	{
		public CommentThreadStatus Status
		{
			get;
			set;
		}

		public UpdateCommentStatusTaskAction(int prid)
			: base(prid)
		{
		}

		public override void Do(GitPullRequestCommentThread thread)
		{
			_ = AdoConfiguration.GetClient<GitHttpClient>().UpdateThreadAsync(new GitPullRequestCommentThread
			{
				Status = Status,
				Id = thread.Id
			}, AdoConfiguration.Project, AdoConfiguration.RepositoryId, base.PullRequestId, thread.Id).Result;
		}
	}
}
