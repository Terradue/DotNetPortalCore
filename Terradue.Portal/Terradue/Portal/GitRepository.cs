using System;
namespace Terradue.Portal {
    [EntityTable("gitrepo", EntityTableConfiguration.Custom, HasPermissionManagement = true, HasOwnerReference = true, HasDomainReference = true)]
    public class GitRepository : Entity {

        [EntityDataField("url")]
        public string Url { get; set; }

        public GitRepository(IfyContext context) : base(context) { }

        public static GitRepository FromId(IfyContext context, int id) {
            GitRepository repo = new GitRepository(context);
            repo.Id = id;
            repo.Load();
            return repo;
        }
    }
}
