using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using System;
using System.IO;

namespace ADOCLI
{
    public class AdoConfiguration
    {
        private AdoConfiguration()
        {
        }

        public static AdoConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = JsonConvert.DeserializeObject<AdoConfiguration>(File.ReadAllText("adoblame.json"));
                }
                return _instance;
            }
        }

        public static AdoConfiguration _instance = null;
        public string Uri { get; set; }
        public string Username { get; set; }
        public string OAuthToken { get; set; }


        private VssCredentials Credentials
        {
            get
            {
                string token = Environment.GetEnvironmentVariable("OAUTH_TOKEN") ?? OAuthToken;
                if (!string.IsNullOrEmpty(token))
                {
                    return new VssOAuthAccessTokenCredential(token);
                }
                else
                {
                    return new VssAadCredential(Username);
                }
            }
        }

        public bool Debug { get; internal set; }
        public bool ShowDetails { get; internal set; }

        internal static T GetClient<T>() where T : VssHttpClientBase
        {
            return new VssConnection(new Uri(Instance.Uri), Instance.Credentials).GetClient<T>();
        }
    }
}
