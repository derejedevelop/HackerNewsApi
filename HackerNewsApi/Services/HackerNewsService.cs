using HackerNewsApi.Model;

namespace HackerNewsApi.Services;

public class HackerNewsService : IHackerNewsService
{
    private readonly HttpClient _httpClient;
    public HackerNewsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<List<HackerNewsStory>> GetNewStoriesAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"https://hacker-news.firebaseio.com/v0/newstories.json");
        response.EnsureSuccessStatusCode();

        var storyIds = await response.Content.ReadFromJsonAsync<List<int>>(cancellationToken);

        var fetchTasks = storyIds.Select(storyId => GetStoryDetailAsync(storyId, cancellationToken)).ToList();
        var fetchStoriesArray = await Task.WhenAll(fetchTasks);
        var newsStories = fetchStoriesArray.Where(story => story.url != null).ToList();
        return newsStories;
    }

    public async Task<HackerNewsStory> GetStoryDetailAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await _httpClient.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{id}.json");
        return await response.Content.ReadFromJsonAsync<HackerNewsStory>(cancellationToken);
    }
}
