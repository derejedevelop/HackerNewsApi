using HackerNewsApi.Model;
using HackerNewsApi.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Net;
using System.Text.Json;
using Moq.Protected;

namespace Tests;

public class HackerNewsServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _mockHttpClient;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly HackerNewsService _hackerNewsService;

    private List<HackerNewsStory> capturedCacheSetStories;

    public HackerNewsServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockHttpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>()))
             .Returns((object key) =>
             {
                 var mockCacheEntry = new Mock<ICacheEntry>();
              
                 mockCacheEntry.SetupSet(e => e.Value = It.IsAny<object>())
                     .Callback<object>(val =>
                     {
                         capturedCacheSetStories = (List<HackerNewsStory>)val;
                     });

                 mockCacheEntry.SetupSet(e => e.AbsoluteExpiration = It.IsAny<DateTimeOffset?>());
                 mockCacheEntry.SetupSet(e => e.AbsoluteExpirationRelativeToNow = It.IsAny<TimeSpan?>());
                 mockCacheEntry.SetupSet(e => e.SlidingExpiration = It.IsAny<TimeSpan?>());
                 mockCacheEntry.Setup(e => e.Dispose());

                 return mockCacheEntry.Object;
             });
        
        _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>()))
              .Returns((object key) =>
              {
                  var mockCacheEntry = new Mock<ICacheEntry>();
                  mockCacheEntry.SetupSet(e => e.Value = It.IsAny<object>()).Callback<object>(val => { });
                  return mockCacheEntry.Object;
              });
        _hackerNewsService = new HackerNewsService(_mockHttpClient, _mockMemoryCache.Object);
    }

    [Fact]
    public async Task GetNewStoriesAsync_FetchesNewStoriesAndCachesThem_WhenCacheIsEmpty()
    {
        // Arrange
        var currentStoryIds = new List<int> { 1, 2, 3 };
        var story1 = new HackerNewsStory { id = 1, title = "Story 1", url = "http://url1.com" };
        var story2 = new HackerNewsStory { id = 2, title = "Story 2", url = "http://url2.com" };
        var story3 = new HackerNewsStory { id = 3, title = "Story 3", url = "http://url3.com" };

        // Mock HTTP calls
        SetupHttpMock("newstories.json", currentStoryIds);
        SetupHttpMock("item/1.json", story1);
        SetupHttpMock("item/2.json", story2);
        SetupHttpMock("item/3.json", story3);

        object dummyCacheValue= null;
        _mockMemoryCache.Setup(m => m.TryGetValue(It.IsAny<object>(), out dummyCacheValue))
                        .Returns(false);

        List<HackerNewsStory> capturedCacheSetStories = null;

        var mockEntry = new Mock<ICacheEntry>();
        mockEntry.SetupSet(m => m.Value = It.IsAny<object>())
                 .Callback<object>(val =>
                 {
                     capturedCacheSetStories = val as List<HackerNewsStory>;
                 });

        mockEntry.SetupSet(e => e.AbsoluteExpiration = It.IsAny<DateTimeOffset?>());
        mockEntry.SetupSet(e => e.AbsoluteExpirationRelativeToNow = It.IsAny<TimeSpan?>());
        mockEntry.SetupSet(e => e.SlidingExpiration = It.IsAny<TimeSpan?>());
        mockEntry.Setup(e => e.Dispose());

        _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>()))
                           .Returns(mockEntry.Object);

        // Act
        var result = await _hackerNewsService.GetNewStoriesAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, s => s.id == story2.id);

        Assert.NotNull(capturedCacheSetStories); 
        Assert.Equal(3, capturedCacheSetStories.Count);
        Assert.Contains(capturedCacheSetStories, s => s.id == story2.id);
    }

    [Fact]
    public async Task GetNewStoriesAsync_UpdatesCacheWithNewStories_WhenCacheHasOldStories()
    {
        // Arrange
        var existingStory = new HackerNewsStory { id = 10, title = "existing Story", url = "http://existing.com" };
        var newStory1 = new HackerNewsStory { id = 1, title = "New Story 1", url = "http://new1.com" };
        var newStory2 = new HackerNewsStory { id = 2, title = "New Story 2", url = "http://new2.com" };

        var currentStoryIds = new List<int> { 10, 1, 2 };

        SetupHttpMock("newstories.json", currentStoryIds);
        SetupHttpMock("item/1.json", newStory1);
        SetupHttpMock("item/2.json", newStory2);


        var cachedStoriesToReturn = new List<HackerNewsStory> { existingStory };
        object cacheOutValue = cachedStoriesToReturn;

        _mockMemoryCache.Setup(m => m.TryGetValue(
            It.Is<string>(key => key == "AllCachedStories"),
            out cacheOutValue))
            .Returns(true);

        List<HackerNewsStory> capturedCacheSetStories = null;

        var mockEntry = new Mock<ICacheEntry>();

        mockEntry.SetupSet(e => e.Value = It.IsAny<object>())
                 .Callback<object>(val =>
                 {
                     capturedCacheSetStories = val as List<HackerNewsStory>;
                 });

        mockEntry.SetupSet(e => e.AbsoluteExpiration = It.IsAny<DateTimeOffset?>());
        mockEntry.SetupSet(e => e.AbsoluteExpirationRelativeToNow = It.IsAny<TimeSpan?>());
        mockEntry.SetupSet(e => e.SlidingExpiration = It.IsAny<TimeSpan?>());
        mockEntry.Setup(e => e.Dispose());

        _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>()))
                        .Returns(mockEntry.Object);
       
        // Act
        var result = await _hackerNewsService.GetNewStoriesAsync(CancellationToken.None); // Changed _sut to _hackerNewsService

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, s => s.id == existingStory.id);
        Assert.Contains(result, s => s.id == newStory1.id);

        Assert.NotNull(capturedCacheSetStories);
        Assert.Equal(3, capturedCacheSetStories.Count);
        Assert.Contains(capturedCacheSetStories, s => s.id == existingStory.id);
        Assert.Contains(capturedCacheSetStories, s => s.id == newStory1.id);
    }

    [Fact]
    public async Task GetStoryDetailAsync_ReturnsStory_OnSuccess()
    {
        // Arrange
        var storyId = 123;
        var expectedStory = new HackerNewsStory { id = storyId, title = "Test Story", url = "http://test.com" };
        var jsonResponse = JsonSerializer.Serialize(expectedStory);

        _mockHttpMessageHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>( 
                        "SendAsync", 
                        ItExpr.Is<HttpRequestMessage>(req =>
                            req.Method == HttpMethod.Get &&
                            req.RequestUri.ToString() == $"https://hacker-news.firebaseio.com/v0/item/{storyId}.json"),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(jsonResponse)
                    });

        // Act
        var result = await _hackerNewsService.GetStoryDetailAsync(storyId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedStory.id, result.id);
        Assert.Equal(expectedStory.title, result.title);
    }

    [Fact]
    public async Task GetStoryDetailAsync_ReturnsNull_OnFailure()
    {
        // Arrange
        var storyId = 123;

        // Direct HttpClient setup for failure
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString() == $"https://hacker-news.firebaseio.com/v0/item/{storyId}.json"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _hackerNewsService.GetStoryDetailAsync(storyId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentStoryIds_ReturnsListOfIds_OnSuccess()
    {
        // Arrange
        var expectedIds = new List<int> { 1, 2, 3 };
        var jsonResponse = JsonSerializer.Serialize(expectedIds);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString() == "https://hacker-news.firebaseio.com/v0/newstories.json"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _hackerNewsService.GetCurrentStoryIds(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedIds.Count, result.Count);
        Assert.True(result.SequenceEqual(expectedIds));
    }

    private void SetupHttpMock<T>(string endpoint, T result)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().EndsWith(endpoint)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(result))
            });
    }
}