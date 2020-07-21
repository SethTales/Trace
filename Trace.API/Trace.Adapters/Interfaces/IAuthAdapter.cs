using System;
using System.Threading.Tasks;
using System.Net.Http;
using Trace.Models;

namespace Trace.Adapters.Interfaces
{
    public interface IAuthAdapter
    {
        Task<HttpResponseMessage> RegisterNewUserAsync(AwsCognitoUser user);
        Task<HttpResponseMessage> ConfirmUserAsync(AwsCognitoUser user);
        Task<HttpResponseMessage> AuthenticateUserAsync(AwsCognitoUser user);
        Task<HttpResponseMessage> AdminDeleteUser(AwsCognitoUser user);
    }
}
