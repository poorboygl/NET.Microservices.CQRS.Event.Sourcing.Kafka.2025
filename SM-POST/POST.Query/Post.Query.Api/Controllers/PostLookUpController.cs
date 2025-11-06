using CQRS.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Post.Common.DTOs;
using Post.Query.Api.DTOs;
using Post.Query.Api.Queries;
using Post.Query.Domain.Entities;

namespace Post.Query.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostLookUpController(ILogger<PostLookUpController> logger, IQueryDispatcher<PostEntity> queryDispatcher) : ControllerBase
{
    public async Task<ActionResult> GetAllPostsAsync()
    {
        try
        {
            var posts = await queryDispatcher.SendAsync(new FindAllPostQuery());

            if (posts == null || posts.Count == 0) return NoContent();

            var count = posts.Count;

            return Ok(new PostLookupResponse
            {
                Posts = posts,
                Message = $"Successfully returned {count} post{(count > 1 ? "s" : string.Empty)}!"
            });
        }
        catch (Exception ex)
        {

            const string SAFE_ERROR_MESSAGE = "Error while processing request to retrieve all posts!";
            logger.LogError(ex, SAFE_ERROR_MESSAGE);

            return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Message = SAFE_ERROR_MESSAGE
            });
        }
    }

    [HttpGet("byId/{postId}")]
    public async Task<ActionResult> GetByPostAsync(Guid postId)
    {
        try
        {
            var posts = await queryDispatcher.SendAsync(new FindPostByIdQuery { Id = postId });

            if (posts == null || posts.Count == 0) return NoContent();

            var count = posts.Count;

            return Ok(new PostLookupResponse
            {
                Posts = posts,
                Message = $"Successfully returned post!"
            });   
        }
        catch (Exception ex)
        {

            const string SAFE_ERROR_MESSAGE = "Error while processing request to post by Id!";
            logger.LogError(ex, SAFE_ERROR_MESSAGE);

            return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Message = SAFE_ERROR_MESSAGE
            });
        }
    }
}
