using System;
using System.Net;

namespace Terradue.Portal {
    [EntityTable("gitrepo", EntityTableConfiguration.Custom, HasPermissionManagement = true, HasOwnerReference = true, HasDomainReference = true)]
    public class GitRepository : Entity {

        [EntityDataField("kind")]
        public int Kind { get; set; }

        [EntityDataField("url")]
        public string Url { get; set; }

        public GitRepository(IfyContext context) : base(context) { }

        public static GitRepository FromId(IfyContext context, int id) {
            GitRepository repo = new GitRepository(context);
            repo.Id = id;
            repo.Load();
            return repo;
        }

        public override void Store() {
            //check url is not null
            if (string.IsNullOrEmpty(this.Url)) throw new Exception("Invalid Git repository url");

            //check url finishes with .git
            try {
                var urib = new UriBuilder(this.Url);
                if (!urib.Path.EndsWith(".git", StringComparison.CurrentCulture)) throw new Exception("Invalid Git repository url");                
            } catch (Exception e) {
                throw new Exception("Invalid Git repository url");
            }

            //check url exists
            var request = (HttpWebRequest)WebRequest.Create(this.Url);
            request.Method = "HEAD";
            try {                
                System.Threading.Tasks.Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse,request.EndGetResponse,null)
                .ContinueWith(task =>
                {
                    var httpResponse = (HttpWebResponse)task.Result;                
                }).ConfigureAwait(false).GetAwaiter().GetResult();     
            } catch (Exception e) {
                throw new Exception("Invalid Git repository url");
            }

            base.Store();
        }
    }

    public class GitRepositoryKind {
        public static int GITLAB = 1;
        public static int GITHUB = 2;
    }

}
