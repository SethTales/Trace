using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Trace.API.Client.Config;
using Trace.Models;

namespace Trace.API.Client.Handlers
{
    public class HttpHandler : IHttpHandler
    {
        private readonly ClientConfig _configuration;
        private readonly HttpClient _httpClient;
        public HttpHandler(ClientConfig configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }
        public async Task<HttpResponseMessage> SendRequestWithBodyAsync(object content, HttpMethod method, string endpoint, TokenResponse token = null)
        {
            var request = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(content)),
                Method = method,
                RequestUri = new Uri($"{_configuration.BaseUri}/{endpoint}")
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            if (token != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.IdToken);
            }
            return await _httpClient.SendAsync(request);
        }
    }
}
