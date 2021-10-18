using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
        private readonly LinkGenerator linkGenerator;
        private readonly IActionDescriptorCollectionProvider actionDescriptorsProvider;

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(
            IUserRepository userRepository,
            IMapper mapper,
            LinkGenerator linkGenerator,
            IActionDescriptorCollectionProvider actionDescriptorsProvider)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
            this.actionDescriptorsProvider = actionDescriptorsProvider;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return NotFound();
            }

            var foundUser = userRepository.FindById(userId);
            if (foundUser is null)
            {
                return NotFound();
            }

            return Ok(mapper.Map<UserEntity, UserDto>(foundUser));
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] PostUserDto user)
        {
            if (user is null)
            {
                return BadRequest();
            }
            if (user.Login != null && !user.Login.All(char.IsLetterOrDigit)) // пусть тут будет не через реги
            {
                ModelState.AddModelError("Login", "Login should be alphanumeric!");
            }
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var insertedUser = userRepository.Insert(mapper.Map<PostUserDto, UserEntity>(user));

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = insertedUser.Id },
                insertedUser.Id
            );
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto userDto)
        {
            if (userId == Guid.Empty || userDto is null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            
            var userEntity = mapper.Map(userDto, new UserEntity(userId));
            userRepository.UpdateOrInsert(userEntity, out var isInserted);
            
            return isInserted
                ? CreatedAtRoute(nameof(GetUserById), new { userId }, userId)
                : NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            var user = userRepository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            var userDto = mapper.Map<UpdateUserDto>(user); // по заданию, как я понял, при обновлении
            patchDoc.ApplyTo(userDto, ModelState); // GamesPlayed и CurrGameId должны затираться
            // может, неправильно понял
            if (!TryValidateModel(userDto))
            {
                return UnprocessableEntity(ModelState);
            }

            var updUser = mapper.Map(userDto, new UserEntity(userId));
            userRepository.Update(updUser);

            return NoContent();
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteUser(Guid userId) // лучше с [FromRoute]?
        {
            if (userId == Guid.Empty || userRepository.FindById(userId) is null)
            {
                return NotFound();
            }

            userRepository.Delete(userId);
            return NoContent();
        }

        private string GetUsersPageUri(int pageNumber, int pageSize)
        {
            return linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new { pageNumber, pageSize }
            );
        }

        private const int MaxPageSize = 20;

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public OkObjectResult GetUsers(
            [FromQuery]
            [DefaultValue(1)]
            [Range(1, int.MaxValue)] // в итоге и не нужны кроме как для читаемости, получается
            int pageNumber,
            
            [FromQuery] // аналогично
            [DefaultValue(10)]
            [Range(1, MaxPageSize)]
            int pageSize
        )
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var page = userRepository.GetPage(pageNumber, pageSize);
            var users = mapper.Map<IEnumerable<UserDto>>(page);

            var paginationHeader = new
            {
                previousPageLink = pageNumber > 1 ? GetUsersPageUri(pageNumber - 1, pageSize) : null,
                nextPageLink = pageNumber < MaxPageSize ? GetUsersPageUri(pageNumber + 1, pageSize) : null,
                totalCount = page.TotalCount,
                pageSize = page.PageSize,
                currentPage = page.CurrentPage,
                totalPages = page.TotalPages
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader)); // а вдруг он строку перенесёт и хэдеры поедут...
            return Ok(users); // не должен, но кто его знает
        }

        private string[] UsersOptions { get; set; }

        [HttpOptions]
        public OkResult GetUsersOptions()
        {
            if (UsersOptions is null)
            {
                var usersRoute = ((string)Request.Path).Trim('/'); // средняя такая красота
                UsersOptions = actionDescriptorsProvider.ActionDescriptors.Items
                    .Where(
                        action => string.Equals(
                            action.AttributeRouteInfo?.Template?.Trim('/'),
                            usersRoute,
                            StringComparison.InvariantCultureIgnoreCase
                        )
                    )
                    .SelectMany(action => action.ActionConstraints)
                    .OfType<HttpMethodActionConstraint>()
                    .SelectMany(methodConstraint => methodConstraint.HttpMethods)
                    .Distinct()
                    .ToArray();
            }

            Response.Headers.Add("Allow", string.Join(", ", UsersOptions));
            return Ok();
        }
    }
}