using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace ManageVMs
{
    class ArgsOption
    {
        [Option('c', "vmcount", Required = false, HelpText = "Specify VM Count")]
        public int VmCount { get; set; }

        [Option('p', "prefix", Required = false, HelpText = "Specify VM Prefix for vm and groups")]
        public string Prefix { get; set; }

        [Option('a', "app", Required = false, HelpText = "Specify Auth File")]
        public string AuthFile { get; set; }

        [Option('l', "location", Required = false, HelpText = "Specify Location")]
        public string Location { get; set; }

        [Option('N', "vmname", Required = false, HelpText = "Specify VM Name")]
        public string VmName { get; set; }

        [Option('P', "vmpassword", Required = false, HelpText = "Specify VM PassWord")]
        public string VmPassWord { get; set; }

        [Option('S', "vmsize", Required = false, HelpText = "Specify VM Size")]
        public string VmSize { get; set; }

        [Option('H', "sshkey", Required = false, HelpText = "Specify Ssh")]
        public string Ssh { get; set; }

        [Option('z', "sshport", Required = false, HelpText = "Specify Ssh Port")]
        public int SshPort { get; set; }

        [Option('y', "otherport", Required = false, HelpText = "Specify Benchmark Port")]
        public int OtherPort { get; set; }

        [Option('h', "help", Required = false, HelpText = "dotnet run -- -a ../../../../credential/sp.txt -c 10 -p wanltest -l eastus -N wanl -P wanl12151215WANL -S d2v2 --sshport 22222 --otherport 7000 --sshkey 'ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDETOfp9MX0AgYOlaXX+U2iH0PMLdC0Fm0ET0hmEgakdtAG6ZJnO8DygxYq9a52CzOd6+G0lf1Wxd1eNqFzkc9DjScCfikSrr9iT2+7Wz1tDsKRdh0x9lcwq/jQkH+fmmYiiKYoPplKLtGAsxyAwGPes/QGR1DIfBrpKKDSc6mSyfcyfmkzGNWObtksrgSE11oYY4FLdS/23c9o5915phHHHZKankTn9K9qP4Qenj9VCZpZEKpgv9+3DBxRRPIgN2d2tePBgDWtou3lqBkwjJwWoemOVlRD+gm8yqLUnYn8G/xSqxzWwkPgZjF5zHDeQMv9lb+q5rYNnS8T9CG1WZc1'")]
        public int Help { get; set; }
        
    }
}
