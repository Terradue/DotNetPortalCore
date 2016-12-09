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

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Terradue.Portal.DBCookie"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public DBCookie (IfyContext context) {
            Context = context;
        }

        /// <summary>
        /// Froms the session and identifier.
        /// </summary>
        /// <returns>The session and identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="session">Session.</param>
        /// <param name="identifier">Identifier.</param>
        public static DBCookie FromSessionAndIdentifier(IfyContext context, string session, string identifier){
            DBCookie cookie = new DBCookie (context);
            string sql = String.Format ("SELECT value, expire, creation_date FROM cookie WHERE session={0} AND identifier={1};", StringUtils.EscapeSql (session), StringUtils.EscapeSql (identifier));
            IDbConnection dbConnection = context.GetDbConnection ();
            IDataReader reader = context.GetQueryResult (sql, dbConnection);
            Console.WriteLine (sql);

            if (reader.Read ()) {
                cookie.Session = session;
                cookie.Identifier = identifier;
                cookie.Value = reader.GetString (0);
                cookie.Expire = reader.GetDateTime (1);
                cookie.CreationDate = reader.GetDateTime (2);
            }
            context.CloseQueryResult (reader, dbConnection);

            return cookie;
        }

        /// <summary>
        /// Stores the db cookie.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="session">Session.</param>
        /// <param name="identifier">Identifier.</param>
        /// <param name="value">Value.</param>
        /// <param name="expire">Expire.</param>
        public static void StoreDBCookie (IfyContext context, string session, string identifier, string value, DateTime expire) { 
            DBCookie cookie = new DBCookie (context);
            cookie.Session = session;
            cookie.Identifier = identifier;
            cookie.Value = value;
            cookie.Expire = expire;
            cookie.Store ();
        }

        /// <summary>
        /// Deletes the DB Cookie.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="session">Session.</param>
        /// <param name="identifier">Identifier.</param>
        public static void DeleteDBCookie (IfyContext context, string session, string identifier) { 
            DBCookie cookie = DBCookie.FromSessionAndIdentifier (context, session, identifier);
            cookie.Delete ();
        }

        /// <summary>
        /// Deletes the DB Cookies from the session.
        /// </summary> 
        /// <param name="context">Context.</param>
        /// <param name="session">Session.</param>
        public static void DeleteDBCookies (IfyContext context, string session)
        {
            string sql = string.Format("DELETE FROM cookie WHERE session='{0}';",session);
            context.Execute (sql);
        }

        /// <summary>
        /// Store the cookie.
        /// </summary>
        public void Store(){
            if (string.IsNullOrEmpty (Session)) throw new Exception ("Empty Session ID");
            if (string.IsNullOrEmpty (Identifier)) throw new Exception ("Empty Identifier");
            if (string.IsNullOrEmpty (Value)) throw new Exception ("Empty value");

            //delete old value if exists
            Delete (false);

            string sql = string.Format ("INSERT INTO cookie (session, identifier, value, expire, creation_date) VALUES ({0},{1},{2},{3},{4});",
                                        StringUtils.EscapeSql (Session),
                                        StringUtils.EscapeSql (Identifier),
                                        StringUtils.EscapeSql (Value),
                                        Expire.Equals (DateTime.MinValue) ? StringUtils.EscapeSql (DateTime.UtcNow.AddDays(1).ToString (@"yyyy\-MM\-dd\THH\:mm\:ss")) : StringUtils.EscapeSql (Expire.ToString (@"yyyy\-MM\-dd\THH\:mm\:ss")),
                                        StringUtils.EscapeSql (DateTime.UtcNow.ToString (@"yyyy\-MM\-dd\THH\:mm\:ss")));

            Context.Execute (sql);
            Console.WriteLine (sql);
        }

        /// <summary>
        /// Delete the cookie.
        /// </summary>
        public void Delete(bool emptyValue = true){
            if (string.IsNullOrEmpty (Session)) throw new Exception ("Empty Session ID");
            if (string.IsNullOrEmpty (Identifier)) throw new Exception ("Empty Identifier");

            string sql = string.Format ("DELETE FROM cookie WHERE session={0} AND identifier={1};", StringUtils.EscapeSql (Session), StringUtils.EscapeSql (Identifier));
            Context.Execute (sql);
            Console.WriteLine (sql);

            if(emptyValue) Value = null;
        }
    }
}
