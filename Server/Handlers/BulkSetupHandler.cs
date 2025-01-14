using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace MockApi.Server.Handlers
{
    internal class BulkSetupHandler : RequestHandler
    {
        public BulkSetupHandler(RouteCache routeCache) : base(routeCache)
        {
        }

        public override async Task<MockApiResponse> ProcessRequest(IHttpRequestFeature request)
        {
            var routesDocument = await request.Body.ReadAsTextAsync();
            RouteCache.LoadRoutes(routesDocument);

            return new MockApiResponse
            {
                StatusCode = 200,
                Payload = $"Bulk setup complete",
                ContentType = "text"
            };
        }
    }
}
