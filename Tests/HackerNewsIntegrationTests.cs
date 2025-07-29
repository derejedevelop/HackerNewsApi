using HackerNewsApi.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;

namespace Tests;

public class HackerNewsIntegrationTests : IClassFixture<IntegrationWebApplicationFactory<Program>>
{
    private readonly IntegrationWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public HackerNewsIntegrationTests(IntegrationWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _httpClient = _factory.CreateClient();

        var memoryCache = _factory.Services.GetRequiredService<IMemoryCache>() as MemoryCache;
        memoryCache?.Compact(1.0);
    }

    [Fact]
    public async Task Get_ReturnsStoriesWithUrls_WhenApiEndpointCalled()
    {
        var mockStoryIds = new List<int> { 1, 2, 3 };
        var mockStoryIdsResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(mockStoryIds)
        };

        var mockStory1 = new HackerNewsStory { id = 1, title = "Test Story 1", url = "http://test1.com"};
        var mockStory1Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(mockStory1)
        };

        var mockStory2 = new HackerNewsStory { id = 2, title = "Test Story 2", url = "http://test2.com"};
        var mockStory2Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(mockStory2)
        };

        var mockStory3 = new HackerNewsStory { id = 3, title = "Test Story 3", url = null };
        var mockStory3Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(mockStory3)
        };

    
        _factory.MockHttpMessageHandler
            .Protected() 
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns(Task.FromResult(mockStoryIdsResponse)) 
            .Returns(Task.FromResult(mockStory1Response))   
            .Returns(Task.FromResult(mockStory2Response))
            .Returns(Task.FromResult(mockStory3Response));

        var response = await _httpClient.GetAsync("http://localhost:5279/HackerNewsStory");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stories = await response.Content.ReadFromJsonAsync<List<HackerNewsStory>>();

        Assert.NotNull(stories);
        Assert.Equal(2, stories.Count);
        Assert.Contains(stories, s => s.id == 1 && s.title == "Test Story 1" && s.url == "http://test1.com");
        Assert.Contains(stories, s => s.id == 2 && s.title == "Test Story 2" && s.url == "http://test2.com");
        Assert.DoesNotContain(stories, s => s.id == 3); 
    }
}
