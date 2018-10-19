using CommandLine;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core.ResourceActions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VMAccess
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateVM(args).Wait();
        }

        static void CheckInputArgs(string[] args)
        {
            bool invalidOptions = false;
            // parse args
            var agentConfig = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => agentConfig = options)
                .WithNotParsed(error => {
                    Console.WriteLine($"Fail to parse the options: {error}");
                    invalidOptions = true;
                });
            if (invalidOptions)
            {
                return;
            }
            if (agentConfig.BenchClientListFile != null)
            {
                Util.Log($"Bench client output file: {agentConfig.BenchClientListFile}");
            }
            if (agentConfig.VMHostFile != null)
            {
                Util.Log($"VM host output file: {agentConfig.VMHostFile}");
            }
            Util.Log($"auth file: {agentConfig.AuthFile}");
            var credentials = SdkContext.AzureCredentialsFactory
                .FromFile(agentConfig.AuthFile);
            var azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();
            var img = azure.VirtualMachineCustomImages.GetByResourceGroup(agentConfig.ImgResourceGroup, agentConfig.ImgName);
            Util.Log($"Customized image id: {img.Id}");

            var region = img.Region;
            Util.Log($"target region: {region.Name}");
            var VmSize = VirtualMachineSizeTypes.Parse(agentConfig.VmSize);
            Util.Log($"VM size: {VmSize}");
            if (agentConfig.SshPubKeyFile != null)
            {
                var sshPubKey = System.IO.File.ReadAllText(agentConfig.SshPubKeyFile);
                Util.Log($"SSH public key: {sshPubKey}");
            }
            Util.Log($"Accelerated Network: {agentConfig.AcceleratedNetwork}");
        }

        static IVirtualMachineCustomImage GetVMImageWithRetry(IAzure azure, string resourceGroupName, string imageName, int maxRetry = 3)
        {
            var i = 0;
            IVirtualMachineCustomImage img = null;
            while (i < maxRetry)
            {
                try
                {
                    img = azure.VirtualMachineCustomImages.GetByResourceGroup(resourceGroupName, imageName);
                }
                catch (Exception e)
                {
                    Util.Log(e.ToString());
                    if (i + 1 < maxRetry)
                    {
                        Util.Log($"Fail to get VM image for {e.Message} and will retry");
                    }
                    else
                    {
                        Util.Log($"Fail to get VM image for {e.Message} and retry has reached max limit, will return with failure");
                    }
                }
                i++;
            }
            return img;
        }

        static INetworkSecurityGroup CreateNetworkSecurityGroupWithRetry(IAzure azure,
            string resourceGroupName, string name, ArgsOption agentConfig, Region region)
        {
            INetworkSecurityGroup rtn = null;
            var i = 0;
            var maxRetry = agentConfig.MaxRetry;
            var azureRegionIP =
                "167.220.148.0/23,131.107.147.0/24,131.107.159.0/24,131.107.160.0/24,131.107.174.0/24,167.220.24.0/24,167.220.26.0/24,167.220.238.0/27,167.220.238.128/27,167.220.238.192/27,167.220.238.64/27,167.220.232.0/23,167.220.255.0/25,167.220.242.0/27,167.220.242.128/27,167.220.242.192/27,167.220.242.64/27,94.245.87.0/24,167.220.196.0/23,194.69.104.0/25,191.234.97.0/26,167.220.0.0/23,167.220.2.0/24,207.68.190.32/27,13.106.78.32/27,10.254.32.0/20,10.97.136.0/22,13.106.174.32/27,13.106.4.96/27";
            if (agentConfig.BenchServerIP != null)
            {
                azureRegionIP += "," + agentConfig.BenchServerIP;
            }
            var allowedIpRange = azureRegionIP.Split(',');
            while (i < maxRetry)
            {
                try
                {
                    rtn = azure.NetworkSecurityGroups.Define(agentConfig.Prefix + "NSG")
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .DefineRule("Limit-Benchserver")
                        .AllowInbound()
                        .FromAddresses(allowedIpRange)
                        .FromAnyPort()
                        .ToAnyAddress()
                        .ToPort(22)
                        .WithProtocol(SecurityRuleProtocol.Tcp)
                        .WithPriority(800)
                        .WithDescription("Limit SSH")
                        .Attach()
                    .DefineRule("New-SSH-Port")
                        .AllowInbound()
                        .FromAnyAddress()
                        .FromAnyPort()
                        .ToAnyAddress()
                        .ToPort(agentConfig.SshPort)
                        .WithProtocol(SecurityRuleProtocol.Tcp)
                        .WithPriority(900)
                        .WithDescription("New SSH Port")
                        .Attach()
                    .DefineRule("Benchmark-Port")
                        .AllowInbound()
                        .FromAnyAddress()
                        .FromAnyPort()
                        .ToAnyAddress()
                        .ToPort(agentConfig.OtherPort)
                        .WithProtocol(SecurityRuleProtocol.Tcp)
                        .WithPriority(901)
                        .WithDescription("Benchmark Port")
                        .Attach()
                    .DefineRule("Service-Ports")
                        .AllowInbound()
                        .FromAnyAddress()
                        .FromAnyPort()
                        .ToAnyAddress()
                        .ToPortRange(5001, 5003)
                        .WithProtocol(SecurityRuleProtocol.Tcp)
                        .WithPriority(903)
                        .WithDescription("Service Port")
                        .Attach()
                    .DefineRule("Chat-Sample-Ports")
                        .AllowInbound()
                        .FromAnyAddress()
                        .FromAnyPort()
                        .ToAnyAddress()
                        .ToPort(agentConfig.ChatSamplePort)
                        .WithProtocol(SecurityRuleProtocol.Tcp)
                        .WithPriority(904)
                        .WithDescription("Chat Sample Port")
                        .Attach()
                    .DefineRule("RDP-Port")
                        .AllowInbound()
                        .FromAnyAddress()
                        .FromAnyPort()
                        .ToAnyAddress()
                        .ToPort(agentConfig.RDPPort)
                        .WithProtocol(SecurityRuleProtocol.Tcp)
                        .WithPriority(905)
                        .WithDescription("Windows RDP Port")
                        .Attach()
                    .Create();
                }
                catch (Exception e)
                {
                    Util.Log(e.ToString());
                    if (i + 1 < maxRetry)
                    {
                        Util.Log($"Fail to create security network group for {e.Message} and will retry");
                    }
                    else
                    {
                        Util.Log($"Fail to create security network group for {e.Message} and retry has reached max limit, will return with failure");
                    }
                }
                i++;
            }
            return rtn;
        }

        static INetwork CreateVirtualNetworkWithRetry(IAzure azure,
            string subNetName, string resourceGroupName,
            string virtualNetName, Region region, int maxRetry=3)
        {
            var i = 0;
            while (i < maxRetry)
            {
                try
                {
                    var networkList = azure.Networks.ListByResourceGroupAsync(resourceGroupName);
                    var nwEnumerator = networkList.Result.GetEnumerator();
                    INetwork net = null;
                    while (nwEnumerator.MoveNext())
                    {
                        net = nwEnumerator.Current;
                        break;
                    }
                    if (net != null)
                    {
                        // use existing INetwork
                        return net;
                    }
                    var network = azure.Networks.Define(virtualNetName)
                        .WithRegion(region)
                        .WithExistingResourceGroup(resourceGroupName)
                        .WithAddressSpace("10.0.0.0/16")
                        .WithSubnet(subNetName, "10.0.0.0/24")
                        .Create();
                    return network;
                }
                catch (Exception e)
                {
                    Util.Log(e.ToString());
                    if (i + 1 < maxRetry)
                    {
                        Util.Log($"Fail to create virtual network for {e.Message} and will retry");
                    }
                    else
                    {
                        Util.Log($"Fail to create virtual network for {e.Message} and retry has reached max limit, will return with failure");
                    }
                }
                i++;
            }
            return null;
        }

        static async Task<List<Task<IPublicIPAddress>>> CreatePublicIPAddrListWithRetry(IAzure azure,
            int count, string prefix, string resourceGroupName, Region region, int maxTry=3)
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
                        var publicIPAddress = azure.PublicIPAddresses.Define(prefix + Convert.ToString(i) + "PubIP")
                            .WithRegion(region)
                            .WithExistingResourceGroup(resourceGroupName)
                            .WithLeafDomainLabel(prefix + Convert.ToString(i))
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

        static async Task<List<Task<INetworkInterface>>> CreateNICWithRetry(IAzure azure, string resourceGroupName,
            ArgsOption agentConfig, INetwork network, List<Task<IPublicIPAddress>> publicIpTaskList,
            string subNetName, INetworkSecurityGroup nsg, Region region)
        {
            var j = 0;
            var i = 0;
            var maxTry = agentConfig.MaxRetry;
            var nicTaskList = new List<Task<INetworkInterface>>();
            while (j < maxTry)
            {
                try
                {
                    var allowAcceleratedNet = false;
                    if (agentConfig.CandidateOfAcceleratedNetVM != null)
                    {
                        allowAcceleratedNet = CheckValidVMForAcceleratedNet(agentConfig.CandidateOfAcceleratedNetVM, agentConfig.VmSize);
                    }
                    for (i = 0; i < agentConfig.VmCount; i++)
                    {
                        var nicName = agentConfig.Prefix + Convert.ToString(i) + "NIC";
                        Task<INetworkInterface> networkInterface = null;
                        if (allowAcceleratedNet && agentConfig.AcceleratedNetwork)
                        {
                            networkInterface = azure.NetworkInterfaces.Define(nicName)
                                .WithRegion(region)
                                .WithExistingResourceGroup(resourceGroupName)
                                .WithExistingPrimaryNetwork(network)
                                .WithSubnet(subNetName)
                                .WithPrimaryPrivateIPAddressDynamic()
                                .WithExistingPrimaryPublicIPAddress(publicIpTaskList[i].Result)
                                .WithExistingNetworkSecurityGroup(nsg)
                                .WithAcceleratedNetworking()
                                .CreateAsync();
                            Util.Log("Accelerated Network is enabled!");
                        }
                        else
                        {
                            Util.Log("Accelerated Network is disabled!");
                            networkInterface = azure.NetworkInterfaces.Define(nicName)
                                .WithRegion(region)
                                .WithExistingResourceGroup(resourceGroupName)
                                .WithExistingPrimaryNetwork(network)
                                .WithSubnet(subNetName)
                                .WithPrimaryPrivateIPAddressDynamic()
                                .WithExistingPrimaryPublicIPAddress(publicIpTaskList[i].Result)
                                .WithExistingNetworkSecurityGroup(nsg)
                                .CreateAsync();
                        }
                        nicTaskList.Add(networkInterface);
                    }
                    await Task.WhenAll(nicTaskList.ToArray());
                    return nicTaskList;
                }
                catch (Exception e)
                {
                    Util.Log(e.ToString());
                    nicTaskList.Clear();

                    var allNICs = azure.NetworkInterfaces.ListByResourceGroupAsync(resourceGroupName);
                    await allNICs;
                    var ids = new List<string>();
                    var enumerator = allNICs.Result.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        ids.Add(enumerator.Current.Id);
                    }
                    await azure.NetworkInterfaces.DeleteByIdsAsync(ids);
                    if (j + 1 < maxTry)
                    {
                        Util.Log($"Fail to create NIC for {e.Message} and will retry");
                    }
                    else
                    {
                        Util.Log($"Fail to create NIC for {e.Message} and retry has reached max limit, will return with failure");
                    }
                }
                j++;
            }
            return null;
        }

        static async Task CreateVM(string[] args)
        {
            bool invalidOptions = false;
            // parse args
            var agentConfig = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => agentConfig = options)
                .WithNotParsed(error => {
                    Util.Log($"Fail to parse the options: {error}");
                    invalidOptions = true;
                });
            if (invalidOptions)
            {
                Util.Log("Invalid options");
                return;
            }

            var sw = new Stopwatch();
            sw.Start();

            // auth file
            Util.Log($"auth file: {agentConfig.AuthFile}");
            var credentials = SdkContext.AzureCredentialsFactory
                .FromFile(agentConfig.AuthFile);

            var azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();
            var img = GetVMImageWithRetry(azure, agentConfig.ImgResourceGroup, agentConfig.ImgName, agentConfig.MaxRetry);
            if (img == null)
            {
                throw new Exception("Fail to get custom image");
            }
            Util.Log($"Customized image id: {img.Id}");

            var region = img.Region;
            Util.Log($"target region: {region.Name}");

            var VmSize = VirtualMachineSizeTypes.Parse(agentConfig.VmSize);
            Util.Log($"VM size: {VmSize}");

            string sshPubKey = null;
            if (agentConfig.SshPubKeyFile == null && agentConfig.VmType == 1)
            {
                Util.Log("SSH public key is not set for Linux VM");
                throw new Exception("SSH public key is not specified!");
            }
            else if (agentConfig.SshPubKeyFile != null && agentConfig.VmType == 1)
            {
                sshPubKey = File.ReadAllText(agentConfig.SshPubKeyFile);
                Util.Log($"SSH public key: {sshPubKey}");
                Util.Log($"Accelerated Network: {agentConfig.AcceleratedNetwork}");
            }

            if (agentConfig.VmType == 2 && agentConfig.Password == null)
            {
                Util.Log($"You must specify password for windows VM by -p XXXX");
                return;
            }
            var resourceGroupName = agentConfig.ResourceGroup;
            IResourceGroup resourceGroup = null;
            if (!azure.ResourceGroups.Contain(resourceGroupName))
            {
                resourceGroup = azure.ResourceGroups.Define(resourceGroupName)
                    .WithRegion(region)
                    .Create();
            }

            // create virtual net
            Util.Log("Creating virtual network...");
            var subNetName = agentConfig.Prefix + "Subnet";
            var network = CreateVirtualNetworkWithRetry(azure, subNetName,
                resourceGroupName, agentConfig.Prefix + "VNet", region, agentConfig.MaxRetry);
            if (network == null)
            {
                throw new Exception("Fail to create virtual network");
            }
            Util.Log("Finish creating virtual network");
            // Prepare a batch of Creatable Virtual Machines definitions
            var creatableVirtualMachines = new List<ICreatable<IVirtualMachine>>();

            // create vms
            Util.Log("Creating public IP address...");
            var publicIpTaskList = await CreatePublicIPAddrListWithRetry(azure, agentConfig.VmCount, agentConfig.Prefix, resourceGroupName, region, agentConfig.MaxRetry);
            if (publicIpTaskList == null)
            {
                throw new Exception("Fail to create Public IP Address");
            }
            Util.Log("Finish creating public IP address...");

            Util.Log($"Creating network security group...");
            var nsg = CreateNetworkSecurityGroupWithRetry(azure, resourceGroupName, agentConfig.Prefix + "NSG", agentConfig, region);
            if (nsg == null)
            {
                throw new Exception("Fail to create network security group");
            }
            Util.Log($"Finish creating network security group...");

            Util.Log("Creating network interface...");
            var nicTaskList = await CreateNICWithRetry(azure, resourceGroupName, agentConfig, network, publicIpTaskList, subNetName, nsg, region);
            if (nicTaskList == null)
            {
                throw new Exception("Fail to create NIC task list");
            }
            Util.Log("Finish creating network interface...");

            if (agentConfig.VmType == 1)
            {
                for (var i = 0; i < agentConfig.VmCount; i++)
                {
                    var vm = azure.VirtualMachines.Define(agentConfig.Prefix + Convert.ToString(i))
                        .WithRegion(region)
                        .WithExistingResourceGroup(resourceGroupName)
                        .WithExistingPrimaryNetworkInterface(nicTaskList[i].Result)
                        .WithLinuxCustomImage(img.Id)
                        .WithRootUsername(agentConfig.Username)
                        .WithSsh(sshPubKey)
                        .WithComputerName(agentConfig.Prefix + Convert.ToString(i))
                        .WithSize(VmSize);
                    creatableVirtualMachines.Add(vm);
                }
            }
            else if (agentConfig.VmType == 2)
            {
                for (var i = 0; i < agentConfig.VmCount; i++)
                {
                    var vm = azure.VirtualMachines.Define(agentConfig.Prefix + Convert.ToString(i))
                        .WithRegion(region)
                        .WithExistingResourceGroup(resourceGroupName)
                        .WithExistingPrimaryNetworkInterface(nicTaskList[i].Result)
                        .WithWindowsCustomImage(img.Id)
                        .WithAdminUsername(agentConfig.Username)
                        .WithAdminPassword(agentConfig.Password)
                        .WithComputerName(agentConfig.Prefix + Convert.ToString(i))
                        .WithSize(VmSize);
                    creatableVirtualMachines.Add(vm);
                }
            }
            
            Util.Log("Ready to create virtual machine...");
            sw.Stop();
            Util.Log($"prepare for creating vms elapsed time: {sw.Elapsed.TotalMinutes} min");

            sw.Restart();
            Util.Log($"Creating vms");
            var virtualMachines = azure.VirtualMachines.Create(creatableVirtualMachines.ToArray());
            Util.Log($"Finish creating vms");

            Util.Log("Check SSH port");
            var portCheckTaskList = new List<Task>();
            for (var i = 0; i < agentConfig.VmCount; i++)
            {
                var publicIPAddress = azure.PublicIPAddresses.GetByResourceGroup(resourceGroupName, agentConfig.Prefix + Convert.ToString(i) + "PubIP");
                portCheckTaskList.Add(Task.Run(() => WaitPortOpen(publicIPAddress.IPAddress, 22222)));
            }
            
            if (Task.WaitAll(portCheckTaskList.ToArray(), TimeSpan.FromSeconds(120)))
            {
                Util.Log("All ports are ready");
            }
            else
            {
                Util.Log("Not all ports are ready");
            }

            sw.Stop();
            Util.Log($"creating vms elapsed time: {sw.Elapsed.TotalMinutes} min");
            if (agentConfig.VMHostFile != null)
            {
                var builder = new StringBuilder();
                for (var i = 0; i < agentConfig.VmCount; i++)
                {
                    if (i != 0)
                    {
                        builder.Append('|');
                    }
                    builder.Append(agentConfig.Prefix).Append(i).Append(".")
                           .Append(region.Name).Append(".cloudapp.azure.com");
                }
                File.WriteAllText(agentConfig.VMHostFile, builder.ToString());
            }
            if (agentConfig.BenchClientListFile != null)
            {
                var builder = new StringBuilder();
                for (var i = 0; i < agentConfig.VmCount; i++)
                {
                    if (i != 0)
                    {
                        builder.Append('|');
                    }
                    builder.Append(agentConfig.Prefix).Append(i).Append(".")
                           .Append(region.Name).Append(".cloudapp.azure.com")
                           .Append(':').Append(agentConfig.SshPort).Append(':').Append(agentConfig.Username);
                }
                File.WriteAllText(agentConfig.BenchClientListFile, builder.ToString());
            }
        }

        static bool CheckValidVMForAcceleratedNet(string accelVMSizeFile, string vmSize)
        {
            bool found = false;
            IEnumerable<string> lines = File.ReadLines(accelVMSizeFile);
            var enumerator = lines.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Equals(vmSize))
                {
                    found = true;
                    break;
                }
            }
            return found;
        }

        static void CheckVMPorts(string[] args)
        {
            bool invalidOptions = false;
            // parse args
            var agentConfig = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => agentConfig = options)
                .WithNotParsed(error => {
                    Console.WriteLine($"Fail to parse the options: {error}");
                    invalidOptions = true;
                });
            if (invalidOptions)
            {
                return;
            }

            var sw = new Stopwatch();
            sw.Start();

            // auth file
            Util.Log($"auth file: {agentConfig.AuthFile}");
            var credentials = SdkContext.AzureCredentialsFactory
                .FromFile(agentConfig.AuthFile);

            var azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();
            var pubIP = azure.PublicIPAddresses.GetByResourceGroup("honzhanautovm0705", "hzautovm07050PubIP");
            Util.Log($"Public IP: {pubIP.IPAddress}");
            CheckAllPorts(pubIP.IPAddress);
        }

        static void CheckAllPorts(string ipAddr)
        {
            var portCheckTaskList = new List<Task>();
            portCheckTaskList.Add(Task.Run(() => WaitPortOpen(ipAddr, 22222)));
            if (Task.WaitAll(portCheckTaskList.ToArray(), TimeSpan.FromSeconds(120)))
            {
                Util.Log("All ports are ready");
            }
            else
            {
                Util.Log("Not all ports are ready");
            }
        }

        static void WaitPortOpen(string ipAddr, int port)
        {
            using (var cts = new CancellationTokenSource())
            {
                Util.Log($"Check {ipAddr}:{port} open or not");
                cts.CancelAfter(TimeSpan.FromSeconds(120));
                while (!cts.IsCancellationRequested)
                {
                    if (Util.isPortOpen(ipAddr, port))
                    {
                        break;
                    }
                }
            }
        }
    }
}
