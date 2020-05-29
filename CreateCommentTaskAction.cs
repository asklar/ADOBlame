using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System;

namespace ADOCLI
{
	public class CreateCommentTaskAction : UpdateCommentStatusTaskAction
	{
		public string Comment
		{
			get;
			set;
		}

		public CreateCommentTaskAction(int prid)
			: base(prid)
		{
			base.Status = CommentThreadStatus.Pending;
		}

		public override void Do(GitPullRequestCommentThread thread)
		{
			Comment newComment = new Comment
			{
				CommentType = CommentType.Text,
				Content = Comment,
				IsDeleted = false,
				ParentCommentId = thread.Comments[0].Id,
				PublishedDate = DateTime.Now
			};
			newComment.Author = new IdentityRef
			{
				DisplayName = "DCPP PR Bot",
				IsAadIdentity = false
			};
			_ = AdoConfiguration.GetClient<GitHttpClient>().CreateCommentAsync(newComment, AdoConfiguration.Project, AdoConfiguration.RepositoryId, base.PullRequestId, thread.Id).Result;
			base.Do(thread);
			Console.WriteLine("Replied to PR: " + PullRequestCommentController.GetThreadContextDescription(thread.ThreadContext) + " --> " + Comment);
		}
	}
}
