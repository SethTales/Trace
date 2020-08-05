using System.Net.Http;
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
using Trace.API.Client;
using Trace.API.Client.Config;
using Trace.API.Client.Handlers;
using Trace.API.IntegrationTests.Adapters;
using Trace.API.IntegrationTests.Adapters.Interfaces;
using Trace.API.IntegrationTests.Models;
using Trace.Models;

namespace Trace.API.IntegrationTests
{
    public class AccountsControllerIntegrationTests
    {
        private const string TestUserSecretId = "dev/trace/test-user-creds";

        private AwsCognitoUser _user;
        private TestServer _testServer;
        private IEmailAdapter _emailAdapter;
        private TestUserCreds _testUserCreds;
        private IAuthAdapter _authAdapter;
        private HttpClient _httpClient;

        private ClientConfig _clientConfig = new ClientConfig
        {
            BaseUri = "http://localhost"
        };
        private IHttpHandler _httpHandler;
        private ITraceApiClient _apiClient;

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
            _httpHandler = new HttpHandler(_clientConfig, _httpClient);
            _apiClient = new TraceApiClient(_httpHandler);
        }

        [TearDown]
        public async Task TearDown()
        {
            await CleanUpTestData();
        }

        [Test]
        public async Task UserCanSignUp_ConfirmAccount_AndLogin()
        {
            var createResponse = await _apiClient.CreateAccountWithRetryAsync(_user);
            Assert.IsTrue(createResponse.IsSuccessStatusCode);

            _user.ConfirmationCode = GetVerificationCodeFromEmail();
            var confirmResponse = await _apiClient.ConfirmAccountWithRetryAsync(_user);
            Assert.IsTrue(confirmResponse.IsSuccessStatusCode);

            var loginResponse = await _apiClient.LoginWithRetryAsync(_user);
            Assert.IsTrue(loginResponse.IsSuccessStatusCode);
        }

        [Test]
        public async Task UserCanResetPassword()
        {
            await _apiClient.CreateAccountWithRetryAsync(_user);
            _user.ConfirmationCode = GetVerificationCodeFromEmail();
            await _apiClient.ConfirmAccountWithRetryAsync(_user);
            await _apiClient.LoginWithRetryAsync(_user);

            var resetPasswordResponse = await _apiClient.ResetPasswordWithRetryAsync(_user);
            Assert.IsTrue(resetPasswordResponse.IsSuccessStatusCode);

            _user.ConfirmationCode = GetVerificationCodeFromEmail();
            _user.Password = "abcdABCD1234&";
            var confirmPasswordRespsone = await _apiClient.ConfirmPasswordWithRetryAsync(_user);
            Assert.IsTrue(confirmPasswordRespsone.IsSuccessStatusCode);
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
