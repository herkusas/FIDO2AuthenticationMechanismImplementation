namespace FidoAuth.V1.Commands
{
    public class MakeCredentialOptionsRequest
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string AttType { get; set; }
        public string AuthType { get; set; }
        public string UserVerification { get; set; }
        public bool RequireResidentKey { get; set; }
    }
}