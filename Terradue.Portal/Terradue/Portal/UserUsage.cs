using System;
using System.Collections.Generic;

namespace Terradue.Portal {
    public class UserUsage {

        private IfyContext context;

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public int UserId { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Portal.UserUsage"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="usrId">Usr identifier.</param>
        public UserUsage(IfyContext context, int usrId){
            this.context = context;
            this.UserId = usrId;
        }

        /// <summary>
        /// Gets the score.
        /// </summary>
        /// <returns>The score.</returns>
        /// <param name="types">Types.</param>
        public int GetScore(List<Type> types = null){
            
            if (this.UserId == 0) return 0;

            List<Activity> activities = Activity.ForUser(context, this.UserId);
            if (activities == null) return 0;

            int score = 0;
            foreach (Activity activity in activities) {
                if (types == null) {
                    score += GetScore(activity, this.UserId);
                } else {
                    foreach (Type type in types) {
                        EntityType entityType = EntityType.GetEntityType(type);
                        if (entityType != null && activity.EntityTypeId == entityType.Id) {
                            score += GetScore(activity, this.UserId);
                            break;
                        }
                    }
                }
            }
            return score;
        }

        private int GetScore(Activity activity, int userId){
            string sql = "";
            if (activity.OwnerId == userId)
                return context.GetQueryIntegerValue(string.Format("SELECT score_owner FROM priv_score WHERE id_priv={0};", activity.PrivilegeId));
            if (activity.UserId == userId)
                return context.GetQueryIntegerValue(string.Format("SELECT score_usr FROM priv_score WHERE id_priv={0};", activity.PrivilegeId));
            return 0;
        }



    }
}

