using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MockApi.Server
{
    internal class RouteCache
    {
        private readonly List<RouteSetup> _routeSetups = new List<RouteSetup>();
        private readonly IFileReader _fileReader;
        private readonly Options _options;

        public RouteCache(IFileReader fileReader, IOptions<Options> options)
        {
            _fileReader = fileReader;
            _options = options.Value;
        }

        public async Task Initialise()
        {
            if (string.IsNullOrEmpty(_options.RoutesFile) == false && _routeSetups.Any() == false)
            {
                System.Console.WriteLine(_options.RoutesFile);
                LoadRoutes(await _fileReader.ReadContentsAsync(_options.RoutesFile));
            }
        }

        public void LoadRoutes(string routesDocument)
        {
            var routes = Newtonsoft.Json.JsonConvert.DeserializeObject<RouteSetupInfo[]>(routesDocument);
            RegisterRoutes(routes);
        }

        public void RegisterRouteSetup(HttpMethod method, PathString path, string response, int statusCode, bool onceOnly)
        {
            RegisterRouteSetup(method, path, response, statusCode, new Dictionary<string, string>(), onceOnly, string.Empty);
        }

        public void RegisterRouteSetup(HttpMethod method, PathString path, string response, int statusCode, Dictionary<string, string> headers, bool onceOnly, string queryString)
        {
            if (onceOnly)
                _routeSetups.RemoveAll(r => r.Path == path && r.Method == method);

            _routeSetups.Add(new RouteSetup(method, path, response, statusCode, headers, onceOnly, queryString));
        }

        public IEnumerable<RouteSetup> GetRouteSetups(HttpMethod method, PathString path)
        {
            return _routeSetups
                .Where(r => r.Path == path && r.Method == method)
                .OrderByDescending(r => r.CreationDateTime);
        }

        public RouteMatch GetBestRouteMatch(HttpMethod method, PathString path, string queryString)
        {
            var tempMatches = _routeSetups.Select(r => r.MatchesOn(method, path, queryString))
                .Where(x => x.Success).ToList();

            var matchGroup = _routeSetups.Select(r => r.MatchesOn(method, path, queryString))
                    .Where(rm => rm.Success)
                    .GroupBy(r => r.WildcardCount)
                    .OrderBy(grp => grp.Key)
                    .FirstOrDefault();

            return matchGroup?
                .OrderByDescending(r => r.Setup.CreationDateTime)
                .FirstOrDefault(rm => rm.Success);
        }

        private void RegisterRoutes(RouteSetupInfo[] routes)
        {
            foreach (var route in routes)
            {
                var method = new HttpMethod(route.Method);
                var response = route.Response.ToString();
                RegisterRouteSetup(method, route.Path, response, route.Status, route.Headers, false, route.QueryString);
            }
        }

        public class Options
        {
            public string RoutesFile { get; set; }
        }
    }
}