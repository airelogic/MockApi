using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace MockApi.Server
{
    public static class HttpRequestExtensions
    {
        public static MockApiAction GetMockApiAction(this IHttpRequestFeature request)
        {
            Func<string, MockApiAction> parseAction = (s) => (MockApiAction)Enum.Parse(typeof(MockApiAction), s);
            const string header = "MockApi-Action";
            return request.Headers.ContainsKey(header) ? parseAction(request.Headers[header]) : MockApiAction.Call;
        }

        public static HttpMethod GetMockApiMethod(this IHttpRequestFeature request)
        {
            const string header = "MockApi-Method";
            if (request.Headers.ContainsKey(header))
            {
                return new HttpMethod(request.Headers[header]);
            }

            return HttpMethod.Get;
        }

        public static int GetMockApiStatus(this IHttpRequestFeature request)
        {
            const string header = "MockApi-Status";
            if (request.Headers.ContainsKey(header))
            {
                return int.Parse(request.Headers[header], null);
            }

            return 200;
        }

        public static bool GetMockApiFlag(this IHttpRequestFeature request, string flag)
        {
            string header = $"MockApi-Flag-{flag}";
            if (request.Headers.ContainsKey(header))
            {
                return bool.Parse(request.Headers[header]);
            }

            return false;
        }

        public static string GetSessionId(this IHttpRequestFeature request)
        {
            const string header = "MockApi-Session";
            if(request.Headers.ContainsKey(header))
            {
                return request.Headers[header];
            }
            return "";
        }

        public static Dictionary<string, StringValues> GetQuery(this IHttpRequestFeature request)
        {
            return Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(request.QueryString);
        }

        public static Task<string> ReadAsTextAsync(this Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEndAsync();
            }
        }
    }
}