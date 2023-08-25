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
        private readonly bool _success;
        private readonly Dictionary<string, string> _wildcards;

        public RouteMatch(RouteSetup routeSetup, Dictionary<string, string> wildcards)
        {
            _success = true;
            _wildcards = wildcards;
            _routeSetup = routeSetup;
        }

        private RouteMatch(bool success)
        {
            _success = success;
        }

        public static RouteMatch NoMatch => new RouteMatch(false);

        public bool Success => _success;

        public RouteSetup Setup => _routeSetup;

        public int WildcardCount => _wildcards.Count;

        public string GetResponse(string body, Dictionary<string, StringValues> query, IHeaderDictionary headers)
        {
            var response = _routeSetup.Response;
            var placeholders = Regex.Matches(response, @"{(.+)}");
            var payloadObjects = BodyAsObject(body);

            foreach (Match placeholder in placeholders)
            {
                var key = placeholder.Groups[1].Value;
                Func<string, string> valueProcessor = str => str;

                if(key.Contains("@"))
                {
                    var parts = key.Split("@");
                    if(parts.Length != 2)
                    {
                        throw new FormatException("Badly formatted substitution");
                    }
                    key = parts[0];
                    valueProcessor = str => ProcessRegex(str, parts[1]);
                }

                if (_wildcards.ContainsKey(key))
                {
                    response = response.Replace(placeholder.Value, valueProcessor(_wildcards[key]), StringComparison.InvariantCulture);
                }
                else if (query.ContainsKey(key))
                {
                    response = response.Replace(placeholder.Value, valueProcessor(query[key].First()), StringComparison.InvariantCulture);
                }
                else if (payloadObjects.Any())
                {
                    foreach (var obj in payloadObjects)
                    {
                        var valueFromBody = obj.SelectToken(key);
                        if (valueFromBody != null)
                        {
                            response = response.Replace(placeholder.Value, valueProcessor(valueFromBody.ToString()), StringComparison.InvariantCulture);
                            break;
                        }
                    }
                }
                else if (headers.ContainsKey(key))
                {
                    response = response.Replace(placeholder.Value, valueProcessor(headers[key].First()), StringComparison.InvariantCulture);
                }
            }

            return response;
        }

        private string ProcessRegex(string input, string pattern)
        {
            var match = Regex.Match(input, pattern);
            if(match.Success == false)
                throw new FormatException("Regex match failed");
            return match.Groups[1].Value;
        }

        private static JArray BodyAsObject(string body)
        {
            if (body.StartsWith('[') == false)
                body = $"[{body}]";

            return JArray.Parse(body);
        }
    }
}
