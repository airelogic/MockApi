using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http.Features;

namespace MockApi.Server.Handlers
{
    internal class WebRequestHandler : RequestHandler
    {
        private readonly IFileReader _fileReader;

        public WebRequestHandler(RouteCache routeCache, IFileReader fileReader) : base(routeCache)
        {
            _fileReader = fileReader;
        }

        public override async Task<MockApiResponse> ProcessRequest(IHttpRequestFeature request)
        {
            var requestMethod = new HttpMethod(request.Method);
            var requestPath = request.Path;
            var session = request.GetSessionId();
            var routeMatch = RouteCache.GetBestRouteMatch(requestMethod, requestPath, session);

            if (routeMatch != null)
            {
                var bodyText = await request.Body?.ReadAsTextAsync();
                var query = request.GetQuery();
                routeMatch.Setup.LogRequest(request.Path, request.Method, bodyText, request.Headers);
                var response = routeMatch.GetResponse(bodyText, query, request.Headers);

                if (response.StartsWith("file:"))
                {
                    var templateFile = response.Substring("file:".Length);
                    try
                    {
                        response = await _fileReader.ReadContentsAsync(templateFile);
                    }
                    catch (System.IO.IOException)
                    {
                        return new MockApiResponse
                        {
                            StatusCode = 404,
                            Payload = $"Unable to load template {templateFile}",
                            ContentType = "text/plain"
                        };
                    }
                }

                return new MockApiResponse
                {
                    StatusCode = routeMatch.Setup.StatusCode,
                    Payload = response,
                    ContentType = "application/json; charset=utf-8",
                    Headers = routeMatch.ProcessHeaders(routeMatch.Setup.Headers, bodyText, query)
                };
            }

            return new MockApiResponse
            {
                StatusCode = 404,
                Payload = "Method not set up",
                ContentType = "text/plain"
            };
        }
    }
}
