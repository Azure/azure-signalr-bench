using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using CommandLine;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core.ResourceActions;

namespace ManageVMs
{
    class Program
    {
        public static void Main (string[] args)
        {
            

            // parse args
            var agentConfig = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => agentConfig = options)
                .WithNotParsed(error => { });

            var sw = new Stopwatch();
            sw.Start();

            // auth file
            Util.Log($"auth file: {agentConfig.AuthFile}");
            var credentials = SdkContext.AzureCredentialsFactory
                .FromFile(agentConfig.AuthFile);

            //var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId:, clientSecret:, tenantId:, environment:AzureEnvironment.AzureGlobalCloud)

            var azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();

            // create resource group
            var rnd = new Random();
            var groupName = agentConfig.Prefix + "ResourceGroup";
            var rndNum = Convert.ToString(rnd.Next(0, 100000) * rnd.Next(0, 100000));
            var vmNameBase = agentConfig.Prefix.ToLower() + rndNum + "vm";
            Region location = null;
            switch (agentConfig.Location.ToLower())
            {
                case "useast":
                    location = Region.USEast;
                    break;
                case "westus":
                    location = Region.USWest;
                    break;
                default:
                    location = Region.USEast;
                    break;
            }

            VirtualMachineSizeTypes VmSize = null;
            switch (agentConfig.VmSize.ToLower())
            {
                case "standardds1":
                    VmSize = VirtualMachineSizeTypes.StandardDS1;
                    break;
                case "d2v2":
                    VmSize = VirtualMachineSizeTypes.StandardD2V2;
                    break;
                default:
                    break;
            }

            Console.WriteLine("Creating resource group...");
            var resourceGroup = azure.ResourceGroups.Define(groupName)
                .WithRegion(location)
                .Create();

            // create availability set
            Console.WriteLine("Creating availability set...");
            var availabilitySet = azure.AvailabilitySets.Define(agentConfig.Prefix + "AVSet")
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithSku(AvailabilitySetSkuTypes.Managed)
                .Create();


            // create virtual net
            Console.WriteLine("Creating virtual network...");
            var network = azure.Networks.Define(agentConfig.Prefix + rndNum + "VNet")
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithAddressSpace("10.0.0.0/16")
                .WithSubnet(agentConfig.Prefix + rndNum + "Subnet", "10.0.0.0/24")
                .Create();


            // Prepare a batch of Creatable Virtual Machines definitions
            List<ICreatable<IVirtualMachine>> creatableVirtualMachines = new List<ICreatable<IVirtualMachine>>();

            // create vms
            for (var i = 0; i < agentConfig.VmCount; i++)
            {
                // create public ip
                Console.WriteLine("Creating public IP address...");
                var publicIPAddress = azure.PublicIPAddresses.Define(agentConfig.Prefix + rndNum + "PublicIP" + Convert.ToString(i))
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithLeafDomainLabel(agentConfig.Prefix + rndNum + "DNS" + Convert.ToString(i))
                    .WithDynamicIP()
                    .Create();

                // create network security group
                Console.WriteLine($"Creating network security group...");
                var nsg = azure.NetworkSecurityGroups.Define(agentConfig.Prefix + rndNum + "NSG" + Convert.ToString(i))
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
                        .ToPort(agentConfig.SshPort)
                        .WithAnyProtocol()
                        .WithPriority(101)
                        .Attach()
                    .DefineRule("BENCHMARK-PORT")
                        .AllowInbound()
                        .FromAnyAddress()
                        .FromAnyPort()
                        .ToAnyAddress()
                        .ToPort(agentConfig.OtherPort)
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
                    .Create();

                // create network interface
                Console.WriteLine("Creating network interface...");
                var networkInterface = azure.NetworkInterfaces.Define(agentConfig.Prefix + rndNum + "NIC" + Convert.ToString(i))
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetwork(network)
                    .WithSubnet(agentConfig.Prefix + rndNum + "Subnet")
                    .WithPrimaryPrivateIPAddressDynamic()
                    .WithExistingPrimaryPublicIPAddress(publicIPAddress)
                    .WithExistingNetworkSecurityGroup(nsg)
                    .Create();

                Console.WriteLine("Ready to create virtual machine...");
                var vm = azure.VirtualMachines.Define(vmNameBase + Convert.ToString(i))
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetworkInterface(networkInterface)
                    .WithPopularLinuxImage(KnownLinuxVirtualMachineImage.UbuntuServer16_04_Lts)
                    .WithRootUsername(agentConfig.VmName)
                    .WithRootPassword(agentConfig.VmPassWord)
                    .WithSsh(agentConfig.Ssh)
                    .WithComputerName(vmNameBase + Convert.ToString(i))
                    .WithExistingAvailabilitySet(availabilitySet)
                    .WithSize(VmSize);

                creatableVirtualMachines.Add(vm);
            }

            sw.Stop();
            Console.WriteLine($"prepare for creating vms elapsed time: {sw.Elapsed.TotalMinutes} min");
            sw.Restart();
            Console.WriteLine($"creating vms");
            var virtualMachines = azure.VirtualMachines.Create(creatableVirtualMachines.ToArray());
            sw.Stop();
            Console.WriteLine($"creating vms elapsed time: {sw.Elapsed.TotalMinutes} min");

            var errCode = 0;
            var res = "";
            var cmd = "";
            //var rndNum = "1698968314"; // TODO: only for debug

