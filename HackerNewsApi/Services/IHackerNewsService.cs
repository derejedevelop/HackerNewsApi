using HackerNewsApi.Model;

namespace HackerNewsApi.Services;

public interface IHackerNewsService
{
    Task<List<HackerNewsStory>> GetNewStoriesAsync(CancellationToken cancellationToken);
    Task<HackerNewsStory> GetStoryDetailAsync(int id, CancellationToken cancellationToken);
}
