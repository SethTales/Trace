using OpenPop.Mime;
using OpenPop.Pop3;
using Trace.API.IntegrationTests.Adapters.Interfaces;

namespace Trace.API.IntegrationTests.Adapters
{
    public class EmailAdapter : IEmailAdapter
    {
        public Message GetLatestMessage(string hostname, bool useSsl, int port, string username, string password)
        {
            using (var pop3Client = new Pop3Client())
            {
                pop3Client.Connect(hostname, port, useSsl);
                pop3Client.Authenticate(username, password, AuthenticationMethod.UsernameAndPassword);

                var messageCount = pop3Client.GetMessageCount();
                return pop3Client.GetMessage(messageCount);
            }
        }

        public void DeleteAllMessages(string hostname, bool useSsl, int port, string username, string password)
        {
            using (var pop3Client = new Pop3Client())
            {
                pop3Client.Connect(hostname, port, useSsl);
                //specify "recent:" to get messages that have already been opened
                pop3Client.Authenticate($"recent:{username}", password, AuthenticationMethod.UsernameAndPassword);
                pop3Client.DeleteAllMessages();
                pop3Client.Disconnect();
            }
        }
    }
}
