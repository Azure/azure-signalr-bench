using Microsoft.Azure.Cosmos.Table;

namespace Portal.BasicAuth
{
    public class UserIdentity : TableEntity
    {
        public string Role { get; set; }
        public string Signature { get; set; }
    }
}