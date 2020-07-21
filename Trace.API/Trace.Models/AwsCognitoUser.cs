namespace Trace.Models
{
    public class AwsCognitoUser
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ConfirmationCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
    }
}
