using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin.Base
{
    public interface IBenchmarkConfiguration
    {
        string ModuleName();

        void Parse(string content);
    }
}
