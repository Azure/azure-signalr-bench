using Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin.Base
{
    public class PluginUtils
    {
        public static void AddMethodAndType(IDictionary<string, object> data, IDictionary<string, object> parameters)
        {
            data[Constants.Method] = parameters[Constants.Method];
            data[Constants.Type] = parameters[Constants.Type];
        }


        public static void ShowConfiguration(IDictionary<string, object> dict)
        {
            Log.Information($"Handle step...{Environment.NewLine}Configuration: {Environment.NewLine}{dict.GetContents()}");
        }

        public static void HandleParseEnumResult(bool success, string key)
        {
            if (!success)
            {
                var message = $"Fail to parse enum '{key}'.";
                Log.Error(message);
                throw new Exception(message);
            }
        }
    }
}
