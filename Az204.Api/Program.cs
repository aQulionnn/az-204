using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CosmosClient>(_ =>
{
    var options = new CosmosClientOptions
    {
        HttpClientFactory = () =>
        {
            HttpMessageHandler httpMessageHandler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (req, cert, chain, errors) => true
            };
        
            return new HttpClient(httpMessageHandler);
        }
    };
        
    return new CosmosClient(builder.Configuration.GetConnectionString("CosmosDb"), options);
});

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration.GetConnectionString("AzureBlobStorage"));
    clientBuilder.AddQueueServiceClient(builder.Configuration.GetConnectionString("AzureQueueStorage"));
    clientBuilder.AddTableServiceClient(builder.Configuration.GetConnectionString("AzureTableStorage"));
});

builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
    .AddAzureBlobStorage(serviceProvider => serviceProvider.GetRequiredService<BlobServiceClient>())
    .AddAzureQueueStorage(serviceProvider => serviceProvider.GetRequiredService<QueueServiceClient>())
    .AddAzureTable(serviceProvider => serviceProvider.GetRequiredService<TableServiceClient>())
    .AddAzureCosmosDB(serviceProvider => serviceProvider.GetRequiredService<CosmosClient>());

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