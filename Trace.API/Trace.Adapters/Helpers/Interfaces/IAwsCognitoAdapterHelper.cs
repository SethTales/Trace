using System.Threading.Tasks;
using Trace.Models;

namespace Trace.Adapters.Helpers.Interfaces
{
    public interface IAwsCognitoAdapterHelper
    {
        Task<bool> UserExists(AwsCognitoUser user);
        Task<bool> UserIsConfirmed(AwsCognitoUser user);
    }
}