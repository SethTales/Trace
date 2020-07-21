using NUnit.Framework;
using NSubstitute;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using Trace.Adapters;
using Trace.Adapters.Helpers;
using Trace.Adapters.Helpers.Interfaces;
using Trace.Models;

namespace Trace.API.UnitTests.AdapterTests
{
    [TestFixture]
    public class AwsCognitoAdapterHelperTests
    {
        private AwsCognitoAdapterConfig _cognitoAdapterConfig;
        private IAmazonCognitoIdentityProvider _awsCognitoClient;
        private IAwsCognitoAdapterHelper _cognitoAdapterHelper;

        [SetUp]
        public void Setup()
        {
            _cognitoAdapterConfig = new AwsCognitoAdapterConfig
            {
                UserPoolId = "fake-userpool-id",
                ClientId = "fake-client-id"
            };
            _awsCognitoClient = Substitute.For<IAmazonCognitoIdentityProvider>();
            _cognitoAdapterHelper = new AwsCognitoAdapterHelper(_cognitoAdapterConfig, _awsCognitoClient);
        }

        [Test]
        public async Task UserExists_ReturnsTrue_IfUserExists()
        {
            var user = new AwsCognitoUser
            {
                UserName = "fakeUsername",
                Password = "fakePassword"
            };
            _awsCognitoClient.ListUsersAsync(Arg.Any<ListUsersRequest>()).Returns(new ListUsersResponse
            {
                Users = new List<UserType>
                {
                    new UserType
                    {
                        Username = user.UserName
                    }
                }
            });

            var userExists = await _cognitoAdapterHelper.UserExists(user);

            Assert.IsTrue(userExists);
        }

        [Test]
        public async Task UserExists_ReturnsFalse_IfUserDoesNotExist()
        {
            var user = new AwsCognitoUser
            {
                UserName = "fakeUsername",
                Password = "fakePassword"
            };
            _awsCognitoClient.ListUsersAsync(Arg.Any<ListUsersRequest>()).Returns(new ListUsersResponse
            {
                Users = new List<UserType>()
            });

            var userExists = await _cognitoAdapterHelper.UserExists(user);

            Assert.IsFalse(userExists);
        }

        [Test]
        public async Task UserIsConfirmed_ReturnsTrue_IfUserIsInConfirmedState()
        {
            var user = new AwsCognitoUser
            {
                UserName = "fakeUsername",
                Password = "fakePassword"
            };
            _awsCognitoClient.ListUsersAsync(Arg.Any<ListUsersRequest>()).Returns(new ListUsersResponse
            {
                Users = new List<UserType>
                {
                    new UserType
                    {
                        Username = user.UserName,
                        UserStatus = UserStatusType.CONFIRMED
                    }
                }
            });

            var userIsConfirmed = await _cognitoAdapterHelper.UserIsConfirmed(user);

            Assert.IsTrue(userIsConfirmed);
        }

        [TestCaseSource("UserStatusCases")]
        public async Task UserIsConfirmed_ReturnsFalse_ForAllOtherStates(UserStatusType userStatus)
        {
            var user = new AwsCognitoUser
            {
                UserName = "fakeUsername",
                Password = "fakePassword"
            };
            _awsCognitoClient.ListUsersAsync(Arg.Any<ListUsersRequest>()).Returns(new ListUsersResponse
            {
                Users = new List<UserType>
                {
                    new UserType
                    {
                        Username = user.UserName,
                        UserStatus = userStatus
                    }
                }
            });

            var userIsConfirmed = await _cognitoAdapterHelper.UserIsConfirmed(user);

            Assert.IsFalse(userIsConfirmed);
        }

        static object[] UserStatusCases =
        {
            new object[] {UserStatusType.ARCHIVED},
            new object[] {UserStatusType.COMPROMISED},
            new object[] {UserStatusType.UNCONFIRMED},
            new object[] {UserStatusType.UNKNOWN},
            new object[] {UserStatusType.FORCE_CHANGE_PASSWORD},
            new object[] {UserStatusType.RESET_REQUIRED}
        };
    }
}