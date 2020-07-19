namespace Trace.Models
{
    public class AwsCognitoUser
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ReEnterPassword { get; set; }
        public string ConfirmationCode { get; set; }
    }
}
