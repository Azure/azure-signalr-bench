using System;
using System.Collections.Generic;
using System.Text;

namespace JenkinsScript
{
    class ManageAgents
    {
        public static (int, string) KillAllDotnet(List<string> slaves, string cmd)
        {
            var errCode = 0;
            var result = "";
            slaves.ForEach(s =>
            {
                (errCode, result) = ShellHelper.Bash(cmd);
                if (errCode != 0) return;
            });

            return (errCode, result);
        }

        public static (int, string) CloneRepo(List<string> slaves, string cmd)
        {
            var errCode = 0;
            var result = "";
            slaves.ForEach(s =>
            {
                (errCode, result) = ShellHelper.Bash(cmd);
                if (errCode != 0) return;
            });

            return (errCode, result);
        }

        
    }
}
