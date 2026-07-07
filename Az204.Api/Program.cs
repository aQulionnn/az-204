using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

builder.Services.AddControllers();
builder.Services.AddOpenApi(options => options.AddScalarTransformers());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar", options =>
    {
        options
            .WithTitle("API")
            .WithTheme(ScalarTheme.DeepSpace)
            .SortTagsAlphabetically()
            .SortOperationsByMethod()
            .PreserveSchemaPropertyOrder()
            .ShowOperationId()
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

        options.DefaultOpenAllTags = false;
    });
}

app.MapHealthChecks("health", new HealthCheckOptions()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();