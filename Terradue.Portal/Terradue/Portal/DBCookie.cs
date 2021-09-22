using System;
using System.Data;
using System.Web;
using Terradue.Util;

namespace Terradue.Portal {
    public class DBCookie {

        private IfyContext Context;
        public string Session { get; set; }
        public string Username { get; set; }
        public string Identifier { get; set; }
        public string Value { get; set; }
        public DateTime Expire { get; set; }
        public DateTime CreationDate { get; set; }

        /*****************************************************************************************************************/

        /// <summary>Creates a new DBCookie instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public DBCookie(IfyContext context) {
            this.Context = context;
        }

        /*****************************************************************************************************************/

        /// <summary>Creates a new DBCookie instance representing the cookie with the specified session ID and unique identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="session">The related session ID.</param>
        /// <param name="identifier">The unique identifier of the cookie.</param>
        /// <returns>The created Group object.</returns>
        public static DBCookie FromSessionAndIdentifier(IfyContext context, string session, string identifier) {
            DBCookie cookie = new DBCookie(context);
            string sql = String.Format("SELECT value, expire, creation_date, username FROM cookie WHERE session={0} AND identifier={1};", StringUtils.EscapeSql(session), StringUtils.EscapeSql(identifier));
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);

            if (reader.Read()) {
                cookie.Session = session;
                cookie.Identifier = identifier;
                cookie.Value = reader.GetString(0);
                cookie.Expire = reader.GetDateTime(1);
                cookie.CreationDate = reader.GetDateTime(2);
                cookie.Username = reader.GetValue(3) != DBNull.Value ? reader.GetString(3) : null;
            }
            context.CloseQueryResult(reader, dbConnection);

