using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Compute.Fluent.VirtualMachine.Definition;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core.ResourceActions;

namespace JenkinsScript
{
    public class BenchmarkVmBuilder
    {
        private AgentConfig _agentConfig;
        private ServicePrincipalConfig _servicePrincipal;
        private IAzure _azure;
        private string _rndNum;

        private AzureCredentials _credentials;
        public BenchmarkVmBuilder(AgentConfig agentConfig)
        {
            try
            {
                LoginAzure();
            }
            catch (Exception ex)
            {
                Util.Log($"Login Azure Exception: {ex}");
            }

            _agentConfig = agentConfig;

            var rnd = new Random();
            _rndNum = Convert.ToString(rnd.Next(0, 100000) * rnd.Next(0, 10000));

        }

        public void CreateAllVmsInSameVnet()
        {

            var vnetName = _agentConfig.Prefix + _rndNum + "VNet";
            var subnetName = _agentConfig.Prefix + _rndNum + "Subnet";
            var resourceGroup = CreateResourceGroup(GroupName);
            var vnet = CreateVirtualNetwork(vnetName, Location, GroupName, subnetName);

            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(() => CreateAppServerVmCore(vnet)));
            tasks.Add(Task.Run(() => CreateBenchVmCore(vnet)));
            tasks.Add(Task.Run(() => CreateServiceVmCore(vnet)));
            tasks.Add(Task.Run(() => CreateAgentVmsCore(vnet)));
            Task.WhenAll(tasks).Wait();

            // debug: list all private ip
            var appPvtIp = GetPrivateIp(AppSvrNicBase + "0");
            var svcPvtIp = GetPrivateIp(ServiceNicBase + "0");
            var benchPvtIp = GetPrivateIp(BenchNicBase + "0");
            var slvPvtIps = new List<string>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++) slvPvtIps.Add(GetPrivateIp(NicBase + $"{i}"));

            Util.Log($"app pvt ip: {appPvtIp}");
            Util.Log($"bench pvt ip: {benchPvtIp}");
            Util.Log($"svc pvt ip: {svcPvtIp}");
            slvPvtIps.ForEach(ip => Util.Log($"slv pvt ip: {ip}"));

