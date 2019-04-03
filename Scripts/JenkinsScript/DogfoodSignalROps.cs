namespace JenkinsScript
{
    class DogfoodSignalROps
    {
        public static void RegisterDogfoodCloud(string extensionScriptsDir)
        {
            var cmd = $"cd {extensionScriptsDir}; . ./az_signalr_service.sh; register_signalr_service_dogfood; cd -";
            ShellHelper.Bash(cmd, handleRes : true);
        }

        public static void UnregisterDogfoodCloud(string extensionScriptsDir)
        {
            var cmd = $"cd {extensionScriptsDir}; . ./az_signalr_service.sh; unregister_signalr_service_dogfood; cd -";
            ShellHelper.Bash(cmd, handleRes: true);
        }

        public static string CreateDogfoodSignalRService(string extensionScriptsDir, string location, string resourceGroup, string serviceName, string sku, int unit)
        {
            var errCode = 0;
            var result = "";

            // Dogfood Azure login
            var cmd = $"cd {extensionScriptsDir}; . ./az_signalr_service.sh; az_login_ASRS_dogfood";
            Util.Log(cmd);
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
            if (errCode != 0)
            {
                Util.Log($"Fail to login to dogfood Azure");
                return null;
            }
            // Create resource group if it does not exist
            cmd = $"cd {extensionScriptsDir}; . ./az_signalr_service.sh; create_group_if_not_exist {resourceGroup} {location}";
            Util.Log(cmd);
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
            if (errCode != 0)
            {
                Util.Log($"Fail to create resource group");
                return null;
            }
            // Create SignalR service
            cmd = $"cd {extensionScriptsDir}; . ./az_signalr_service.sh; create_signalr_service {resourceGroup} {serviceName} {sku} {unit}";
            Util.Log(cmd);
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
            if (errCode != 0)
            {
                Util.Log($"Fail to create SignalR Service");
                return null;
            }
            // Check DNS ready
            cmd = $"cd {extensionScriptsDir}; . ./az_signalr_service.sh; check_signalr_service_dns {resourceGroup} {serviceName}";
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
            if (errCode != 0 || !result.Equals("0"))
            {
                Util.Log($"SignalR service DNS is not ready to use");
                return null;
            }
            // Get ConnectionString
            cmd = $"cd {extensionScriptsDir}; . ./az_signalr_service.sh; query_connection_string {serviceName} {resourceGroup}";
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
            if (errCode != 0)
            {
                Util.Log($"Fail to get connection string");
                return null;
            }
            return result;
        }

        public static bool DeleteDogfoodSignalRService(string extensionScriptsDir, string resourceGroup, string serviceName, bool deleteResourceGroup = true)
        {
            var errCode = 0;
            var result = "";
            bool rtn = false;
            var cmd = $"cd {extensionScriptsDir}; . ./az_signalr_service.sh; az_login_ASRS_dogfood";
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
            if (errCode != 0)
            {
                Util.Log($"Fail to login to dogfood Azure");
                return false;
            }
            cmd = $"cd {extensionScriptsDir}; . ./az_signalr_service.sh; delete_signalr_service {serviceName} {resourceGroup}";
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
            if (errCode != 0)
            {
                Util.Log($"Fail to delete SignalR Service");
                rtn = false;
            }
            else
            {
                rtn = true;
            }
            if (deleteResourceGroup)
            {
                cmd = $"cd {extensionScriptsDir}; . ./az_signalr_service.sh; delete_group {resourceGroup}";
                (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
                rtn = true;
            }
            return rtn;
        }
    }
}
