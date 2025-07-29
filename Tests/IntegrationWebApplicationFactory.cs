using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Moq;
namespace Tests;

public class IntegrationWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    public Mock<HttpMessageHandler> MockHttpMessageHandler { get; } = new Mock<HttpMessageHandler>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var httpClientFactoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IHttpClientFactory));
            if (httpClientFactoryDescriptor != null)
            {
                services.Remove(httpClientFactoryDescriptor);
            }

            var httpClientDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(HttpClient));
            if (httpClientDescriptor != null)
            {
                services.Remove(httpClientDescriptor);
            }

            var memoryCacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMemoryCache));
            if (memoryCacheDescriptor != null)
            {
                services.Remove(memoryCacheDescriptor);
            }

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory
                .Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(MockHttpMessageHandler.Object));

            services.AddSingleton<IHttpClientFactory>(mockHttpClientFactory.Object);
            services.AddSingleton<HttpClient>(sp => new HttpClient(MockHttpMessageHandler.Object));

            services.AddMemoryCache();
            services.AddSingleton<IMemoryCache, MemoryCache>(); 
        });
    }
}

