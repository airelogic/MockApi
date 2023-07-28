using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace MockApi.Server
{
    public class RouteMatch
    {
        private readonly RouteSetup _routeSetup;
        private readonly MatchResult _result;
        private readonly Dictionary<string, string> _wildcards;

        public RouteMatch(RouteSetup routeSetup, Dictionary<string, string> wildcards, MatchResult result)
        {
            _result = result;
            _wildcards = wildcards;
            _routeSetup = routeSetup;
        }

        private RouteMatch(MatchResult result)
        {
            _result = result;
        }

        public static RouteMatch NoMatch => new RouteMatch(MatchResult.NoMatch);

        public MatchResult Result => _result;

        public RouteSetup Setup => _routeSetup;

        public int WildcardCount => _wildcards.Count;

        public string GetResponse(string body, Dictionary<string, StringValues> query, IHeaderDictionary headers)
        {
            var response = _routeSetup.Response;
            var placeholders = Regex.Matches(response, @"{([A-Za-z0-9\.\[\]]+)}");
            var payloadObjects = BodyAsObject(body);

            foreach (Match placeholder in placeholders)
            {
                var key = placeholder.Groups[1].Value;
                if (_wildcards.ContainsKey(key))
                {
                    response = response.Replace(placeholder.Value, _wildcards[key], StringComparison.InvariantCulture);
                }
                else if (query.ContainsKey(key))
                {
                    response = response.Replace(placeholder.Value, query[key].First(), StringComparison.InvariantCulture);
                }
                else if (payloadObjects.Any())
                {
                    foreach (var obj in payloadObjects)
                    {
                        var valueFromBody = obj.SelectToken(key);
                        if (valueFromBody != null)
                        {
                            response = response.Replace(placeholder.Value, valueFromBody.ToString(), StringComparison.InvariantCulture);
                            break;
                        }
                    }
                }
                else if (headers.ContainsKey(key))
                {
                    response = response.Replace(placeholder.Value, headers[key].First(), StringComparison.InvariantCulture);
                }
            }

            return response;
        }

        private static JArray BodyAsObject(string body)
        {
            if (body.StartsWith('[') == false)
                body = $"[{body}]";

            return JArray.Parse(body);
        }
    }

    public enum MatchResult
    {
        NoMatch = 0,
        DefaultMatch = 1,
        SessionMatch = 2
    }
}
