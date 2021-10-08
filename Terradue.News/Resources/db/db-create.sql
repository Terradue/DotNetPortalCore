-- VERSION 0.1

USE $MAIN$;

/*****************************************************************************/

-- Adding extended entity types for rss news... \
CALL add_type($ID$, 'Terradue.News.RssNews, Terradue.News', 'Terradue.Portal.Article, Terradue.Portal', 'RSS feed', 'RSS feeds', 'rss');
-- RESULT

-- Adding extended entity types for tumblr news... \
CALL add_type($ID$, 'Terradue.News.TumblrNews, Terradue.News', 'Terradue.Portal.Article, Terradue.Portal', 'Tumblr feed', 'Tumblr feeds', 'blog');
-- RESULT

-- Adding extended entity types for twitter news ... \
CALL add_type($ID$, 'Terradue.News.TwitterNews, Terradue.News', 'Terradue.Portal.Article, Terradue.Portal', 'Tweet', 'Tweets', 'tweet');
-- RESULT

-- Adding keys, token and pwd
INSERT INTO config (`name`, `type`, `caption`, `hint`, `optional`) VALUES ('Tumblr-apiKey', 'string', 'Tumblr Application Api Key', 'Enter the value of the Tumblr api key', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `optional`) VALUES ('Twitter-consumerKey', 'string', 'Twitter Application Consumer Key', 'Enter the value of the Twitter consumer key', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `optional`) VALUES ('Twitter-consumerSecret', 'string', 'Twitter Application Consumer Secret', 'Enter the value of the Twitter consumer secret', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `optional`) VALUES ('Twitter-token', 'string', 'Twitter Application Token', 'Enter the value of the Twitter token', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `optional`) VALUES ('Twitter-tokenSecret', 'string', 'Twitter Application Token Secret', 'Enter the value of the Twitter token secret', '0');
-- RESULT
