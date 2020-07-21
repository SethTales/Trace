using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Trace.Adapters.Interfaces;

namespace Trace.API.IntegrationTests.Adapters.Interfaces
{
    internal interface ITestAwsCognitoAdapter : IAuthAdapter
    {
        Task AdminDeleteUser(string userName, string userPoolId);
    }
}
