using System.Collections.Generic;

namespace MockApi.Server
{
    public class HttpRequestDetails
    {
        public string Path { get; set; }
        public string Method { get; set; }
        public string Body { get; set; }
        public IDictionary<string, string> Headers { get; set; }
    }
}