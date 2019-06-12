using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Kakegurui.Web.Config
{
    /// <summary>
    /// http配置读取实现
    /// </summary>
    public class HttpConfigurationProvider : ConfigurationProvider
    {
        private readonly string _systemUrl;
        private readonly int _config;

        internal class Parameter
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }

        public HttpConfigurationProvider(string systemUrl, int config)
        {
            _systemUrl = systemUrl;
            _config = config;
        }

        public override void Load()
        {
            using (HttpClient client = new HttpClient())
            {
                Task<HttpResponseMessage> response = client.GetAsync($"http://{_systemUrl}/api/parameters/systems/{_config}");
                response.Wait();
                Task<string> json= response.Result.Content.ReadAsStringAsync();
                json.Wait();
                List<Parameter> parameters = JsonConvert.DeserializeObject<List<Parameter>>(json.Result);
                Data = parameters.ToDictionary(p => p.Key, p => p.Value);
            }
        }
    }

    /// <summary>
    /// http配置源
    /// </summary>
    public class HttpConfigurationSource : IConfigurationSource
    {
        private readonly string _systemUrl;
        private readonly int _config;
        public HttpConfigurationSource(string systemUrl, int config)
        {
            _systemUrl = systemUrl;
            _config = config;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new HttpConfigurationProvider(_systemUrl, _config);
        }
    }

    /// <summary>
    /// http接口读取扩展
    /// </summary>
    public static class HttpExtensions
    {
        public static IConfigurationBuilder AddHttpConfiguration(
            this IConfigurationBuilder builder,
            string systemUrl, int config)
        {
            return builder.Add(new HttpConfigurationSource(systemUrl, config));
        }
    }
}
