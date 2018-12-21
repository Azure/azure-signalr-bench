using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace azuremonitor
{
    class ArgsOption
    {
        [Option("servicePrincipal", Required = true, HelpText = "Specify service principal file")]
        public string ServicePrincipal { get; set; }

        [Option("resourceId", Required = true, HelpText = "The Id of resource you want to monitor")]
        public string ResourceId { get; set; }

        [Option("secondsBeforeNow", Required = false, Default = 300,
            HelpText = "Specify the monitor window [time (seconds) before now -- now], default is 300s")]
        public int SecondsBeforeNow { get; set; }
    }
}
