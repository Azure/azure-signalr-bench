using FluentFTP;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DeployWebApp
{
    public class FtpClientConnection
    {
        private string _host;
        private string _username;
        private string _password;

        public FtpClientConnection(
            string host,
            string username,
            string password)
        {
            _host = host;
            _username = username;
            _password = password;
        }

        public async Task DownloadFile(
            string remoteFilePrefix,
            string remoteFilePostfix,
            string remoteFolder,
            string localFile)
        {
            var credentials = new NetworkCredential(_username, _password);

            // create an FTP client
            var client = new FtpClient(_host);
            client.Credentials = credentials;
            try
            {
                using (var c = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                {
                    // begin connecting to the server
                    await client.ConnectAsync(c.Token);

                    if (client.IsConnected)
                    {
                        var postfix = 0;
                        foreach (FtpListItem item in client.GetListing(remoteFolder))
                        {
                            // if this is a file
                            if (item.Type == FtpFileSystemObjectType.File &&
                                item.Name.StartsWith(remoteFilePrefix) &&
                                item.Name.EndsWith(remoteFilePostfix))
                            {
                                var localFileName = $"{localFile}{postfix}.log";
                                // get the file size
                                long size = client.GetFileSize(item.FullName);
                                Console.WriteLine($"file {item.FullName} size: {size}");
                                await client.DownloadFileAsync(localFileName, item.FullName, FtpLocalExists.Overwrite);
                                postfix++;
                            }
                            else if (item.Type == FtpFileSystemObjectType.Directory)
                            {
                                Console.WriteLine($"dir: {item.FullName}");
                            }
                        }
                        client.Disconnect();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error {e.Message} {e.InnerException}");
            }
        }
    }
}
