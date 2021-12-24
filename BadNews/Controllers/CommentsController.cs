using System;
using System.Linq;
using BadNews.Models.Comments;
using BadNews.Repositories.Comments;
using Microsoft.AspNetCore.Mvc;

namespace BadNews.Controllers
{
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly CommentsRepository commentsRepository;

        public CommentsController(CommentsRepository commentsRepository)
        {
            this.commentsRepository = commentsRepository;
        }

        // GET
        [HttpGet("api/news/{id}/comments")]
        public ActionResult<CommentsDto> GetCommentsForNews(Guid newsId)
        {
            if (newsId == Guid.Empty)
            {
                return BadRequest();
            }

            var comments = commentsRepository.GetComments(newsId);
            if (comments == null)
            {
                return NotFound();
            }

            // todo маппером
            var commentDtos = comments
                .Select(comment => new CommentDto
                { 
                    User = comment.User,
                    Value = comment.Value
                })
                .ToList();

            return Ok(new CommentsDto { Comments = commentDtos, NewsId = newsId });
        }
    }
}