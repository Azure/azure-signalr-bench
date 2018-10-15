using Rpc.Service;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Plugin.Base
{
    // User can implement Iplugin to handle step in master and in slaves
    public interface IPlugin: IMasterStepHandler, ISlavehandler
    {
    }
}
