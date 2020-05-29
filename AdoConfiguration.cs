using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Security.Principal;

namespace ADOCLI
{
	public static class AdoConfiguration
	{
		public static readonly string Uri = "https://microsoft.visualstudio.com/";

		public static readonly string Project = "OS";

		public static readonly string RepositoryId = "os";

		public static readonly string AreaPath = "OS\\Core\\DEP\\WinDev\\FxCS";

		private static readonly VssAadCredential _userCredentials = new VssAadCredential(WindowsIdentity.GetCurrent().Name.Split('\\')[1] + "@winse.microsoft.com");

		private static readonly VssCredentials _labCredentials = (Environment.GetEnvironmentVariable("OAUTH_TOKEN") == null) ? null : new VssOAuthAccessTokenCredential(Environment.GetEnvironmentVariable("OAUTH_TOKEN"));

		private static VssCredentials Credentials
		{
			get
			{
				if (Environment.GetEnvironmentVariable("OAUTH_TOKEN") != null && _labCredentials != null)
				{
					return _labCredentials;
				}
				return _userCredentials;
			}
		}

		internal static T GetClient<T>() where T : VssHttpClientBase
		{
			return new VssConnection(new Uri(Uri), Credentials).GetClient<T>();
		}
	}
}
