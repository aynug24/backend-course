using System;
using BadNews.Models.Editor;
using BadNews.Repositories.News;
using Microsoft.AspNetCore.Mvc;

public class EditorController : Controller
{
    private readonly INewsRepository newsRepository;

    public EditorController(INewsRepository newsRepository)
    {
        this.newsRepository = newsRepository;
    }

    public IActionResult Index()
    {
        return View(new IndexViewModel());
    }

    //[HttpPost] todo
    public IActionResult CreateArticle([FromForm] IndexViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Index", model);

        var id = newsRepository.CreateArticle(new NewsArticle
        {
            Date = DateTime.Now,
            ContentHtml = model.ContentHtml,
            Header = model.Header,
            Teaser = model.Teaser
        });

        return RedirectToAction("FullArticle", "News", new { id });
    }
}