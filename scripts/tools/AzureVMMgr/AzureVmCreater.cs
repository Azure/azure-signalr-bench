using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Compute.Fluent.VirtualMachine.Definition;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
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
        public BenchmarkVmBuilder(AgentConfig agentConfig, string servicePrincipal, bool disableRandomSuffix = false)
        {
            try
            {
                LoginAzure(servicePrincipal);
            }
            catch (Exception ex)
            {
                Util.Log($"Login Azure Exception: {ex}");
            }

            _agentConfig = agentConfig;

            var rnd = new Random();
            if (disableRandomSuffix) _rndNum = "";
            else _rndNum = Convert.ToString(rnd.Next(0, 100000) * rnd.Next(0, 10000));

        }

        public void CreateBenchServer()
        {
            var vnetName = _agentConfig.Prefix + _rndNum + "VNet";
            var subnetName = _agentConfig.Prefix + _rndNum + "Subnet";
            var resourceGroup = CreateResourceGroup(BenchGroupName);
            var vnet = CreateVirtualNetwork(vnetName, Location, BenchGroupName, subnetName);
            CreateBenchServerCore(0, vnet);
        }

        private bool CheckAllSSHPort(int appSvrVmCount, int svcVmCount, TimeSpan timeSpan)
        {
            var portCheckTaskList = new List<Task>();
            for (var i = 0; i < _agentConfig.AgentVmCount; i++)
            {
                var privateIp = GetPrivateIp(NicBase + $"{i}");
                portCheckTaskList.Add(Task.Run(() => WaitPortOpen(privateIp, 22, timeSpan)));
            }
            // service
            for (var i = 0; i < svcVmCount; i++)
            {
                var privateIp = GetPrivateIp(ServiceNicBase + $"{i}");
                portCheckTaskList.Add(Task.Run(() => WaitPortOpen(privateIp, 22, timeSpan)));
            }

            // app server
            for (var i = 0; i < appSvrVmCount; i++)
            {
                var privateIp = GetPrivateIp(AppSvrNicBase + $"{i}");
                portCheckTaskList.Add(Task.Run(() => WaitPortOpen(privateIp, 22, timeSpan)));
            }

            if (Task.WaitAll(portCheckTaskList.ToArray(), TimeSpan.FromSeconds(300)))
            {
                Util.Log("All ports are ready");
                return true;
            }
            else
            {
                Util.Log("Not all ports are ready");
                return false;
            }
        }

        public void CreateAllVmsInSameVnet(string groupName, string vnetName, string subnetName, int appSvrVmCount, int svcVmCount)
        {
            (var vnet,
                var subnet) = GetVnetSubnet(groupName, vnetName, subnetName);

            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(() => CreateAppServerVmsCore(appSvrVmCount, vnet, subnet)));
            tasks.Add(Task.Run(() => CreateServiceVmsCore(svcVmCount, vnet, subnet)));
            tasks.Add(Task.Run(() => CreateAgentVmsCore(vnet, subnet)));
            Task.WhenAll(tasks).Wait();

            var timeSpan = TimeSpan.FromSeconds(300);
            if (!CheckAllSSHPort(appSvrVmCount, svcVmCount, timeSpan))
            {
                Util.Log($"Fail to access VMs after {timeSpan.Seconds} seconds");
            }

            // debug: list all private ip
            var slvPvtIps = new List<string>();
            for (var i = 0; i < _agentConfig.AgentVmCount; i++)
            {
                slvPvtIps.Add(GetPrivateIp(NicBase + $"{i}"));
            }
            slvPvtIps.ForEach(ip => Util.Log($"slv pvt ip: {ip}"));

            // save all private ips
            var str = "";

            // service
            str += $"servicePrivateIp: ";
            for (var i = 0; i < svcVmCount; i++)
            {
                var privateIp = GetPrivateIp(ServiceNicBase + $"{i}");
                str += $"{privateIp}";
                if (i < svcVmCount - 1) str += ";";
            }
            str += "\n";

            // app server
            str += $"appServerPrivateIp: ";
            for (var i = 0; i < appSvrVmCount; i++)
            {
                var privateIp = GetPrivateIp(AppSvrNicBase + $"{i}");
                str += $"{privateIp}";
                if (i < appSvrVmCount - 1) str += ";";
            }
            str += "\n";

            // agent private IP
            if (slvPvtIps.Count > 0)
            {
                str += $"masterPrivateIp: {slvPvtIps[0]}\n";
                str += "agentPrivateIp: ";
                for (var i = 0; i < slvPvtIps.Count; i++)
                {
                    str += slvPvtIps[i];
                    if (i < slvPvtIps.Count - 1) str += ";";
                }
                str += "\n";
            }
            var privateIpFile = Environment.GetEnvironmentVariable("PrivateIps");
            if (string.IsNullOrEmpty(privateIpFile))
            {
                privateIpFile = "privateIps.yaml";
            }
            File.WriteAllText(privateIpFile, str);

            // save public IP of service and appserver
            str = "";

            str += $"servicePublicIp: ";
            for (var i = 0; i < svcVmCount; i++)
            {
                str += $"{ServicePublicDnsBase}{i}.{Location}.cloudapp.azure.com";
                if (i < svcVmCount - 1) str += ";";
            }
            str += "\n";

            str += $"appServerPublicIp: ";
            for (var i = 0; i < appSvrVmCount; i++)
            {
                str += $"{AppSvrPublicDnsBase}{i}.{Location}.cloudapp.azure.com";
                if (i < appSvrVmCount - 1) str += ";";
            }
            str += "\n";
            var publicIpFile = Environment.GetEnvironmentVariable("PublicIps");
            if (string.IsNullOrEmpty(publicIpFile))
            {
                publicIpFile = "publicIps.yaml";
            }
            File.WriteAllText(publicIpFile, str);
        }

        public Task CreateAppServerVm()
        {
            return Task.Run(() => CreateAppServerVmCore());
        }

        public void CreateAppServerVmsCore(int count = 1, INetwork vnet = null, ISubnet subnet = null)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < count; i++)
            {
                var ind = i;
                tasks.Add(Task.Run(() => CreateAppServerVmCore(ind, vnet, subnet)));
            }
            Task.WhenAll(tasks).Wait();
        }
        public void CreateAppServerVmCore(int i = 0, INetwork vnet = null, ISubnet subnet = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var resourceGroup = CreateResourceGroup(GroupName);
            if (vnet == null) vnet = CreateVirtualNetwork(AppSvrVnet, Location, GroupName, SubNet);

            var publicIp = CreatePublicIpAsync(AppSvrPublicIpBase, Location, GroupName, AppSvrPublicDnsBase, i).GetAwaiter().GetResult();
            var nsg = CreateNetworkSecurityGroupAsync(AppSvrNsgBase, Location, GroupName, _agentConfig.SshPort, i).GetAwaiter().GetResult();
            var nic = CreateNetworkInterfaceAsync(AppSvrNicBase, Location, GroupName, subnet == null? SubNet : subnet.Name, vnet, publicIp, nsg, i).GetAwaiter().GetResult();
            var vmTemp = GenerateVmTemplateAsync(AppSvrVmNameBase, Location, GroupName, _agentConfig.ImageId, _agentConfig.User, _agentConfig.Password, _agentConfig.Ssh, AppSvrVmSize, nic, null, i).GetAwaiter().GetResult();
            vmTemp.Create();
            sw.Stop();
            Util.Log($"create vm time: {sw.Elapsed.TotalMinutes} min");
        }

        public void CreateBenchServerCore(int i = 0, INetwork vnet = null, ISubnet subnet = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var resourceGroup = CreateResourceGroup(BenchGroupName);
            if (vnet == null) vnet = CreateVirtualNetwork(AppSvrVnet, Location, BenchGroupName, SubNet);

            var publicIp = CreatePublicIpAsync(BenchPublicIpBase, Location, BenchGroupName, BenchPublicDnsBase, i).GetAwaiter().GetResult();
            var nsg = CreateNetworkSecurityGroupAsync(BenchNsgBase, Location, BenchGroupName, _agentConfig.SshPort, i).GetAwaiter().GetResult();
            var nic = CreateNetworkInterfaceAsync(BenchNicBase, Location, BenchGroupName, subnet == null? SubNet : subnet.Name, vnet, publicIp, nsg, i).GetAwaiter().GetResult();
            var vmTemp = GenerateVmTemplateAsync(BenchVmNameBase, Location, BenchGroupName, _agentConfig.ImageId, _agentConfig.User, _agentConfig.Password, _agentConfig.Ssh, BenchVmSize, nic, null, i).GetAwaiter().GetResult();
            vmTemp.Create();
            sw.Stop();
            Util.Log($"create vm time: {sw.Elapsed.TotalMinutes} min");
        }

        public void CreateServiceVmsCore(int count = 1, INetwork vnet = null, ISubnet subnet = null)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < count; i++)
            {
                var ind = i;
                tasks.Add(Task.Run(() => CreateServiceVmCore(ind, vnet, subnet)));
            }
            Task.WhenAll(tasks).Wait();
        }
        public void CreateServiceVmCore(int i = 0, INetwork vnet = null, ISubnet subnet = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var resourceGroup = CreateResourceGroup(GroupName);
            if (vnet == null) vnet = CreateVirtualNetwork(AppSvrVnet, Location, GroupName, SubNet);
            var publicIp = CreatePublicIpAsync(ServicePublicIpBase, Location, GroupName, ServicePublicDnsBase, i).GetAwaiter().GetResult();
            var nsg = CreateNetworkSecurityGroupAsync(ServiceNsgBase, Location, GroupName, _agentConfig.SshPort, i).GetAwaiter().GetResult();
            var nic = CreateNetworkInterfaceAsync(ServiceNicBase, Location, GroupName, subnet == null? SubNet : subnet.Name, vnet, publicIp, nsg, i).GetAwaiter().GetResult();
            var vmTemp = GenerateVmTemplateAsync(ServiceVmNameBase, Location, GroupName, _agentConfig.ImageId, _agentConfig.User, _agentConfig.Password, _agentConfig.Ssh, ServiceVmSize, nic, null, i).GetAwaiter().GetResult();
            vmTemp.Create();
            sw.Stop();
            Util.Log($"create vm time: {sw.Elapsed.TotalMinutes} min");
        }

        public Task CreateAgentVms()
        {
            return Task.Run(() => CreateAgentVmsCore());
        }

        public void CreateAgentVmsCore(INetwork vNet = null, ISubnet subnet = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            var resourceGroup = CreateResourceGroup(GroupName);
            var avSet = CreateAvailabilitySet(AVSet, Location, GroupName);
            if (vNet == null) vNet = CreateVirtualNetwork(VNet, Location, GroupName, SubNet);

            List<ICreatable<IVirtualMachine>> creatableVirtualMachines = new List<ICreatable<IVirtualMachine>>();

            /*
            var publicIpTasks = CreatePublicIPAddrListWithRetry(_azure, _agentConfig.AgentVmCount,
                PublicIpBase, Location, GroupName, PublicDnsBase).GetAwaiter().GetResult();
            */
            var publicIpTasks = new List<Task<IPublicIPAddress>>();
            for (var i = 0; i < _agentConfig.AgentVmCount; i++)
            {
                publicIpTasks.Add(CreatePublicIpAsync(PublicIpBase, Location, GroupName, PublicDnsBase, i));
            }
            var publicIps = Task.WhenAll(publicIpTasks).GetAwaiter().GetResult();

            var nsgTasks = new List<Task<INetworkSecurityGroup>>();
            for (var i = 0; i < _agentConfig.AgentVmCount; i++)
            {
                nsgTasks.Add(CreateNetworkSecurityGroupAsync(NsgBase, Location, GroupName, _agentConfig.SshPort, i));
            }
            var nsgs = Task.WhenAll(nsgTasks).GetAwaiter().GetResult();

            var nicTasks = new List<Task<INetworkInterface>>();
            for (var i = 0; i < _agentConfig.AgentVmCount; i++)
            {
                nicTasks.Add(CreateNetworkInterfaceAsync(NicBase, Location, GroupName,
                    subnet == null? SubNet : subnet.Name, vNet, publicIps[i], nsgs[i], i));
            }
            var nics = Task.WhenAll(nicTasks).GetAwaiter().GetResult();

            var vmTasks = new List<Task<IWithCreate>>();
            for (var i = 0; i < _agentConfig.AgentVmCount; i++)
            {
                vmTasks.Add(GenerateVmTemplateAsync(VmNameBase, Location, GroupName, _agentConfig.ImageId,
                    _agentConfig.User, _agentConfig.Password, _agentConfig.Ssh, AgentVmSize, nics[i], avSet, i));
            }

            var vms = Task.WhenAll(vmTasks).GetAwaiter().GetResult();
            creatableVirtualMachines.AddRange(vms);

            Console.WriteLine($"creating vms");
            var virtualMachines = _azure.VirtualMachines.Create(creatableVirtualMachines.ToArray());

            sw.Stop();
            Util.Log($"create vm time: {sw.Elapsed.TotalMinutes} min");
        }

        public void LoginAzure(string servicePrincipal)
        {
            // var content = AzureBlobReader.ReadBlob("ServicePrincipalFileName");
            // _servicePrincipal = AzureBlobReader.ParseYaml<ServicePrincipalConfig>(content);

            var configLoader = new ConfigLoader();
            _servicePrincipal = configLoader.Load<ServicePrincipalConfig>(servicePrincipal);

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
                .WithAddressSpace("10.220.0.0/24")
                .WithSubnet(subNetName, "10.220.0.0/24")
                .Create();
        }

        public Task<IPublicIPAddress> CreatePublicIpAsync(
            string publicIpBase, Region location, string groupName, string publicDnsBase, int i = 0)
        {
            return Task.Run(async() =>
            {
                try
                {
                    var newIp = await _azure.PublicIPAddresses.Define(publicIpBase + Convert.ToString(i))
                        .WithRegion(location)
                        .WithExistingResourceGroup(groupName)
                        .WithLeafDomainLabel(publicDnsBase + Convert.ToString(i))
                        .WithDynamicIP()
                        .CreateAsync();
                    return newIp;
                }
                catch (System.Exception e)
                {
                    await Task.Delay(2000);
                    Util.Log($"Fail to create PubIP {e.Message}");
                    throw;
                }
            });
        }

        private static async Task<List<Task<IPublicIPAddress>>> CreatePublicIPAddrListWithRetry(
            IAzure azure, int count, string publicIpBase, Region region,
            string resourceGroupName, string publicDnsBase, int maxTry = 3)
        {
            var publicIpTaskList = new List<Task<IPublicIPAddress>>();
            var j = 0;
            var i = 0;
            while (j < maxTry)
            {
                try
                {
                    for (i = 0; i < count; i++)
                    {
                        // create public ip
                        var publicIPAddress = azure.PublicIPAddresses.Define(publicIpBase + Convert.ToString(i) + "PubIP")
                            .WithRegion(region)
                            .WithExistingResourceGroup(resourceGroupName)
                            .WithLeafDomainLabel(publicDnsBase + Convert.ToString(i))
                            .WithDynamicIP()
                            .CreateAsync();
                        publicIpTaskList.Add(publicIPAddress);
                    }
                    await Task.WhenAll(publicIpTaskList.ToArray());
                    return publicIpTaskList;
                }
                catch (Exception e)
                {
                    Util.Log(e.ToString());
                    await Task.Delay(2000);
                    publicIpTaskList.Clear();

                    var allPubIPs = azure.PublicIPAddresses.ListByResourceGroupAsync(resourceGroupName);
                    await allPubIPs;
                    var ids = new List<string>();
                    var enumerator = allPubIPs.Result.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        ids.Add(enumerator.Current.Id);
                    }
                    await azure.PublicIPAddresses.DeleteByIdsAsync(ids);
                    if (j + 1 < maxTry)
                    {
                        Util.Log($"Fail to create public IP for {e.Message} and will retry");
                    }
                    else
                    {
                        Util.Log($"Fail to create public IP for {e.Message} and retry has reached max limit, will return with failure");
                    }
                }
                j++;
            }
            return null;
        }

        public Task<INetworkSecurityGroup> CreateNetworkSecurityGroupAsync(string nsgBase, Region location, string groupName, int sshPort, int i = 0)
        {

            return Task.Run(async() =>
            {
                var azureRegionIP =
                "167.220.148.0/23,131.107.147.0/24,131.107.159.0/24,131.107.160.0/24,131.107.174.0/24,167.220.24.0/24,167.220.26.0/24,167.220.238.0/27,167.220.238.128/27,167.220.238.192/27,167.220.238.64/27,167.220.232.0/23,167.220.255.0/25,167.220.242.0/27,167.220.242.128/27,167.220.242.192/27,167.220.242.64/27,94.245.87.0/24,167.220.196.0/23,194.69.104.0/25,191.234.97.0/26,167.220.0.0/23,167.220.2.0/24,207.68.190.32/27,13.106.78.32/27,10.254.32.0/20,10.97.136.0/22,13.106.174.32/27,13.106.4.96/27,168.61.37.236";
                var allowedIpRange = azureRegionIP.Split(',');
                while (true)
                {
                    try
                    {
                        Console.WriteLine($"Creating {i}th network security group in resource group {groupName}");
                        var newNsg = await _azure.NetworkSecurityGroups.Define(nsgBase + Convert.ToString(i))
                            .WithRegion(location)
                            .WithExistingResourceGroup(groupName)
                            .DefineRule("SSH-PORT")
                            .AllowInbound()
                            .FromAddresses(allowedIpRange)
                            .FromAnyPort()
                            .ToAnyAddress()
                            .ToPort(22)
                            .WithAnyProtocol()
                            .WithPriority(100)
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
                            .DefineRule("5001")
                            .AllowInbound()
                            .FromAnyAddress()
                            .FromAnyPort()
                            .ToAnyAddress()
                            .ToPort(5001)
                            .WithAnyProtocol()
                            .WithPriority(107)
                            .Attach()
                            .DefineRule("5002")
                            .AllowInbound()
                            .FromAnyAddress()
                            .FromAnyPort()
                            .ToAnyAddress()
                            .ToPort(5002)
                            .WithAnyProtocol()
                            .WithPriority(108)
                            .Attach()
                            .DefineRule("5003")
                            .AllowInbound()
                            .FromAnyAddress()
                            .FromAnyPort()
                            .ToAnyAddress()
                            .ToPort(5003)
                            .WithAnyProtocol()
                            .WithPriority(109)
                            .Attach()
                            .CreateAsync();
                        return newNsg;
                    }
                    catch (System.Exception e)
                    {
                        await Task.Delay(2000);
                        Util.Log($"error {e.Message}, retry create {i}th nsg");
                        continue;
                    }
                }

            });

        }

        public Task<INetworkInterface> CreateNetworkInterfaceAsync(string nicBase, Region location,
            string groupName, string subNet, INetwork network,
            IPublicIPAddress publicIPAddress, INetworkSecurityGroup nsg, int i = 0)
        {
            Console.WriteLine($"Creating {i}th network interface in resource group {groupName}");
            var j = 0;
            var maxRetry = 5; // 5 times retry is enough
            while (j < maxRetry)
            {
                try
                {
                    if (publicIPAddress != null && nsg != null)
                    {
                        var newNic = _azure.NetworkInterfaces.Define(nicBase + Convert.ToString(i))
                            .WithRegion(location)
                            .WithExistingResourceGroup(groupName)
                            .WithExistingPrimaryNetwork(network)
                            .WithSubnet(subNet)
                            .WithPrimaryPrivateIPAddressDynamic()
                            .WithExistingPrimaryPublicIPAddress(publicIPAddress)
                            .WithExistingNetworkSecurityGroup(nsg)
                            .CreateAsync();
                        return newNic;
                    }
                    else
                    {
                        var newNic = _azure.NetworkInterfaces.Define(nicBase + Convert.ToString(i))
                            .WithRegion(location)
                            .WithExistingResourceGroup(groupName)
                            .WithExistingPrimaryNetwork(network)
                            .WithSubnet(subNet)
                            .WithPrimaryPrivateIPAddressDynamic()
                            .CreateAsync();
                        return newNic;
                    }
                }
                catch (Exception e)
                {
                    // clear the uncompleted data
                    var allNICs = _azure.NetworkInterfaces.ListByResourceGroup(groupName);
                    var ids = new List<string>();
                    var enumerator = allNICs.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        ids.Add(enumerator.Current.Id);
                    }
                    _azure.NetworkInterfaces.DeleteByIds(ids);

                    Task.Delay(2000);
                    Util.Log($"error: {e.Message} retry create {i}th nic");
                }
                j++;
            }
            return null;
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

                cmd = $"echo '{password}' | sudo -S cp /etc/security/limits.conf /etc/security/limits.conf.bak";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes : true, retry : 5);

                cmd = $"cp /etc/security/limits.conf ~/limits.conf";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes : true, retry : 5);

                cmd = $"echo '{user}    soft    nofile  655350\n' >> ~/limits.conf";
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
                    case "eastus":
                        location = Region.USEast;
                        break;
                    case "westus":
                        location = Region.USWest;
                        break;
                    case "westus2":
                        location = Region.USWest2;
                        break;
                    case "southeastasia":
                        location = Region.AsiaSouthEast;
                        break;
                    case "westeurope":
                        location = Region.EuropeWest;
                        break;
                    default:
                        location = Region.AsiaSouthEast;
                        break;
                }

                return location;
            }
        }

        public VirtualMachineSizeTypes AgentVmSize
        {
            get
            {
                return GetVmSize(_agentConfig.AgentVmSize);
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
                case "StandardDS1V2":
                    return VirtualMachineSizeTypes.StandardDS1V2;
                case "StandardDS2V2":
                    return VirtualMachineSizeTypes.StandardDS2V2;
                case "StandardDS3V2":
                    return VirtualMachineSizeTypes.StandardDS3V2;
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
                case "StandardF4s":
                    return VirtualMachineSizeTypes.StandardF4s;
                case "StandardF4sV2":
                    return VirtualMachineSizeTypes.StandardF4sV2;
                case "StandardF8sV2":
                    return VirtualMachineSizeTypes.StandardF8sV2;
                case "StandardF16sV2":
                    return VirtualMachineSizeTypes.StandardF16sV2;
                case "StandardD16sV3":
                    return VirtualMachineSizeTypes.StandardD16sV3;
                case "StandardD32sV3":
                    return VirtualMachineSizeTypes.StandardD32sV3;
                default:
                    return VirtualMachineSizeTypes.Parse(vmSizeName);
            }
        }

        public string GroupName
        {
            get
            {
                return _agentConfig.Prefix + "ResourceGroup" + _rndNum;

            }
        }

        public string BenchGroupName
        {
            get
            {
                return _agentConfig.Prefix + "BenchResourceGroup" + _rndNum;

            }
        }

        public string GetPrivateIp(string nicName)
        {
            var retry = 5;
            var i = 0;
            while (i < retry)
            {
                using (var client = new NetworkManagementClient(_credentials))
                {
                    try
                    {
                        client.SubscriptionId = _servicePrincipal.Subscription;
                        var network = NetworkInterfacesOperationsExtensions.GetAsync(client.NetworkInterfaces, GroupName, nicName).GetAwaiter().GetResult();
                        string ip = network.IpConfigurations[0].PrivateIPAddress;
                        return ip;
                    }
                    catch (Exception e)
                    {
                        if (i + 1 == retry)
                        {
                            throw;
                        }
                        else
                        {
                            Util.Log($"Encounter error {e.Message} and retry");
                        }
                    }
                };
                i++;
            }
            throw new Exception("Fail to get private IP");
        }

        static void WaitPortOpen(string ipAddr, int port, TimeSpan timeSpan)
        {
            using (var cts = new CancellationTokenSource(timeSpan))
            {
                Util.Log($"Check {ipAddr}:{port} open or not");
                while (!cts.IsCancellationRequested)
                {
                    if (isPortOpen(ipAddr, port))
                    {
                        Util.Log($"{ipAddr}:{port} is ready");
                        return;
                    }
                }
                Util.Log($"{ipAddr}:{port} is not reachable after {timeSpan.Seconds} seconds.");
            }
        }

        private static bool isPortOpen(string ip, int port)
        {
            var ipAddr = IPAddress.Parse(ip);
            var endPoint = new IPEndPoint(ipAddr, port);
            try
            {
                var tcp = new TcpClient();
                tcp.Connect(endPoint);
                return true;
            }
            catch (Exception e)
            {
                Util.Log($"{ip}:{port} is unaccessible for {e.Message}");
                return false;
            }
        }

        public (INetwork, ISubnet) GetVnetSubnet(string groupName, string vnetName, string subnetName)
        {
            foreach (var vnet in _azure.Networks.ListByResourceGroup(groupName))
            {
                var subnet = vnet.Subnets.GetValueOrDefault(subnetName);
                if (vnet.Name == vnetName) return (vnet, subnet);
            }
            return (null, null);
        }
    }
}