using HackerNewsApi.Model;
using Microsoft.Extensions.Caching.Memory;

namespace HackerNewsApi.Services;

public class HackerNewsService : IHackerNewsService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    private const string AllCachedStoriesCacheKey = "AllCachedStories";
    private static readonly TimeSpan AllStoriesListCacheDuration = TimeSpan.FromHours(5);
    public HackerNewsService(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
    }
    public async Task<List<HackerNewsStory>> GetNewStoriesAsync(CancellationToken cancellationToken)
    {
        var currentStoryIds = await GetCurrentStoryIds(cancellationToken);

        _cache.TryGetValue(AllCachedStoriesCacheKey, out List<HackerNewsStory> cachedStories);
        cachedStories ??= [];

        var cachedStoryIds = new HashSet<int>(cachedStories.Select(s => s.id));
        var brandNewStoryIds = currentStoryIds.Where(id => !cachedStoryIds.Contains(id)).ToList();


        var fetchTasks = brandNewStoryIds.Select(storyId => GetStoryDetailAsync(storyId, cancellationToken)).ToList();

        var newlyFetchedStories = (await Task.WhenAll(fetchTasks));

        var combinedStories = new List<HackerNewsStory>();
        combinedStories.AddRange(cachedStories.Where(story => currentStoryIds.Contains(story.id)));
        combinedStories.AddRange(newlyFetchedStories);

        _cache.Set(AllCachedStoriesCacheKey, combinedStories, AllStoriesListCacheDuration);

        return combinedStories.Where(story => story.url != null).ToList();
    }

    public async Task<HackerNewsStory?> GetStoryDetailAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await _httpClient.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{id}.json");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<HackerNewsStory>(cancellationToken); ;
    }

    public async Task<List<int>> GetCurrentStoryIds(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"https://hacker-news.firebaseio.com/v0/newstories.json");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<int>>(cancellationToken);
    }
}