            var swConfig = new Stopwatch();
            swConfig.Start();

            // modify limit.conf and change sshd port and restart
            for (int i = 0; i < agentConfig.VmCount; i++)
            {
                Console.WriteLine($"modify limits: {i}th");

                var domain = DomainName(agentConfig, rndNum, i);

                cmd = $"echo '{agentConfig.VmPassWord}' | sudo -S cp /etc/security/limits.conf /etc/security/limits.conf.bak";
                (errCode, res) = ShellHelper.RemoteBash(agentConfig.VmName, domain, 22, agentConfig.VmPassWord, cmd, handleRes: true);

                cmd = $"cp /etc/security/limits.conf ~/limits.conf";
                (errCode, res) = ShellHelper.RemoteBash(agentConfig.VmName, domain, 22, agentConfig.VmPassWord, cmd, handleRes: true);

                cmd = $"echo 'wanl    soft    nofile  655350\n' >> ~/limits.conf";
                (errCode, res) = ShellHelper.RemoteBash(agentConfig.VmName, domain, 22, agentConfig.VmPassWord, cmd, handleRes: true);

                cmd = $"echo '{agentConfig.VmPassWord}' | sudo -S mv ~/limits.conf /etc/security/limits.conf";
                (errCode, res) = ShellHelper.RemoteBash(agentConfig.VmName, domain, 22, agentConfig.VmPassWord, cmd, handleRes: true);
            }

            // install dotnet
            for (var i = 0; i < agentConfig.VmCount; i++)
            {
                Console.WriteLine($"install dotnet: {i}th");
                var port = 22;
                var domain = DomainName(agentConfig, rndNum, i);
                cmd = $"wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb";
                (errCode, res) = ShellHelper.RemoteBash(agentConfig.VmName, domain, port, agentConfig.VmPassWord, cmd, handleRes: true);

                cmd = $"sudo dpkg -i packages-microsoft-prod.deb";
                (errCode, res) = ShellHelper.RemoteBash(agentConfig.VmName, domain, port, agentConfig.VmPassWord, cmd, handleRes: true);

                cmd = $"sudo apt-get -y install apt-transport-https";
                (errCode, res) = ShellHelper.RemoteBash(agentConfig.VmName, domain, port, agentConfig.VmPassWord, cmd, handleRes: true);

                cmd = $"sudo apt-get update";
                (errCode, res) = ShellHelper.RemoteBash(agentConfig.VmName, domain, port, agentConfig.VmPassWord, cmd, handleRes: true);

                cmd = $"sudo apt-get -y install dotnet-sdk-2.1";
                (errCode, res) = ShellHelper.RemoteBash(agentConfig.VmName, domain, port, agentConfig.VmPassWord, cmd, handleRes: true);
            }

            // modify sshd.conf and restart sshd
            for (int i = 0; i < agentConfig.VmCount; i++)
            {
                Console.WriteLine($"modify sshd_config: {i}th");

                var domain = DomainName(agentConfig, rndNum, i);

                cmd = $"echo '{agentConfig.VmPassWord}' | sudo -S cp   /etc/ssh/sshd_config  /etc/ssh/sshd_config.bak";
                (errCode, res) = ShellHelper.RemoteBash(agentConfig.VmName, domain, 22, agentConfig.VmPassWord, cmd, handleRes: true);

                cmd = $"echo '{agentConfig.VmPassWord}' | sudo -S sed -i 's/22/22222/g' /etc/ssh/sshd_config";
                (errCode, res) = ShellHelper.RemoteBash(agentConfig.VmName, domain, 22, agentConfig.VmPassWord, cmd, handleRes: true);

                cmd = $"echo '{agentConfig.VmPassWord}' | sudo -S service sshd restart";
                (errCode, res) = ShellHelper.RemoteBash(agentConfig.VmName, domain, 22, agentConfig.VmPassWord, cmd, handleRes: true);
            }
            swConfig.Stop();
            Console.WriteLine($"config elapsed time: {swConfig.Elapsed.TotalMinutes} min");
        }

        private static string DomainName(ArgsOption argsOption, string rndNum, int i)
        {
            return argsOption.Prefix + rndNum + "DNS" + Convert.ToString(i) + "." + argsOption.Location.ToLower() + ".cloudapp.azure.com";
        }
    }

    

    class ShellHelper
    {
        public static void HandleResult(int errCode, string result)
        {
            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }
            return;
        }

        public static (int, string) Bash(string cmd, bool wait = true, bool handleRes = false)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            var result = "";
            var errCode = 0;
            if (wait == true) result = process.StandardOutput.ReadToEnd();
            if (wait == true) process.WaitForExit();
            if (wait == true) errCode = process.ExitCode;

            if (handleRes == true)
            {
                HandleResult(errCode, result);
            }

            return (errCode, result);
        }

        public static (int, string) RemoteBash(string user, string host, int port, string password, string cmd, bool wait = true, bool handleRes = false)
        {
            if (host.IndexOf("localhost") >= 0 || host.IndexOf("127.0.0.1") >= 0) return Bash(cmd, wait);
            string sshPassCmd = $"ssh -p {port} -o StrictHostKeyChecking=no {user}@{host} \"{cmd}\"";
            return Bash(sshPassCmd, wait: wait, handleRes: handleRes);
        }

    }
}