            // save all private ips
            var str = "";
            str += $"servicePrivateIp: {svcPvtIp}\n";
            str += $"appServerPrivateIp: {appPvtIp}\n";
            str += $"benchPrivateIp: {benchPvtIp}\n";
            str += $"masterPrivateIp: {slvPvtIps[0]}\n";
            str += "slavePrivateIp: ";
            for (var i = 1; i < slvPvtIps.Count; i++)
            {
                str += slvPvtIps[i];
                if (i < slvPvtIps.Count - 1) str += ";";
            }
            str += "\n";
            File.WriteAllText("privateIps.yaml", str);
        }

        public Task CreateAppServerVm()
        {
            return Task.Run(() => CreateAppServerVmCore());
        }

        public void CreateAppServerVmCore(INetwork vnet = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var resourceGroup = CreateResourceGroup(GroupName);
            if (vnet == null) vnet = CreateVirtualNetwork(AppSvrVnet, Location, GroupName, SubNet);
            var publicIp = CreatePublicIpAsync(AppSvrPublicIpBase, Location, GroupName, AppSvrPublicDnsBase).GetAwaiter().GetResult();
            var nsg = CreateNetworkSecurityGroupAsync(AppSvrNsgBase, Location, GroupName, _agentConfig.SshPort).GetAwaiter().GetResult();
            var nic = CreateNetworkInterfaceAsync(AppSvrNicBase, Location, GroupName, SubNet, vnet, publicIp, nsg).GetAwaiter().GetResult();
            var vmTemp = GenerateVmTemplateAsync(AppSvrVmNameBase, Location, GroupName, _agentConfig.ImageId, _agentConfig.User, _agentConfig.Password, _agentConfig.Ssh, AppSvrVmSize, nic).GetAwaiter().GetResult();
            vmTemp.Create();
            sw.Stop();
            Util.Log($"create vm time: {sw.Elapsed.TotalMinutes} min");
        }

        public void CreateBenchVmCore(INetwork vnet = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var resourceGroup = CreateResourceGroup(GroupName);
            if (vnet == null) vnet = CreateVirtualNetwork(AppSvrVnet, Location, GroupName, SubNet);
            var publicIp = CreatePublicIpAsync(BenchPublicIpBase, Location, GroupName, BenchPublicDnsBase).GetAwaiter().GetResult();
            var nsg = CreateNetworkSecurityGroupAsync(BenchNsgBase, Location, GroupName, _agentConfig.SshPort).GetAwaiter().GetResult();
            var nic = CreateNetworkInterfaceAsync(BenchNicBase, Location, GroupName, SubNet, vnet, publicIp, nsg).GetAwaiter().GetResult();
            var vmTemp = GenerateVmTemplateAsync(BenchVmNameBase, Location, GroupName, _agentConfig.ImageId, _agentConfig.User, _agentConfig.Password, _agentConfig.Ssh, BenchVmSize, nic).GetAwaiter().GetResult();
            vmTemp.Create();
            sw.Stop();
            Util.Log($"create vm time: {sw.Elapsed.TotalMinutes} min");
        }

        public void CreateServiceVmCore(INetwork vnet = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var resourceGroup = CreateResourceGroup(GroupName);
            if (vnet == null) vnet = CreateVirtualNetwork(AppSvrVnet, Location, GroupName, SubNet);
            var publicIp = CreatePublicIpAsync(ServicePublicIpBase, Location, GroupName, ServicePublicDnsBase).GetAwaiter().GetResult();
            var nsg = CreateNetworkSecurityGroupAsync(ServiceNsgBase, Location, GroupName, _agentConfig.SshPort).GetAwaiter().GetResult();
            var nic = CreateNetworkInterfaceAsync(ServiceNicBase, Location, GroupName, SubNet, vnet, publicIp, nsg).GetAwaiter().GetResult();
            var vmTemp = GenerateVmTemplateAsync(ServiceVmNameBase, Location, GroupName, _agentConfig.ImageId, _agentConfig.User, _agentConfig.Password, _agentConfig.Ssh, ServiceVmSize, nic).GetAwaiter().GetResult();
            vmTemp.Create();
            sw.Stop();
            Util.Log($"create vm time: {sw.Elapsed.TotalMinutes} min");
        }

        public Task CreateAgentVms()
        {
            return Task.Run(() => CreateAgentVmsCore());
        }

        public void CreateAgentVmsCore(INetwork vNet = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            var resourceGroup = CreateResourceGroup(GroupName);
            var avSet = CreateAvailabilitySet(AVSet, Location, GroupName);
            if (vNet == null) vNet = CreateVirtualNetwork(VNet, Location, GroupName, SubNet);

            List<ICreatable<IVirtualMachine>> creatableVirtualMachines = new List<ICreatable<IVirtualMachine>>();

            var publicIpTasks = new List<Task<IPublicIPAddress>>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                publicIpTasks.Add(CreatePublicIpAsync(PublicIpBase, Location, GroupName, PublicDnsBase, i));
            }
            var publicIps = Task.WhenAll(publicIpTasks).GetAwaiter().GetResult();

            var nsgTasks = new List<Task<INetworkSecurityGroup>>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                nsgTasks.Add(CreateNetworkSecurityGroupAsync(NsgBase, Location, GroupName, _agentConfig.SshPort, i));
            }
            var nsgs = Task.WhenAll(nsgTasks).GetAwaiter().GetResult();

            var nicTasks = new List<Task<INetworkInterface>>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                nicTasks.Add(CreateNetworkInterfaceAsync(NicBase, Location, GroupName, SubNet, vNet, publicIps[i], nsgs[i], i));
            }
            var nics = Task.WhenAll(nicTasks).GetAwaiter().GetResult();

            var vmTasks = new List<Task<IWithCreate>>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                vmTasks.Add(GenerateVmTemplateAsync(VmNameBase, Location, GroupName, _agentConfig.ImageId, _agentConfig.User, _agentConfig.Password, _agentConfig.Ssh, SlaveVmSize, nics[i], avSet, i));
            }

            var vms = Task.WhenAll(vmTasks).GetAwaiter().GetResult();
            creatableVirtualMachines.AddRange(vms);

            Console.WriteLine($"creating vms");
            var virtualMachines = _azure.VirtualMachines.Create(creatableVirtualMachines.ToArray());

            sw.Stop();
            Util.Log($"create vm time: {sw.Elapsed.TotalMinutes} min");
        }

        public void LoginAzure()
        {
            var content = AzureBlobReader.ReadBlob("ServicePrincipalFileName");
            _servicePrincipal = AzureBlobReader.ParseYaml<ServicePrincipalConfig>(content);

            // auth
            _credentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(_servicePrincipal.ClientId, _servicePrincipal.ClientSecret, _servicePrincipal.TenantId, AzureEnvironment.AzureGlobalCloud);

            _azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                .Authenticate(_credentials)
                .WithSubscription(_servicePrincipal.Subscription);
        }

        public IResourceGroup CreateResourceGroup(string groupName)
        {
            Console.WriteLine($"Creating resource group: {groupName}");
            if (_azure.ResourceGroups.Contain(groupName))
            {
                Console.WriteLine($"Resource group {groupName} existed");
                return _azure.ResourceGroups.GetByName(groupName);
                // return null;
            }

            return _azure.ResourceGroups.Define(groupName)
                .WithRegion(Location)
                .Create();
        }

        public IAvailabilitySet CreateAvailabilitySet(string name, Region location, string groupName)
        {
            return _azure.AvailabilitySets.Define(name)
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithSku(AvailabilitySetSkuTypes.Managed)
                .Create();
        }

        public INetwork CreateVirtualNetwork(string name, Region location, string groupName, string subNetName)
        {
            return _azure.Networks.Define(name)
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithAddressSpace("10.0.0.0/16")
                .WithSubnet(subNetName, "10.0.0.0/24")
                .Create();
        }

        public Task<IPublicIPAddress> CreatePublicIpAsync(string publicIpBase, Region location, string groupName, string publicDnsBase, int i = 0)
        {
            return _azure.PublicIPAddresses.Define(publicIpBase + Convert.ToString(i))
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithLeafDomainLabel(publicDnsBase + Convert.ToString(i))
                .WithDynamicIP()
                .CreateAsync();
        }

        public Task<INetworkSecurityGroup> CreateNetworkSecurityGroupAsync(string nsgBase, Region location, string groupName, int sshPort, int i = 0)
        {
            Console.WriteLine($"Creating {i}th network security group in resource group {groupName}");
            return _azure.NetworkSecurityGroups.Define(nsgBase + Convert.ToString(i))
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .DefineRule("SSH-PORT")
                .AllowInbound()
                .FromAnyAddress()
                .FromAnyPort()
                .ToAnyAddress()
                .ToPort(22)
                .WithAnyProtocol()
                .WithPriority(100)
                .Attach()
                .DefineRule("NEW-SSH-PORT")
                .AllowInbound()
                .FromAnyAddress()
                .FromAnyPort()
                .ToAnyAddress()
                .ToPort(sshPort)
                .WithAnyProtocol()
                .WithPriority(101)
                .Attach()
                .DefineRule("BENCHMARK-PORT")
                .AllowInbound()
                .FromAnyAddress()
                .FromAnyPort()
                .ToAnyAddress()
                .ToPort(7000)
                .WithAnyProtocol()
                .WithPriority(102)
                .Attach()
                .DefineRule("RPC")
                .AllowInbound()
                .FromAnyAddress()
                .FromAnyPort()
                .ToAnyAddress()
                .ToPort(5555)
                .WithAnyProtocol()
                .WithPriority(103)
                .Attach()
                .DefineRule("AppServer")
                .AllowInbound()
                .FromAnyAddress()
                .FromAnyPort()
                .ToAnyAddress()
                .ToPort(5050)
                .WithAnyProtocol()
                .WithPriority(104)
                .Attach()
                .DefineRule("Report")
                .AllowInbound()
                .FromAnyAddress()
                .FromAnyPort()
                .ToAnyAddress()
                .ToPort(3000)
                .WithAnyProtocol()
                .WithPriority(105)
                .Attach()
                .DefineRule("JENKINS")
                .AllowInbound()
                .FromAnyAddress()
                .FromAnyPort()
                .ToAnyAddress()
                .ToPort(8080)
                .WithAnyProtocol()
                .WithPriority(106)
                .Attach()
                .CreateAsync();
        }

        public Task<INetworkInterface> CreateNetworkInterfaceAsync(string nicBase, Region location, string groupName, string subNet, INetwork network, IPublicIPAddress publicIPAddress, INetworkSecurityGroup nsg, int i = 0)
        {
            Console.WriteLine($"Creating {i}th network interface in resource group {groupName}");
            return _azure.NetworkInterfaces.Define(nicBase + Convert.ToString(i))
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithExistingPrimaryNetwork(network)
                .WithSubnet(subNet)
                .WithPrimaryPrivateIPAddressDynamic()
                .WithExistingPrimaryPublicIPAddress(publicIPAddress)
                .WithExistingNetworkSecurityGroup(nsg)
                .CreateAsync();
        }

        public Task<IWithCreate> GenerateVmTemplateAsync(string vmNameBase, Region location, string groupName, string imageId, string user, string password, string ssh, VirtualMachineSizeTypes vmSize, INetworkInterface networkInterface, IAvailabilitySet availabilitySet = null, int i = 0)
        {
            Console.WriteLine($"Create Vm Teamlate: {vmNameBase + Convert.ToString(i)}");
            var option = _azure.VirtualMachines.Define(vmNameBase + Convert.ToString(i))
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithExistingPrimaryNetworkInterface(networkInterface)
                .WithLinuxCustomImage(imageId)
                // .WithPopularLinuxImage(KnownLinuxVirtualMachineImage.UbuntuServer16_04_Lts)
                .WithRootUsername(user)
                .WithRootPassword(password)
                // .WithSsh(ssh)
                .WithComputerName(vmNameBase + Convert.ToString(i));

            IWithCreate creator = null;
            if (availabilitySet != null)
                creator = option.WithExistingAvailabilitySet(availabilitySet);
            else
                creator = option;
            var vm = creator.WithSize(vmSize);
            return Task.FromResult(vm);
        }

        public Task ModifyLimitAsync(string domain, string user, string password, int i = 0)
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"modify limits: {domain}");

                var errCode = 0;
                var res = "";
                var cmd = "";

                //var domain = SlaveDomainName(i);

                cmd = $"echo '{password}' | sudo -S cp /etc/security/limits.conf /etc/security/limits.conf.bak";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes : true, retry : 5);

                cmd = $"cp /etc/security/limits.conf ~/limits.conf";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes : true, retry : 5);

                cmd = $"echo 'wanl    soft    nofile  655350\n' >> ~/limits.conf";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes : true, retry : 5);

                cmd = $"echo '{password}' | sudo -S mv ~/limits.conf /etc/security/limits.conf";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes : true, retry : 5);

            });

        }

        public Task InstallDotnetAsync(string domain, string user, string password, int i = 0)
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"install dotnet: {domain}");
                var errCode = 0;
                var res = "";
                var cmd = "";
                var port = 22;

                cmd = $"wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, port, password, cmd, handleRes : true, retry : 5);

                cmd = $"sudo dpkg -i packages-microsoft-prod.deb";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, port, password, cmd, handleRes : true, retry : 5);

                cmd = $"sudo apt-get -y install apt-transport-https";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, port, password, cmd, handleRes : true, retry : 5);

                cmd = $"sudo apt-get update";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, port, password, cmd, handleRes : true, retry : 5);

                cmd = $"sudo apt-get -y install dotnet-sdk-2.1";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, port, password, cmd, handleRes : true, retry : 5);
            });

        }

        public Task ModifySshdAndRestart(string domain, string user, string password, int i = 0)
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"modify sshd_config: {domain}");

                var errCode = 0;
                var res = "";
                var cmd = "";

                cmd = $"echo '{password}' | sudo -S cp   /etc/ssh/sshd_config  /etc/ssh/sshd_config.bak";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes : true, retry : 5);

                cmd = $"echo '{password}' | sudo -S sed -i 's/22/22222/g' /etc/ssh/sshd_config";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes : true, retry : 5);

                cmd = $"echo '{password}' | sudo -S service sshd restart";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes : true, retry : 5);

            });
        }

        public string SlaveDomainName(int i)
        {
            return PublicDnsBase + Convert.ToString(i) + "." + _agentConfig.Location.ToLower() + ".cloudapp.azure.com";
        }

        public string AppSvrDomainName(int i = 0)
        {
            return AppSvrPublicDnsBase + Convert.ToString(i) + "." + _agentConfig.Location.ToLower() + ".cloudapp.azure.com";

        }
        public string AppSvrVnet
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "AppSvrVNet";
            }
        }

        public string VNet
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "VNet";
            }
        }

        public string AppSvrVmNameBase
        {
            get
            {
                return _agentConfig.Prefix.ToLower() + _rndNum + "appsvrvm";
            }

        }
        public string ServiceVmNameBase
        {
            get
            {
                return _agentConfig.Prefix.ToLower() + _rndNum + "serivcevm";
            }

        }

        public string BenchVmNameBase
        {
            get
            {
                return _agentConfig.Prefix.ToLower() + _rndNum + "benchvm";
            }

        }

        public string VmNameBase
        {
            get
            {
                return _agentConfig.Prefix.ToLower() + _rndNum + "vm";
            }
        }

        public string AppSvrSubNet
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "AppSvrSubnet";
            }
        }

        public string SubNet
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "Subnet";
            }
        }

        public string AppSvrNicBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "AppSvrNIC";
            }
        }

        public string ServiceNicBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "ServiceNIC";
            }
        }

        public string BenchNicBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "BenchNIC";
            }
        }

        public string NicBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "NIC";
            }
        }

        public string AppSvrNsgBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "AppSvrNSG";
            }
        }
        public string ServiceNsgBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "ServiceNSG";
            }
        }

        public string BenchNsgBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "BenchNSG";
            }
        }

        public string NsgBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "NSG";
            }
        }

        public string PublicIpBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "PublicIP";
            }
        }

        public string AppSvrPublicIpBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "AppSvrPublicIP";
            }
        }

        public string ServicePublicIpBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "ServicePublicIP";
            }
        }

        public string BenchPublicIpBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "BenchPublicIP";
            }
        }

        public string PublicDnsBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "DNS";
            }
        }

        public string AppSvrPublicDnsBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "AppSvrDNS";
            }
        }

        public string ServicePublicDnsBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "ServiceDNS";
            }
        }

        public string BenchPublicDnsBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "BenchDNS";
            }
        }

        public string AVSet
        {
            get
            {
                return _agentConfig.Prefix + "AVSet";
            }
        }

        public Region Location
        {
            get
            {
                Region location = null;
                switch (_agentConfig.Location.ToLower())
                {
                    case "useast":
                        location = Region.USEast;
                        break;
                    case "westus":
                        location = Region.USWest;
                        break;
                    case "southeastasia":
                        location = Region.AsiaSouthEast;
                        break;
                    default:
                        location = Region.AsiaSouthEast;
                        break;
                }

                return location;
            }
        }

        public VirtualMachineSizeTypes SlaveVmSize
        {
            get
            {
                return GetVmSize(_agentConfig.SlaveVmSize);
            }
        }

        public VirtualMachineSizeTypes AppSvrVmSize
        {
            get
            {
                return GetVmSize(_agentConfig.AppSvrVmSize);
            }

        }

        public VirtualMachineSizeTypes ServiceVmSize
        {
            get
            {
                return GetVmSize(_agentConfig.SvcVmSize);
            }

        }

        public VirtualMachineSizeTypes BenchVmSize
        {
            get
            {
                return GetVmSize(_agentConfig.BenchVmSize);
            }

        }

        private VirtualMachineSizeTypes GetVmSize(string vmSizeName)
        {
            switch (vmSizeName)
            {
                case "StandardDS1":
                    return VirtualMachineSizeTypes.StandardDS1;
                case "StandardDS4":
                    return VirtualMachineSizeTypes.StandardDS4;
                case "StandardDS4V2":
                    return VirtualMachineSizeTypes.StandardDS4V2;
                case "StandardD3V2":
                    return VirtualMachineSizeTypes.StandardD3V2;
                case "StandardD2V2":
                    return VirtualMachineSizeTypes.StandardD2V2;
                case "StandardF2s":
                    return VirtualMachineSizeTypes.StandardF2s;
                default:
                    return VirtualMachineSizeTypes.StandardDS1;
            }
        }

        public string AppSvrGroupName
        {
            get
            {
                return _agentConfig.Prefix + "AppSvrResourceGroup" + _rndNum;

            }
        }
        public string GroupName
        {
            get
            {
                return _agentConfig.Prefix + "ResourceGroup" + _rndNum;

            }
        }

        public string GetPrivateIp(string nicName)
        {
            using(var client = new NetworkManagementClient(_credentials))
            {
                client.SubscriptionId = _servicePrincipal.Subscription;
                var network = NetworkInterfacesOperationsExtensions.GetAsync(client.NetworkInterfaces, GroupName, nicName).GetAwaiter().GetResult();
                string ip = network.IpConfigurations[0].PrivateIPAddress;
                return ip;
            }
        }

    }
}