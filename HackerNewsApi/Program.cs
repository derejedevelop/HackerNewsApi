using HackerNewsApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IHackerNewsService, HackerNewsService>();
builder.Services.AddHttpClient<IHackerNewsService, HackerNewsService>(client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAllOrigins",
                    policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
});
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseCors(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyHeader();
    policy.AllowAnyMethod();
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }