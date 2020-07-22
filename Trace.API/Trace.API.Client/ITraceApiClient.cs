using System;
using System.Net.Http;
using System.Threading.Tasks;
using Trace.Models;

namespace Trace.API.Client
{
    public interface ITraceApiClient
    {
        Task<HttpResponseMessage> CreatAccountWithRetryAsync(AwsCognitoUser user);
        Task<HttpResponseMessage> ConfirmAccountWithReryAsync(AwsCognitoUser user);
        Task<HttpResponseMessage> LoginWithRetryAsync(AwsCognitoUser user);
        Task<HttpResponseMessage> ResetPasswordWithRetryAsync(AwsCognitoUser user);
        Task<HttpResponseMessage> ConfirmPasswordWithRetryAsync(AwsCognitoUser user);
    }
}
