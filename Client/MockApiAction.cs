using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MockApi.Client
{
    public class MockApiAction
    {
        private readonly string _host;
        private readonly string _path;
        private readonly string _method;

        private readonly bool _onceOnly;

        public MockApiAction(string host, string method, string path, bool onceOnly)
        {
            _host = host;
            _path = path;
            _method = method;
            _onceOnly = onceOnly;
        }

        public async Task Returns(int statusCode, string document)
        {
            var uri = $"{_host}/{_path}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("MockApi-Action", "Setup");
                client.DefaultRequestHeaders.Add("MockApi-Method", _method);
                client.DefaultRequestHeaders.Add("MockApi-Status", statusCode.ToString());
                if (_onceOnly)
                    client.DefaultRequestHeaders.Add("MockApi-Flag-Once", "true");
                var response = await client.PostAsync(uri, new StringContent(document, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task ReturnsJson(object json)
        {
            await ReturnsJson(200, json);
        }

        public async Task ReturnsJson(int statusCode, object json)
        {
            await Returns(statusCode, JsonConvert.SerializeObject(json));
        }
    }
}
