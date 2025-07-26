using HackerNewsApi.Model;
using HackerNewsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HackerNewsStoryController : ControllerBase
    {
        public IHackerNewsService _hackerNewsService;

        public HackerNewsStoryController(IHackerNewsService hackerNewsService)
        {
            _hackerNewsService = hackerNewsService;
        }

        [HttpGet]
        public async Task<List<HackerNewsStory>> Get(CancellationToken cancellationToken)
        {
            return await _hackerNewsService.GetNewStoriesAsync(cancellationToken);
        }
    }
}
