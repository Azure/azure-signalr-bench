using System.IO;

namespace JenkinsScript
{
    public class StatisticsCollector
    {
        private string _parent = "";
        private string _root = "";
        private string _scenario = "";
        private string _logDirName = Constants.ResultDirectoryLog;
        private string _machineDirName = Constants.ResultDirectoryMachine;
        private string _configDirName = Constants.ResultDirectoryConfig;
        private string _resultDirName = Constants.ResultDirectoryResult;

        public string LogDirPath
        {
            get
            {
                return Path.Combine(_parent, _root, _scenario, _logDirName);
            }
        }

        public string ResultDirPath
        {
            get
            {
                return Path.Combine(_parent, _root, _scenario, _resultDirName);
            }
        }

        public string MachineDirPath
        {
            get
            {
                return Path.Combine(_parent, _root, _scenario, _machineDirName);
            }
        }

        public string ConfigDirPath
        {
            get
            {
                return Path.Combine(_parent, _root, _scenario, _configDirName);
            }
        }

        public StatisticsCollector(string parent, string root, string scenario)
        {
            _parent = parent;
            _root = root;
            _scenario = scenario;
        }

        private void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void PrepareDirectory()
        {
            CreateDirectory(LogDirPath);
            CreateDirectory(MachineDirPath);
            CreateDirectory(ConfigDirPath);
        }

        public void CopyJobConfig(string src)
        {
            File.Copy(src, Path.Combine(LogDirPath, Path.GetFileName(src)), true);
        }

        public void Collect(string hostname, string username, string password, int port, string remote, string local)
        {
            ShellHelper.ScpDirecotryRemoteToLocal(username, hostname, password, remote, local);
        }

        public void CollectConfig(string src)
        {
            CreateDirectory(ConfigDirPath);
            File.Copy(src, ConfigDirPath, true);
        }
    }

}