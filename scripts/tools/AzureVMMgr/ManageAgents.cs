using System;
using System.Collections.Generic;
using System.Text;

namespace JenkinsScript
{
    class ManageAgents
    {
        public static (int, string) KillAllDotnet(List<string> agents, string cmd)
        {
            var errCode = 0;
            var result = "";
            agents.ForEach(s =>
            {
                (errCode, result) = ShellHelper.Bash(cmd);
                if (errCode != 0) return;
            });

            return (errCode, result);
        }

        public static (int, string) CloneRepo(List<string> agents, string cmd)
        {
            var errCode = 0;
            var result = "";
            agents.ForEach(s =>
            {
                (errCode, result) = ShellHelper.Bash(cmd);
                if (errCode != 0) return;
            });

            return (errCode, result);
        }

        
    }
}
