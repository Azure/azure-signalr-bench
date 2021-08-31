using System.Threading.Tasks;

namespace Portal.BasicAuth
{
    public interface IUserService
    {
        Task<UserIdentity> Authenticate(string username, string password);
    }
}