            return cookie;
        }

        /// <summary>Creates a new DBCookie instance representing the cookie with the specified username and unique identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="username">The related username.</param>
        /// <param name="identifier">The unique identifier of the cookie.</param>
        /// <returns>The created Group object.</returns>
        public static DBCookie FromUsernameAndIdentifier(IfyContext context, string username, string identifier) {
            DBCookie cookie = new DBCookie(context);
            string sql = String.Format("SELECT value, expire, creation_date, session FROM cookie WHERE username={0} AND identifier={1} ORDER BY expire DESC;", StringUtils.EscapeSql(username), StringUtils.EscapeSql(identifier));
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);

            if (reader.Read()) {
                cookie.Username = username;
                cookie.Identifier = identifier;
                cookie.Value = reader.GetString(0);
                cookie.Expire = reader.GetDateTime(1);
                cookie.CreationDate = reader.GetDateTime(2);
                cookie.Session = reader.GetValue(3) != DBNull.Value ? reader.GetString(3) : null;
            }
            context.CloseQueryResult(reader, dbConnection);

            return cookie;
        }

        /// <summary>
        /// Loads the DB cookie.
        /// </summary>
        /// <returns>The cookie.</returns>
        /// <param name="context">Context.</param>
        /// <param name="identifier">Identifier.</param>
        public static DBCookie LoadDBCookie(IfyContext context, string identifier) {
            if (HttpContext.Current.Session == null) return new DBCookie(context);
            return DBCookie.FromSessionAndIdentifier(context, HttpContext.Current.Session.SessionID, identifier);
        }

        /*****************************************************************************************************************/

        /// <summary>Stores the DB cookie.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="session">The related session ID.</param>
        /// <param name="identifier">The unique identifier of the cookie.</param>
        /// <param name="value">The cookie value.</param>
        /// <param name="expire">The expiration date.</param>
        public static void StoreDBCookie(IfyContext context, string session, string identifier, string value, string username, DateTime expire) {
            if (string.IsNullOrEmpty(session) || string.IsNullOrEmpty(identifier)) return;
            DBCookie cookie = new DBCookie(context);
            cookie.Session = session;
            cookie.Identifier = identifier;
            cookie.Value = value;
            cookie.Expire = expire;
            cookie.Username = username;
            cookie.Store();
        }

        /// <summary>Stores the DB cookie.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="identifier">The unique identifier of the cookie.</param>
        /// <param name="value">The cookie value.</param>
        /// <param name="expire">The expiration time in seconds (default = 1 day).</param>
        public static void StoreDBCookie(IfyContext context, string identifier, string value, string username, long expire = 86400) {
            if (HttpContext.Current.Session == null) return;
            DBCookie.StoreDBCookie(context, HttpContext.Current.Session.SessionID, identifier, value, username, DateTime.UtcNow.AddSeconds(expire));
        }

        /*****************************************************************************************************************/

        /// <summary>Deletes the DB cookie.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="session">The related session ID.</param>
        /// <param name="identifier">The unique identifier of the cookie.</param>
        public static void DeleteDBCookie(IfyContext context, string session, string identifier) {
            if (string.IsNullOrEmpty(session) || string.IsNullOrEmpty(identifier)) return;
            DBCookie cookie = DBCookie.FromSessionAndIdentifier(context, session, identifier);
            cookie.Delete();
        }

        /// <summary>
        /// Deletes the DB cookie.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="identifier">Identifier.</param>
        public static void DeleteDBCookie(IfyContext context, string identifier) {
            if (HttpContext.Current.Session == null) return;
            DBCookie.DeleteDBCookie(context, HttpContext.Current.Session.SessionID, identifier);
        }

        /*****************************************************************************************************************/

        /// <summary>Deletes all DB cookies from a session.</summary> 
        /// <param name="context">The execution environment context.</param>
        /// <param name="session">The related session ID.</param>
        public static void DeleteDBCookies(IfyContext context, string session) {
            if (string.IsNullOrEmpty(session)) return;
            string sql = string.Format("DELETE FROM cookie WHERE session='{0}';", session);
            context.Execute(sql);
        }

        /// <summary>Deletes all DB cookies from a username.</summary> 
        /// <param name="context">The execution environment context.</param>
        /// <param name="username">The related username.</param>
        public static void DeleteDBCookiesFromUsername(IfyContext context, string username) {
            if (string.IsNullOrEmpty(username)) return;
            string sql = string.Format("DELETE FROM cookie WHERE username='{0}';", username);
            context.Execute(sql);
        }

        /// <summary>
        /// Revokes the session cookies.
        /// </summary>
        /// <param name="context">Context.</param>
        public static void RevokeSessionCookies(IfyContext context) {
            if (HttpContext.Current.Session == null) return;
            DBCookie.DeleteDBCookies(context, HttpContext.Current.Session.SessionID);
        }

        /*****************************************************************************************************************/

        /// <summary>Stores the DB cookie.</summary>
        public void Store() {
            if (string.IsNullOrEmpty(Session)) throw new Exception("Empty Session ID");
            if (string.IsNullOrEmpty(Identifier)) throw new Exception("Empty Identifier");
            if (string.IsNullOrEmpty(Value)) throw new Exception("Empty value");

            //delete old value if exists
            Delete(false);

            string sql = string.Format("INSERT INTO cookie (session, identifier, value, expire, creation_date, username) VALUES ({0},{1},{2},{3},{4},{5});",
                    StringUtils.EscapeSql(Session),
                    StringUtils.EscapeSql(Identifier),
                    StringUtils.EscapeSql(Value),
                    Expire.Equals(DateTime.MinValue) ? StringUtils.EscapeSql(DateTime.UtcNow.AddDays(1).ToString(@"yyyy\-MM\-dd\THH\:mm\:ss")) : StringUtils.EscapeSql(Expire.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss")),
                    StringUtils.EscapeSql(DateTime.UtcNow.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss")),
                    StringUtils.EscapeSql(Username)
            );

            Context.Execute(sql);
        }

        /// <summary>/// Deletes the cookie.</summary>
        public void Delete(bool emptyValue = true) {
            if (string.IsNullOrEmpty(Session) || string.IsNullOrEmpty(Identifier)) return;

            string sql = string.Format("DELETE FROM cookie WHERE session={0} AND identifier={1};", StringUtils.EscapeSql(Session), StringUtils.EscapeSql(Identifier));
            Context.Execute(sql);

            if (emptyValue) Value = null;
        }

        /*****************************************************************************************************************/

    }
}
