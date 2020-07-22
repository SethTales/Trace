using System;
using System.Net.Http;
using System.Threading.Tasks;
using Trace.Models;

namespace Trace.API.Client.Handlers
{
    public interface IHttpHandler
    {
        Task<HttpResponseMessage> SendRequestWithBodyAsync(object content, HttpMethod method, string endpoint, TokenResponse token = null);
    }
}
