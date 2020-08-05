using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polly;
using Trace.API.Client.Handlers;
using Trace.Models;

namespace Trace.API.Client
{
    public class TraceApiClient : ITraceApiClient
    {
        private static TokenResponse _token;
        private const string CreateAccountEndpoint = "accounts/create";
        private const string ConfirmAccountEndpoint = "accounts/user/status";
        private const string LoginEndpoint = "accounts/users/authenticate";
        private const string ResetPasswordEndpoint = "accounts/user/password/reset";
        private const string ConfirmPasswordEndpoint = "accounts/user/password/confirm";
        private readonly TimeSpan _minWaitBeforeRetry = TimeSpan.FromMilliseconds(500);
        private readonly TimeSpan _maxWaitBeforeRetry = TimeSpan.FromMilliseconds(4000);
        private readonly int _maxRetries = 3;

        private readonly Func<HttpResponseMessage, bool> _circuitBreaker = r => r.StatusCode != HttpStatusCode.BadRequest && r.StatusCode != HttpStatusCode.OK;

        private readonly IHttpHandler _httpHandler;

        public TraceApiClient(IHttpHandler httpHandler)
        {
            _httpHandler = httpHandler;
        }

        public async Task<HttpResponseMessage> CreateAccountWithRetryAsync(AwsCognitoUser user)
        {
            return await Policy.HandleResult(_circuitBreaker)
                .WaitAndRetryAsync(DecorrelatedJitter())
                .ExecuteAsync(ex =>
                    _httpHandler.SendRequestWithBodyAsync(user, HttpMethod.Post, CreateAccountEndpoint), CancellationToken.None);

        }

        public async Task<HttpResponseMessage> ConfirmAccountWithRetryAsync(AwsCognitoUser user)
        {
            return await Policy.HandleResult(_circuitBreaker)
                .WaitAndRetryAsync(DecorrelatedJitter())
                .ExecuteAsync(ex =>
                    _httpHandler.SendRequestWithBodyAsync(user, HttpMethod.Post, ConfirmAccountEndpoint), CancellationToken.None);
        }

        public async Task<HttpResponseMessage> LoginWithRetryAsync(AwsCognitoUser user)
        {
            var respsone = await Policy.HandleResult(_circuitBreaker)
                .WaitAndRetryAsync(DecorrelatedJitter())
                .ExecuteAsync(ex =>
                    _httpHandler.SendRequestWithBodyAsync(user, HttpMethod.Post, LoginEndpoint), CancellationToken.None);
            _token = JsonConvert.DeserializeObject<TokenResponse>(await respsone.Content.ReadAsStringAsync());
            return respsone;
        }

        public async Task<HttpResponseMessage> ResetPasswordWithRetryAsync(AwsCognitoUser user)
        {
            return await Policy.HandleResult(_circuitBreaker)
                .WaitAndRetryAsync(DecorrelatedJitter())
                .ExecuteAsync(ex =>
                    _httpHandler.SendRequestWithBodyAsync(user, HttpMethod.Post, ResetPasswordEndpoint, _token), CancellationToken.None);
        }

        public async Task<HttpResponseMessage> ConfirmPasswordWithRetryAsync(AwsCognitoUser user)
        {
            return await Policy.HandleResult(_circuitBreaker)
                .WaitAndRetryAsync(DecorrelatedJitter())
                .ExecuteAsync(ex =>
                    _httpHandler.SendRequestWithBodyAsync(user, HttpMethod.Post, ConfirmPasswordEndpoint, _token), CancellationToken.None);
        }

        private IEnumerable<TimeSpan> DecorrelatedJitter()
        {
            var jitterer = new Random();
            var retries = 0;
            var seed = _minWaitBeforeRetry.TotalMilliseconds;
            var max = _maxWaitBeforeRetry.TotalMilliseconds;
            var current = seed;

            while (++retries <= _maxRetries)
            {
                current = Math.Min(max, Math.Max(seed, current * 3 * jitterer.NextDouble()));
                yield return TimeSpan.FromMilliseconds(current);
            }
        }
    }
}
