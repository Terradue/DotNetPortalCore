using System;

namespace Terradue.Portal {

    [EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above)]
    public abstract class ExternalWpsProcess : Service {

        //---------------------------------------------------------------------------------------------------------------------

        public ExternalWpsProcess(IfyContext context) : base(context) {}

    }

}

