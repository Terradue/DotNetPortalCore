using System;
using Terradue.Portal;

namespace Terradue.Cloud {
    [EntityTable("usr_cloud", EntityTableConfiguration.Custom, IsRequired = false)]
    public class CloudUser : User {

        [EntityDataField("id")]
        public int UserId { get; set; }


        [EntityDataField("id_provider")]
        public int ProviderId { get; set; }

        [EntityDataField("username")]
        public string CloudUsername { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public override string GetIdentifyingConditionSql(){
            if (UserId != 0 && ProviderId != 0) return String.Format("t1.id={0} AND t1.id_provider={1}",UserId,ProviderId); 
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public CloudUser(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public static CloudUser FromIdAndProvider(IfyContext context, int usrId, int providerId){
            CloudUser result = new CloudUser(context);
            result.UserId = usrId;
            result.ProviderId = providerId;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

    }
}

