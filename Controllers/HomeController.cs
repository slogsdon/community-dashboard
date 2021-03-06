using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CommunityDashboard.Controllers
{
    public class HomeController : Controller
    {
        IMemoryCache _memoryCache;
        static List<FeedSource> feeds = new List<FeedSource> {
            // Wordpress
            new FeedSource {Source = "wordpress-woocommerce", Url = "https://wordpress.org/support/plugin/woocommerce-securesubmit-gateway/rss/"},
            new FeedSource {Source = "wordpress-securesubmit", Url = "https://wordpress.org/support/plugin/securesubmit/rss/"},
            new FeedSource {Source = "wordpress-events-manager-pro", Url = "https://wordpress.org/support/plugin/events-manager-pro-securesubmit-gateway/rss/"},
            new FeedSource {Source = "wordpress-gravityforms", Url = "https://wordpress.org/support/plugin/heartland-secure-submit-addon-for-gravity-forms/rss/"},
            // Magento
            new FeedSource {Source = "magento-1", Url = "https://community.magento.com/jvdeh29369/rss/search?q=\"secure+submit\"&filter=labels&search_type=thread"},
            // Twitter
            new FeedSource {Source = "twitter-secure-submit", Url = "https://queryfeed.net/tw?q=%22secure+submit%22"},
            new FeedSource {Source = "twitter-securesubmit", Url = "https://queryfeed.net/tw?q=securesubmit"},
            // Facebook
            new FeedSource {Source = "facebook-secure-submit", Url = "https://queryfeed.net/facebook?q=%22secure+submit%22"},
            new FeedSource {Source = "facebook-securesubmit", Url = "https://queryfeed.net/facebook?q=securesubmit"},
            // Google+
            new FeedSource {Source = "gplus-secure-submit", Url = "https://queryfeed.net/plus?q=%22secure+submit%22"},
            new FeedSource {Source = "gplus-securesubmit", Url = "https://queryfeed.net/plus?q=securesubmit"},
        };

        public HomeController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public async Task<IActionResult> Index()
        {
            var CACHE_KEY = "CommunityDashboard.Articles";
            List<FeedItem> articles;

            if (!_memoryCache.TryGetValue(CACHE_KEY, out articles))
            {
                articles = new List<FeedItem>();

                foreach (var feed in HomeController.feeds)
                {
                    articles.AddRange(await GetArticles(feed));
                }

                _memoryCache.Set(CACHE_KEY, articles, new MemoryCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromHours(1)
                });
            }
            return View(articles.OrderByDescending(a => a.PublishDate));
        }

        public IActionResult Error()
        {
            return View();
        }

        private async Task<List<FeedItem>> GetArticles(FeedSource feed)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(feed.Url);
                var responseMessage = await client.GetAsync(feed.Url);
                var responseString = await responseMessage.Content.ReadAsStringAsync();

                responseString = responseString
                    .Replace("&mdash;", "&#8212;");

                //extract feed items
                XDocument doc = XDocument.Parse(responseString);
                var feedItems = from item in doc.Root
                                                .Descendants()
                                                .FirstOrDefault(i => i.Name.LocalName == "channel")
                                                .Elements()
                                                .Where(i => i.Name.LocalName == "item")
                                select new FeedItem
                                {
                                    Source = feed.Source,
                                    Content = item.Elements().FirstOrDefault(i => i.Name.LocalName == "description")?.Value,
                                    Link = item.Elements().FirstOrDefault(i => i.Name.LocalName == "link")?.Value,
                                    PublishDate = ParseDate(item.Elements().FirstOrDefault(i => i.Name.LocalName == "pubDate")?.Value),
                                    Title = item.Elements().FirstOrDefault(i => i.Name.LocalName == "title")?.Value
                                };
                return feedItems.ToList();
            }
        }

        private DateTime ParseDate(string date)
        {
            DateTime result;
            if (DateTime.TryParse(date, out result))
                return result;
            else
                return DateTime.MinValue;
        }
    }

    public class FeedSource {
        public string Source {get;set;}
        public string Url {get;set;}
    }

    public class FeedItem {
        public string Source {get;set;}
        public string Content {get;set;}
        public string Link {get;set;}
        public DateTime PublishDate {get;set;}
        public string Title {get;set;}
    }
}
