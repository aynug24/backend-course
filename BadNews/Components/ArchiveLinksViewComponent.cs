using System;
using BadNews.Repositories.News;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BadNews.Components
{
    public class ArchiveLinksViewComponent : ViewComponent
    {
        private readonly INewsRepository newsRepository;
        private readonly IMemoryCache cache;

        private const string CacheKey = nameof(ArchiveLinksViewComponent);

        public ArchiveLinksViewComponent(INewsRepository newsRepository, IMemoryCache cache)
        {
            this.newsRepository = newsRepository;
            this.cache = cache;
        }

        public IViewComponentResult Invoke()
        {
            if (!cache.TryGetValue(CacheKey, out var years))
            {
                years = newsRepository.GetYearsWithArticles();
                if (years != null)
                {
                    cache.Set(CacheKey, years, TimeSpan.FromSeconds(30));
                }
            }

            return View(years);
        }
    }
}