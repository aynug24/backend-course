using System;
using BadNews.ModelBuilders.News;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace BadNews.Controllers
{
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "Cookie")]
    public class NewsController : Controller
    {
        private static string cacheKeyPrefix = $"{nameof(NewsController)}/{nameof(FullArticle)}/";

        private readonly INewsModelBuilder newsModelBuilder;
        private readonly IMemoryCache cache;

        public NewsController(INewsModelBuilder newsModelBuilder, IMemoryCache cache)
        {
            this.newsModelBuilder = newsModelBuilder;
            this.cache = cache;
        }

        public IActionResult Index([FromQuery] int? year, [FromQuery] int pageIndex = 0)
        {
            var newsModel = newsModelBuilder.BuildIndexModel(pageIndex, true, year);
            return View(newsModel);
        }

        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult FullArticle([FromRoute] Guid id)
        {
            var cacheKey = cacheKeyPrefix + id;
            if (!cache.TryGetValue(cacheKey, out var articleModel))
            {
                articleModel = newsModelBuilder.BuildFullArticleModel(id);
                if (articleModel != null)
                {
                    cache.Set(cacheKey, articleModel, new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromSeconds(30)
                    });
                }
            }

            if (articleModel == null)
            {
                return NotFound();
            }
            return View(articleModel);
        }
    }
}