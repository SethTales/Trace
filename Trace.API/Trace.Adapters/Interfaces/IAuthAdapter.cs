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
        Task<HttpResponseMessage> ResetPasswordAsync(AwsCognitoUser user);
        Task<HttpResponseMessage> ConfirmNewPasswordAsync(AwsCognitoUser user);
        Task<HttpResponseMessage> AdminDeleteUser(AwsCognitoUser user);
    }
}
