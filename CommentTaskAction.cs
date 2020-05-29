using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace ADOCLI
{
	public abstract class CommentTaskAction
	{
		public int PullRequestId
		{
			get;
			private set;
		}

		public abstract void Do(GitPullRequestCommentThread thread);

		public CommentTaskAction(int prid)
		{
			PullRequestId = prid;
		}
	}
}
