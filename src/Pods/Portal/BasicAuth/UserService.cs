using System.Linq;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Storage;

namespace Portal
{
    public class UserService :IUserService
    {
        private IPerfStorage _perfStorage;

        public UserService(IPerfStorage perfStorage)
        {
            _perfStorage = perfStorage;
        }
        
        public async Task<UserIdentity> Authenticate(string userName, string password)
        {
            var table = await _perfStorage.GetTableAsync<UserIdentity>(PerfConstants.TableNames.UserIdentity);
            var user = await table.GetFirstOrDefaultAsync(from row in table.Rows
                where row.PartitionKey == userName
                select row);
            return user?.Password == password ? user : null;
        }
    }
}