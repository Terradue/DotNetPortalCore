using System;
using Terradue.Portal;
using Terradue.OpenSearch.Twitter;
using System.Collections.Generic;


namespace Terradue.News {
    public class TwitterNews : Article {

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.TepQW.Controller.TwitterNews"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public TwitterNews(IfyContext context) : base(context){}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.OpenSearch.Twitter.TwitterNews"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="feed">Feed.</param>
        public TwitterNews(IfyContext context, TwitterFeed feed) : base(context){
            this.Identifier = feed.Identifier;
            this.Title = feed.Title;
            this.Tags = (feed.Tags != null ? string.Join(",", feed.Tags) : null);
            this.Time = feed.Time;
            this.Url = feed.Url;
            this.Author = feed.Author;
            this.AuthorImage = feed.AuthorImageUrl;
            this.Content = feed.Content;
        }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public new static TwitterNews FromId(IfyContext context, int id){
            return (TwitterNews)Article.FromId(context, id);
        }

        /// <summary>
        /// Transfor a list of TwitterFeed to a list of TwitterNews
        /// </summary>
        /// <returns>The feeds.</returns>
        /// <param name="context">Context.</param>
        /// <param name="feeds">Feeds.</param>
        public static List<TwitterNews> FromFeeds(IfyContext context, List<TwitterFeed> feeds) {
            List<TwitterNews> result = new List<TwitterNews>();
            if(feeds != null)
                foreach (TwitterFeed feed in feeds) result.Add(new TwitterNews(context, feed));
            return result;
        }

        /// <summary>
        /// Loads the twitter feeds.
        /// </summary>
        /// <returns>The twitter feeds.</returns>
        /// <param name="context">Context.</param>
        public static List<TwitterFeed> LoadTwitterFeeds(IfyContext context){
            List<TwitterFeed> result = new List<TwitterFeed>();
            TwitterApplication app = new TwitterApplication(context.GetConfigValue("Twitter-consumerKey"),
                                                            context.GetConfigValue("Twitter-consumerSecret"),
                                                            context.GetConfigValue("Twitter-token"),
                                                            context.GetConfigValue("Twitter-tokenSecret"));

            EntityList<TwitterNews> twitters = new EntityList<TwitterNews>(context);
            twitters.Load();

            foreach (TwitterNews news in twitters) {
                TwitterFeed feed = new TwitterFeed(app, context.BaseUrl);
                feed.Identifier = news.Identifier;
                feed.Title = news.Title;
                feed.Tags = (news.Tags != null ? new List<string>(news.Tags.Split(",".ToCharArray(),StringSplitOptions.RemoveEmptyEntries)) : null);
                feed.Time = news.Time;
                feed.Url = news.Url;
                feed.Author = news.Author;
                feed.AuthorImageUrl = news.AuthorImage;
                feed.Content = news.Content;
                result.Add(feed);
            }
            return result;
        }
            
        public static TwitterCollection LoadTwitterCollection(IfyContext context) {
            List<TwitterFeed> result = new List<TwitterFeed>();
            TwitterApplication app = new TwitterApplication(context.GetConfigValue("Twitter-consumerKey"),
                                                            context.GetConfigValue("Twitter-consumerSecret"),
                                                            context.GetConfigValue("Twitter-token"),
                                                            context.GetConfigValue("Twitter-tokenSecret"));

            var collection = new TwitterCollection(app, context.BaseUrl);
            collection.Identifier = "tweet";
            collection.Accounts = new List<TwitterAccount>();

            EntityList<TwitterNews> twitters = new EntityList<TwitterNews>(context);
            twitters.Load();

            foreach (TwitterNews news in twitters) {
                var tags = news.Tags != null ? new List<string>(news.Tags.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) : null;
                collection.Accounts.Add(new TwitterAccount { Title = news.Title, Author = news.Author, Tags = tags });
            }
            return collection;
        }

    }
}

