using System;
using System.Collections.Generic;
using Terradue.Portal;
using Terradue.OpenSearch.Tumblr;

namespace Terradue.News {
    public class TumblrNews : Article {

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.TepQW.Controller.TumblrNews"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public TumblrNews(IfyContext context) : base(context) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.TepQW.Controller.TumblrNews"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="feed">Feed.</param>
        public TumblrNews(IfyContext context, TumblrFeed feed) : base(context){
            this.Identifier = feed.Identifier;
            this.Title = feed.Title;
            this.Tags = feed.Tags;
            this.Time = feed.Time;
            this.Url = feed.Url;
            this.Author = feed.Author;
            this.Content = feed.Content;
            this.Abstract = feed.Abstract;
        }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public new static TumblrNews FromId(IfyContext context, int id){
            return (TumblrNews)Article.FromId(context, id);
        }

        public static List<TumblrNews> FromFeeds(IfyContext context, List<TumblrFeed> feeds) {
            List<TumblrNews> result = new List<TumblrNews>();
            try{
                foreach (TumblrFeed feed in feeds) result.Add(new TumblrNews(context, feed));
            }catch(Exception){}
            return result;
        }

        /// <summary>
        /// Loads the tumblr feeds.
        /// </summary>
        /// <returns>The tumblr feeds.</returns>
        /// <param name="context">Context.</param>
        public static List<TumblrFeed> LoadTumblrFeeds(IfyContext context){
            List<TumblrFeed> result = new List<TumblrFeed>();
            TumblrApplication app = new TumblrApplication(context.GetConfigValue("Tumblr-apiKey"));

            EntityList<TumblrNews> tumblrs = new EntityList<TumblrNews>(context);
            tumblrs.Load();

            foreach (TumblrNews news in tumblrs) {
                TumblrFeed feed = new TumblrFeed(app, context.BaseUrl);
                feed.Identifier = news.Identifier;
                feed.Title = news.Title;
                feed.Tags = news.Tags;
                feed.Time = news.Time;
                feed.Url = news.Url;
                feed.Author = news.Author;
                feed.Content = news.Content;
                feed.Abstract = news.Abstract;
                result.Add(feed);
            }
            return result;
        }

        // <summary>
        /// Loads the tumblr feeds.
        /// </summary>
        /// <returns>The tumblr feeds.</returns>
        /// <param name="context">Context.</param>
        public static List<TumblrFeed> LoadTumblrFeeds(IfyContext context, string apiType, string apiMethod, string apiUrl){
            List<TumblrFeed> result = new List<TumblrFeed>();
            TumblrApplication app = new TumblrApplication(context.GetConfigValue("Tumblr-apiKey"), apiMethod, apiType, apiUrl);

            EntityList<TumblrNews> tumblrs = new EntityList<TumblrNews>(context);
            tumblrs.Load();

            foreach (TumblrNews news in tumblrs) {
                TumblrFeed feed = new TumblrFeed(app, context.BaseUrl);
                feed.Identifier = news.Identifier;
                feed.Title = news.Title;
                feed.Tags = news.Tags;
                feed.Time = news.Time;
                feed.Url = news.Url;
                feed.Author = news.Author;
                feed.Content = news.Content;
                feed.Abstract = news.Abstract;
                result.Add(feed);
            }
            return result;
        }
    }
}

