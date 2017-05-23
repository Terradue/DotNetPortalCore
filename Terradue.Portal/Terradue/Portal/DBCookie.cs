using System;
using System.Data;
using Terradue.Util;

namespace Terradue.Portal {
    public class DBCookie {

        private IfyContext Context;
        public string Session { get; set; }
        public string Identifier { get; set; }
        public string Value { get; set; }
        public DateTime Expire { get; set; }
        public DateTime CreationDate { get; set; }

        /// <summary>Creates a new DBCookie instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public DBCookie(IfyContext context) {
            this.Context = context;
        }

        /// <summary>Creates a new DBCookie instance representing the cookie with the specified session ID and unique identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="session">The related session ID.</param>
        /// <param name="identifier">The unique identifier of the cookie.</param>
        /// <returns>The created Group object.</returns>
        public static DBCookie FromSessionAndIdentifier(IfyContext context, string session, string identifier) {
            DBCookie cookie = new DBCookie(context);
            string sql = String.Format("SELECT value, expire, creation_date FROM cookie WHERE session={0} AND identifier={1};", StringUtils.EscapeSql(session), StringUtils.EscapeSql(identifier));
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            Console.WriteLine(sql);

            if (reader.Read()) {
                cookie.Session = session;
                cookie.Identifier = identifier;
                cookie.Value = reader.GetString(0);
                cookie.Expire = reader.GetDateTime(1);
                cookie.CreationDate = reader.GetDateTime(2);
            }
            context.CloseQueryResult(reader, dbConnection);

            return cookie;
        }

        /// <summary>Stores the DB cookie.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="session">The related session ID.</param>
        /// <param name="identifier">The unique identifier of the cookie.</param>
        /// <param name="value">The cookie value.</param>
        /// <param name="expire">The expiration time.</param>
        public static void StoreDBCookie(IfyContext context, string session, string identifier, string value, DateTime expire) {
            DBCookie cookie = new DBCookie(context);
            cookie.Session = session;
            cookie.Identifier = identifier;
            cookie.Value = value;
            cookie.Expire = expire;
            cookie.Store();
        }

        /// <summary>Deletes the DB cookie.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="session">The related session ID.</param>
        /// <param name="identifier">The unique identifier of the cookie.</param>
        public static void DeleteDBCookie(IfyContext context, string session, string identifier) {
            DBCookie cookie = DBCookie.FromSessionAndIdentifier(context, session, identifier);
            cookie.Delete();
        }

        /// <summary>Deletes all DB cookies from a session.</summary> 
        /// <param name="context">The execution environment context.</param>
        /// <param name="session">The related session ID.</param>
        public static void DeleteDBCookies(IfyContext context, string session) {
            string sql = string.Format("DELETE FROM cookie WHERE session='{0}';", session);
            context.Execute(sql);
        }

        /// <summary>Stores the DB cookie.</summary>
        public void Store() {
            if (string.IsNullOrEmpty(Session)) throw new Exception("Empty Session ID");
            if (string.IsNullOrEmpty(Identifier)) throw new Exception("Empty Identifier");
            if (string.IsNullOrEmpty(Value)) throw new Exception("Empty value");

            //delete old value if exists
            Delete(false);

            string sql = string.Format("INSERT INTO cookie (session, identifier, value, expire, creation_date) VALUES ({0},{1},{2},{3},{4});",
                    StringUtils.EscapeSql(Session),
                    StringUtils.EscapeSql(Identifier),
                    StringUtils.EscapeSql(Value),
                    Expire.Equals(DateTime.MinValue) ? StringUtils.EscapeSql(DateTime.UtcNow.AddDays(1).ToString(@"yyyy\-MM\-dd\THH\:mm\:ss")) : StringUtils.EscapeSql(Expire.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss")),
                    StringUtils.EscapeSql(DateTime.UtcNow.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss"))
            );

            Context.Execute(sql);
            Console.WriteLine(sql);
        }

        /// <summary>/// Deletes the cookie.</summary>
        public void Delete(bool emptyValue = true) {
            if (string.IsNullOrEmpty(Session)) throw new Exception("Empty Session ID");
            if (string.IsNullOrEmpty(Identifier)) throw new Exception("Empty Identifier");

            string sql = string.Format("DELETE FROM cookie WHERE session={0} AND identifier={1};", StringUtils.EscapeSql(Session), StringUtils.EscapeSql(Identifier));
            Context.Execute(sql);
            Console.WriteLine(sql);

            if (emptyValue) Value = null;
        }
    }
}
