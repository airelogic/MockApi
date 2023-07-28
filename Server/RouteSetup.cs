using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace MockApi.Server
{
    public class RouteSetup
    {
        private readonly HttpMethod _method;
        private readonly PathString _path;
        private readonly string _response;
        private readonly int _status;
        private readonly Dictionary<string, string> _headers;
        private readonly List<HttpRequestDetails> _requests;
        private readonly bool _onceOnly;
        private readonly string _session;
        private DateTime _creationDateTime;

        public RouteSetup(HttpMethod method, PathString path, string response, int status, Dictionary<string, string> headers, bool onceOnly, string session)
        {
            _method = method;
            _path = path;
            _response = response;
            _status = status;
            _headers = headers;
            _requests = new List<HttpRequestDetails>();
            _onceOnly = onceOnly;
            _creationDateTime = DateTime.UtcNow;
            _session = session;
        }

        public HttpMethod Method => _method;

        public string Path => _path;

        public string Response => _response;

        public int StatusCode => _status;

        public string SessionId => _session;

        public DateTime CreationDateTime => _creationDateTime;

        public Dictionary<string, string> Headers => _headers;

        public IEnumerable<HttpRequestDetails> Requests => _requests.ToList().AsReadOnly();

        public bool Archived => _onceOnly && Requests.Any();

        public void LogRequest(string path, string method, string body, IDictionary<string, StringValues> headers)
        {
            var flattenedHeaders = headers.ToDictionary(x => x.Key, x => x.Value.First());
            LogRequest(new HttpRequestDetails
            {
                Path = path,
                Method = method,
                Body = body,
                Headers = flattenedHeaders
            });
        }

        public void LogRequest(HttpRequestDetails requestDetails)
        {
            _requests.Add(requestDetails);
        }

        public RouteMatch MatchesOn(HttpMethod method, PathString requestPath, string session)
        {
            var sessionMatch = _session == string.Empty ?
                MatchResult.DefaultMatch :
                _session == session ? MatchResult.SessionMatch : MatchResult.NoMatch;

            if (method == _method && Archived == false && sessionMatch != MatchResult.NoMatch)
            {
                var routeParts = _path.GetSegments();
                var requestParts = requestPath.GetSegments();
                var wildcards = new Dictionary<string, string>();

                if (routeParts.Length == requestParts.Length)
                {
                    for (int i = 0; i < routeParts.Length; i++)
                    {
                        var routePart = routeParts[i];
                        var requestPart = requestParts[i];

                        if (routePart.StartsWith('{'))
                        {
                            var wildcardKey = routePart.Substring(1, routePart.Length - 2);
                            wildcards.Add(wildcardKey, requestPart);
                        }
                        else if (routePart != requestPart)
                        {
                            return RouteMatch.NoMatch;
                        }
                    }

                    return new RouteMatch(this, wildcards, sessionMatch);
                }
            }

            return RouteMatch.NoMatch;
        }
    }
}
