using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider.Model;
using Trace.Models;

namespace Trace.Adapters.Helpers.Interfaces
{
    public interface IAwsCognitoAdapterHelper
    {
        Task<bool> UserExists(AwsCognitoUser user);
        Task<bool> UserIsConfirmed(AwsCognitoUser user);
        Task<UserType> GetUserInfo(AwsCognitoUser user);
    }
}