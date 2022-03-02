using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace MockApi.Server.Handlers
{
    internal class ValidationHandler : RequestHandler
    {
        public ValidationHandler(RouteCache routeCache) : base(routeCache)
        {
        }

        public override Task<MockApiResponse> ProcessRequest(IHttpRequestFeature request)
        {
            var requestMethod = request.GetMockApiMethod();
            var requestPath = request.Path;
            var routeSetups = RouteCache.GetRouteSetups(requestMethod, requestPath);

            if (routeSetups.Any())
            {
                var responseObject = new
                {
                    count = routeSetups.Sum(r => r.Requests.Count()),
                    requests = routeSetups.SelectMany(r => r.Requests.Select(rq => new
                    {
                        path = rq.Path,
                        body = rq.Body,
                        headers = rq.Headers,
                        method = rq.Method
                    }))
                };

                return Task.FromResult(new MockApiResponse
                {
                    StatusCode = 200,
                    Payload = Newtonsoft.Json.JsonConvert.SerializeObject(responseObject),
                    ContentType = "application/json"
                });
            }

            return Task.FromResult(new MockApiResponse
            {
                StatusCode = 404,
                Payload = "Path not setup"
            });
        }
    }
}
