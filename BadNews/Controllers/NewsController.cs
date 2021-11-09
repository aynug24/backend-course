using System;
using BadNews.ModelBuilders.News;
using Microsoft.AspNetCore.Mvc;

namespace BadNews.Controllers
{
    public class NewsController : Controller
    {
        private readonly INewsModelBuilder newsModelBuilder;

        public NewsController(INewsModelBuilder newsModelBuilder)
        {
            this.newsModelBuilder = newsModelBuilder;
        }

        public IActionResult Index([FromRoute] int pageIndex = 0)
        {
            var newsModel = newsModelBuilder.BuildIndexModel(pageIndex, false, null);
            return View(newsModel);
        }

        public IActionResult FullArticle([FromRoute] Guid id)
        {
            var articleModel = newsModelBuilder.BuildFullArticleModel(id);
            if (articleModel == null)
            {
                return NotFound();
            }

            return View(articleModel);
        }
    }
}