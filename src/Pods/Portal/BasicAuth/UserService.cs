using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Storage;

namespace Portal.BasicAuth
{
    public class UserService : IUserService
    {
        private readonly IPerfStorage _perfStorage;
        private readonly ClusterState _clusterState;


        public UserService(IPerfStorage perfStorage, ClusterState clusterState)
        {
            _perfStorage = perfStorage;
            _clusterState = clusterState;
        }

        public async Task<UserIdentity> Authenticate(string userName, string password)
        {
            var table = await _perfStorage.GetTableAsync<UserIdentity>(PerfConstants.TableNames.UserIdentity);
            var user = await table.GetFirstOrDefaultAsync(from row in table.Rows
                where row.PartitionKey == userName
                select row);
            if (user == null)
                return null;
            var key = GenerateKey(userName, password, user.Role);
            var decodedKey = DecodeSignature(user.Signature,
                _clusterState.AuthCert.GetRSAPrivateKey());
            return key == decodedKey ? user : null;
        }

        public static string CalculateSignature(string key, RSA rsa)
        {
            var signatureByte = rsa.Encrypt(Encoding.UTF8.GetBytes(key), RSAEncryptionPadding.OaepSHA256);
            var signature = Convert.ToBase64String(signatureByte, 0, signatureByte.Length);
            return signature;
        }

        public static string GenerateKey(string userName, string password, string role)
        {
            return userName + ":" + password + ":" + role;
        }

        private static string DecodeSignature(string signature, RSA rsa)
        {
            var bytes = Convert.FromBase64String(signature);
            var decodedBytes = rsa.Decrypt(bytes, RSAEncryptionPadding.OaepSHA256);
            var key = Encoding.UTF8.GetString(decodedBytes);
            return key;
        }
    }
}