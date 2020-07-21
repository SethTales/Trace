using System.Threading.Tasks;
using Trace.Models;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.CognitoIdentityProvider;
using System.Linq;
using System.Collections.Generic;
using Trace.Adapters.Helpers.Interfaces;

namespace Trace.Adapters.Helpers
{
    public class AwsCognitoAdapterHelper : IAwsCognitoAdapterHelper
    {
        private readonly string _userPoolId;
        private readonly IAmazonCognitoIdentityProvider _awsCognitoClient;


        public AwsCognitoAdapterHelper(AwsCognitoAdapterConfig cognitoAdapterConfig, IAmazonCognitoIdentityProvider awsCognitoClient)
        {
            _userPoolId = cognitoAdapterConfig.UserPoolId;
            _awsCognitoClient = awsCognitoClient;
        }
        public async Task<bool> UserExists(AwsCognitoUser user)
        {
            return await LookupUserByEmailAsync(_userPoolId, user.UserName) != null;
        }

        public async Task<bool> UserIsConfirmed(AwsCognitoUser user)
        {
            var cognitoUser = await LookupUserByEmailAsync(_userPoolId, user.UserName);
            return cognitoUser.UserStatus == UserStatusType.CONFIRMED;
        }

        public Task<UserType> GetUserInfo(AwsCognitoUser user)
        {
            return LookupUserByEmailAsync(_userPoolId, user.UserName);
        }

        private async Task<UserType> LookupUserByEmailAsync(string userPoolId, string userName)
        {
            var listUsersRequest = new ListUsersRequest()
            {
                AttributesToGet = new List<string>() { "sub" },
                Filter = $"email=\"{userName}\"",
                UserPoolId = userPoolId
            };
            var listUsersResponse = await _awsCognitoClient.ListUsersAsync(listUsersRequest);
            var user = listUsersResponse.Users.FirstOrDefault();
            return user;
        }
    }
}