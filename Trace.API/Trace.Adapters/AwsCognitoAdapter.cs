using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Trace.Models;
using Trace.Adapters.Interfaces;
using System.Collections.Generic;
using Newtonsoft.Json;
using Trace.Adapters.Helpers.Interfaces;

namespace Trace.Adapters
{
    public class AwsCognitoAdapter : IAuthAdapter
    {
        private readonly IAmazonCognitoIdentityProvider _awsCognitoClient;
        private readonly IAwsCognitoAdapterHelper _cognitoAdapterHelper;
        private readonly string _clientId;

        public AwsCognitoAdapter(
            IAmazonCognitoIdentityProvider awsCognitoClient,
            AwsCognitoAdapterConfig cognitoConfig,
            IAwsCognitoAdapterHelper cognitoAdapterHelper)
        {
            _awsCognitoClient = awsCognitoClient;
            _clientId = cognitoConfig.ClientId;
            _cognitoAdapterHelper = cognitoAdapterHelper;
        }

        public async Task<HttpResponseMessage> RegisterNewUserAsync(AwsCognitoUser user)
        {
            if (await _cognitoAdapterHelper.UserExists(user))
            {
                return new HttpResponseMessage(HttpStatusCode.Conflict);
            }

            var signUpRequest = new SignUpRequest()
            {
                Username = user.UserName,
                Password = user.Password,
                ClientId = _clientId
            };
            var signUpResponse = await _awsCognitoClient.SignUpAsync(signUpRequest);

            return new HttpResponseMessage(HttpStatusCode.Created);
        }

        public async Task<HttpResponseMessage> ConfirmUserAsync(AwsCognitoUser user)
        {
            if (!await _cognitoAdapterHelper.UserExists(user))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (await _cognitoAdapterHelper.UserIsConfirmed(user))
            {
                return new HttpResponseMessage(HttpStatusCode.Conflict);
            }

            var confirmSignupRequest = new ConfirmSignUpRequest
            {
                Username = user.UserName,
                ConfirmationCode = user.ConfirmationCode,
                ClientId = _clientId
            };
            await _awsCognitoClient.ConfirmSignUpAsync(confirmSignupRequest);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        public async Task<HttpResponseMessage> AuthenticateUserAsync(AwsCognitoUser user)
        {
            if (!await _cognitoAdapterHelper.UserExists(user))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (!await _cognitoAdapterHelper.UserIsConfirmed(user))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var authRequest = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    {"USERNAME", user.UserName},
                    {"PASSWORD", user.Password}
                },
                ClientId = _clientId
            };
            var authResponse = await _awsCognitoClient.InitiateAuthAsync(authRequest);

            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(authResponse.AuthenticationResult))
            };
        }
    }
}