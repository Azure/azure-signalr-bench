using System.Threading.Tasks;

namespace Portal
{
    public interface IUserService
    {
        Task<UserIdentity> Authenticate(string username, string password);
    }
}