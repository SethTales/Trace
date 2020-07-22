using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
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
        private const string ResetPasswordEndpoint = "accounts/user/password/reset";
        private const string ConfirmPasswordEndpoint = "accounts/user/password/confirm";
        private const string TestUserSecretId = "dev/trace/test-user-creds";

        private AwsCognitoUser _user;
        private TestServer _testServer;
        private IEmailAdapter _emailAdapter;
        private TestUserCreds _testUserCreds;
        private IAuthAdapter _authAdapter;
        private HttpClient _httpClient;
        private string _token;

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
        public async Task UserCanSignUp_ConfirmAccount_AndLogin()
        {
            var createResponse = await CreateAccount();
            Assert.IsTrue(createResponse.IsSuccessStatusCode);

            var confirmResponse = await ConfirmAccount();
            Assert.IsTrue(confirmResponse.IsSuccessStatusCode);

            var loginResponse = await Login();
            Assert.IsTrue(loginResponse.IsSuccessStatusCode);
        }

        [Test]
        public async Task UserCanResetPassword()
        {
            await CreateAccount();
            await ConfirmAccount();
            var loginResponse = await Login();
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(await loginResponse.Content.ReadAsStringAsync());
            _token = tokenResponse.IdToken;

            var resetPasswordResponse = await ResetPassword();
            Assert.IsTrue(resetPasswordResponse.IsSuccessStatusCode);

            _user.Password = "abcdABCD1234&";
            var confirmPasswordRespsone = await ConfirmNewPassword();
            Assert.IsTrue(confirmPasswordRespsone.IsSuccessStatusCode);
        }

        private async Task<HttpResponseMessage> CreateAccount()
        {
            return await SendHttpRequestWithBody(_user, HttpMethod.Post, CreateAccountEndpoint);
        }

        private async Task<HttpResponseMessage> ConfirmAccount()
        {
            _user.ConfirmationCode = GetVerificationCodeFromEmail();
            return await SendHttpRequestWithBody(_user, HttpMethod.Post, ConfirmAccountEndpoint);
        }

        private async Task<HttpResponseMessage> Login()
        {
            return await SendHttpRequestWithBody(_user, HttpMethod.Post, LoginEndpoint);
        }

        private async Task<HttpResponseMessage> ResetPassword()
        {
            return await SendHttpRequestWithBody(_user, HttpMethod.Post, ResetPasswordEndpoint, _token);
        }

        private async Task<HttpResponseMessage> ConfirmNewPassword()
        {
            _user.ConfirmationCode = GetVerificationCodeFromEmail();
            return await SendHttpRequestWithBody(_user, HttpMethod.Post, ConfirmPasswordEndpoint, _token);
        }

        private async Task<HttpResponseMessage> SendHttpRequestWithBody(object content, HttpMethod method, string endpoint, string token = null)
        {
            var request = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(content)),
                Method = method,
                RequestUri = new Uri($"{BaseAddress}/{endpoint}")
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            if (token != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }
            return await _httpClient.SendAsync(request);
        }

        private string GetVerificationCodeFromEmail()
        {
            Thread.Sleep(2500); // wait a little while to ensure email delivery

            var authenticationEmail = _emailAdapter.GetLatestMessage("pop.gmail.com", true, 995,
                _testUserCreds.TraceTestUserEmail, _testUserCreds.TraceTestUserPassword);
            var messageHtml = authenticationEmail.FindFirstHtmlVersion();
            var emailContent = Encoding.UTF8.GetString(messageHtml.Body);
            var confirmationCode = GetConfirmationCodeFromEmailBody(emailContent);
            return confirmationCode;
        }

        private async Task CleanUpTestData()
        {
            await _authAdapter.AdminDeleteUser(_user);
            _emailAdapter.DeleteAllMessages("pop.gmail.com", true, 995,
                _testUserCreds.TraceTestUserEmail, _testUserCreds.TraceTestUserPassword);
        }

        private static string GetConfirmationCodeFromEmailBody(string emailBody)
        {
            var re = new Regex(@"([0-9]){6}");
            return re.Match(emailBody).Value;
        }
    }
}
