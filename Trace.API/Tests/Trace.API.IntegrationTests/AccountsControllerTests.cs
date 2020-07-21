using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using Trace.Adapters.Interfaces;
using Trace.API.IntegrationTests.Adapters;
using Trace.API.IntegrationTests.Adapters.Interfaces;
using Trace.API.IntegrationTests.Models;
using Trace.Models;

namespace Trace.API.IntegrationTests
{
    public class AccountsControllerIntegrationTests
    {
        private const string BaseAddress = "http://localhost";
        private const string CreateAccountEndpoint = "accounts/create";
        private const string ConfirmAccountEndpoint = "accounts/user/status";
        private const string LoginEndpoint = "accounts/users/authenticate";
        private const string TestUserSecretId = "dev/trace/test-user-creds";

        private AwsCognitoUser _user;
        private TestServer _testServer;
        private IEmailAdapter _emailAdapter;
        private TestUserCreds _testUserCreds;
        private IAuthAdapter _authAdapter;
        private HttpClient _httpClient;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _testServer = new TestServer();
            _httpClient = _testServer.CreateClient();
            var secretsClient = new AmazonSecretsManagerClient(RegionEndpoint.USWest2);
            var secretResponse = secretsClient.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = TestUserSecretId
            }).Result;
            _testUserCreds = JsonConvert.DeserializeObject<TestUserCreds>(secretResponse.SecretString);
            _emailAdapter = new EmailAdapter();
            _user = new AwsCognitoUser
            {
                FirstName = "test",
                LastName = "user",
                PhoneNumber = "+12223334444",
                UserName = _testUserCreds.TraceTestUserEmail,
                Password = _testUserCreds.TraceTestUserPassword
            };
            var serviceProvider = _testServer.GetTestServiceProvider();
            _authAdapter = serviceProvider.GetRequiredService<IAuthAdapter>();
        }

        [TearDown]
        public async Task TearDown()
        {
            await CleanUpTestData();
        }

        [Test]
        public async Task UserCanSignUp_AndRetrieve_JWT_Token()
        {
            var createRequest = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(_user)),
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{BaseAddress}/{CreateAccountEndpoint}")
            };
            createRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var createResponse = await _httpClient.SendAsync(createRequest);

            Assert.IsTrue(createResponse.IsSuccessStatusCode);

            Thread.Sleep(2500); // wait a little while to ensure email delivery
            var authenticationEmail = _emailAdapter.GetLatestMessage("pop.gmail.com", true, 995,
                _testUserCreds.TraceTestUserEmail, _testUserCreds.TraceTestUserPassword);
            var messageHtml = authenticationEmail.FindFirstHtmlVersion();
            var emailContent = Encoding.UTF8.GetString(messageHtml.Body);
            var confirmationCode = GetConfirmationCodeFromEmailBody(emailContent);
            _user.ConfirmationCode = confirmationCode;

            var confirmRequest = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(_user)),
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{BaseAddress}/{ConfirmAccountEndpoint}")
            };
            confirmRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var confirmResponse = await _httpClient.SendAsync(confirmRequest);

            Assert.IsTrue(confirmResponse.IsSuccessStatusCode);

            var loginRequest = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(_user)),
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{BaseAddress}/{LoginEndpoint}")
            };
            loginRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var loginResponse = await _httpClient.SendAsync(loginRequest);
            //TODO: verify token is legit
            var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();

            Assert.IsTrue(loginResponse.IsSuccessStatusCode);
        }

        private async Task CleanUpTestData()
        {
            await _authAdapter.AdminDeleteUser(_user);
            _emailAdapter.DeleteAllMessages("pop.gmail.com", true, 995,
                _testUserCreds.TraceTestUserEmail, _testUserCreds.TraceTestUserPassword);
        }

        private string GetConfirmationCodeFromEmailBody(string emailBody)
        {
            var re = new Regex(@"([0-9]){6}");
            return re.Match(emailBody).Value;
        }
    }
}
