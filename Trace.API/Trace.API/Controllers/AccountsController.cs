﻿using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Amazon.CognitoIdentityProvider.Model;
using Newtonsoft.Json;
using Trace.Adapters.Interfaces;
using Trace.Models;

namespace Trace.API.Controllers
{
    [Route("[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAuthAdapter _authAdapter;

        public AccountsController(IAuthAdapter authAdapter)
        {
            _authAdapter = authAdapter;
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateAccount([FromBody] AwsCognitoUser cognitoUser)
        {
            var signUpResponse = await _authAdapter.RegisterNewUserAsync(cognitoUser);

            switch (signUpResponse.StatusCode)
            {
                case HttpStatusCode.Conflict:
                    return new ConflictObjectResult($"The email address {cognitoUser.UserName} already has an account associated with it.");
                case HttpStatusCode.Created:
                    return new OkObjectResult($"Account successfully created. Please check your email for a confirmation code.");
                default:
                    return new ContentResult
                    {
                        ContentType = "text/plain",
                        Content = "An error has occurred",
                        StatusCode = (int) signUpResponse.StatusCode
                    };
            }
        }

        [HttpPost]
        [Route("user/status")]
        public async Task<IActionResult> ConfirmAccount(AwsCognitoUser cognitoUser, string message = "")
        {
            HttpResponseMessage confirmSignUpResponse;

            try
            {
                confirmSignUpResponse = await _authAdapter.ConfirmUserAsync(cognitoUser);
            }
            catch (CodeMismatchException)
            {
                return new ContentResult
                {
                    Content = "Invalid verification code provided.",
                    ContentType = "text/plain",
                    StatusCode = (int) HttpStatusCode.BadRequest
                };
            }
            catch (ExpiredCodeException)
            {
                return new ContentResult
                {
                    Content = "Verification code expired. Client must request a new code.",
                    ContentType = "text/plain",
                    StatusCode = (int) HttpStatusCode.BadRequest
                };
            }

            switch (confirmSignUpResponse.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    return new NotFoundObjectResult($"The username {cognitoUser.UserName} does not exist.");
                case HttpStatusCode.Conflict:
                    return new ConflictObjectResult($"The username {cognitoUser.UserName} is already confirmed. Login to continue.");
                case HttpStatusCode.OK:
                    return new OkObjectResult("Account confirmed. Please login to continue.");
                default:
                    return new ContentResult
                    {
                        ContentType = "text/plain",
                        Content = "An error has occurred",
                        StatusCode = (int) confirmSignUpResponse.StatusCode
                    };
            }
        }

        [HttpPost]
        [Route("users/authenticate")]
        public async Task<IActionResult> Login(AwsCognitoUser cognitoUser, string message = "")
        {
            var loginResponse = await _authAdapter.AuthenticateUserAsync(cognitoUser);

            switch (loginResponse.StatusCode)
            {
                case HttpStatusCode.OK:
                    var authResult = JsonConvert.DeserializeObject<AuthenticationResultType>(await loginResponse.Content.ReadAsStringAsync());
                    var tokenResponse = new TokenResponse
                    {
                        IdToken = authResult.IdToken,
                        RefreshToken = authResult.RefreshToken,
                        AccessToken = authResult.AccessToken
                    };
                    return new ContentResult
                    {
                        Content = JsonConvert.SerializeObject(tokenResponse),
                        ContentType = "application/json",
                        StatusCode = (int)HttpStatusCode.OK
                    };
                case HttpStatusCode.BadRequest:
                    return new BadRequestObjectResult($"Login failed. User {cognitoUser.UserName} is unconfirmed.");
                case HttpStatusCode.NotFound:
                    return new NotFoundObjectResult($"Login failed. User {cognitoUser.UserName} does not exist.");
                default:
                    return new ContentResult
                    {
                        ContentType = "text/plain",
                        Content = "An error has occurred",
                        StatusCode = (int) loginResponse.StatusCode
                    };
            }
        }
    }
}