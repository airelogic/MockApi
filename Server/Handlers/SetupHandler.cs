using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace MockApi.Server.Handlers
{
    internal class SetupHandler : RequestHandler
    {
        public SetupHandler(RouteCache routeCache) : base(routeCache)
        {
        }

        public override async Task<MockApiResponse> ProcessRequest(IHttpRequestFeature request)
        {
            var path = request.Path;
            var method = request.GetMockApiMethod();
            var statusCode = request.GetMockApiStatus();
            var bodyAsText = await request.Body.ReadAsTextAsync();
            var onceOnly = request.GetMockApiFlag("Once");

            RouteCache.RegisterRouteSetup(method, path, bodyAsText, statusCode, onceOnly);

            return new MockApiResponse
            {
                StatusCode = 200,
                Payload = $"Setup path {method} {path}",
                ContentType = "text"
            };
        }
    }
}
