using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OpenPop.Mime;

namespace Trace.API.IntegrationTests.Adapters.Interfaces
{
    internal interface IEmailAdapter
    {
        Message GetLatestMessage(string hostname, bool useSsl, int port, string username, string password);
        void DeleteAllMessages(string hostname, bool useSsl, int port, string username, string password);
    }
}
