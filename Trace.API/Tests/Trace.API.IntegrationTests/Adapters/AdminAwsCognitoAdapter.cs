using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Trace.Adapters;
using Trace.Adapters.Helpers.Interfaces;
using Trace.Models;

namespace Trace.API.IntegrationTests.Adapters
{
    internal class AdminAwsCognitoAdapter : AwsCognitoAdapter
    {
        internal AdminAwsCognitoAdapter(IAmazonCognitoIdentityProvider awsCognitoClient, AwsCognitoAdapterConfig cognitoConfig, IAwsCognitoAdapterHelper cognitoAdapterHelper) : base(awsCognitoClient, cognitoConfig, cognitoAdapterHelper)
        {
        }

        internal async Task AdminDeleteUser(AwsCognitoUser user)
        {
            var userInfo = await _cognitoAdapterHelper.GetUserInfo(user);
            await _awsCognitoClient.AdminDeleteUserAsync(new AdminDeleteUserRequest
            {
                UserPoolId = _cognitoConfig.UserPoolId,
                Username = userInfo.Username
            });
        }
    }
}
