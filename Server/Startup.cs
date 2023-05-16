using System;
using System.IO;
using Amazon.S3;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MockApi.Server.Handlers;
using static System.Net.Mime.MediaTypeNames;

namespace MockApi.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddScoped<BulkSetupHandler>()
                .AddScoped<HandlerFactory>()
                .AddScoped<SetupHandler>()
                .AddScoped<ValidationHandler>()
                .AddScoped<WebRequestHandler>();

            var dataSource = Configuration.GetValue<string>("DataSource");
            if (string.IsNullOrEmpty(dataSource) == false)
            {
                var dataSourceParts = dataSource.Split(":");
                services.Configure<FileReaderOptions>(opt => opt.Root = dataSourceParts[1]);
                switch (dataSourceParts[0])
                {
                    case "local":
                        services.AddSingleton<IFileReader, FileSystemFileReader>();
                        break;
                    case "s3":
                        services.AddAWSService<IAmazonS3>();
                        services.AddSingleton<IFileReader, S3FileReader>();
                        break;
                    case "azure":
                        services.AddAzureClients(x =>
                        {
                            x.AddBlobServiceClient(new Uri($"https://{dataSourceParts[2]}.blob.core.windows.net"));
                            x.UseCredential(new DefaultAzureCredential());
                        });
                        services.AddSingleton<IFileReader, AzureBlobFileReader>();
                        break;
                    default:
                        throw new NotSupportedException($"Data source {dataSourceParts[0]} is not supported");
                }
            }

            var routesFile = Configuration.GetValue<string>("RoutesFile");
            services
                .Configure<RouteCache.Options>(opt => opt.RoutesFile = routesFile)
                .AddSingleton<RouteCache>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Run(async (context) =>
            {
                try
                {
                    var routeCache = context.RequestServices.GetRequiredService<RouteCache>();
                    await routeCache.Initialise();

                    var handlerFactory = context.RequestServices.GetRequiredService<HandlerFactory>();
                    var requestInfo = context.Features.Get<IHttpRequestFeature>();
                    var handler = handlerFactory.GetHandler(requestInfo.GetMockApiAction());
                    var response = await handler.ProcessRequest(requestInfo);
                    context.Response.StatusCode = response.StatusCode;
                    context.Response.Headers.Add("content-type", response.ContentType);

                    foreach (var header in response.Headers)
                        context.Response.Headers.Add(header.Key, header.Value);

                    await context.Response.WriteAsync(response.Payload);
                }
                catch(Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = Text.Plain;
                    await context.Response.WriteAsync(ex.Message);
                    await context.Response.WriteAsync(ex.StackTrace);
                }
            });            
        }
    }
}
