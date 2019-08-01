using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Commander
{
    class BashUtils
    {
        public static (int, string) Bash(string cmd, bool wait = true, bool handleRes = false, bool captureConsole = false)
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
            if (wait == true)
            {
                if (captureConsole)
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        Console.WriteLine(process.StandardOutput.ReadLine());
                    }
                }
                else
                {
                    result = process.StandardOutput.ReadToEnd();
                }
                process.WaitForExit();
                errCode = process.ExitCode;
            }

            if (handleRes == true)
            {
                if (errCode != 0)
                {
                    Console.WriteLine($"Handle result ERR {errCode}: {result}");
                }
            }

            return (errCode, result);
        }

        public static (int, string) UploadFileToRemote(string host, string username, string password, string srcFile, string destFile)
        {
            int errCode = 0;
            string result = "";
            string cmd = $"sshpass -p {password} scp -o StrictHostKeyChecking=no  -o LogLevel=ERROR {srcFile} {username}@{host}:{destFile}";
            Log.Information($"CMD: {cmd}");
            (errCode, result) = BashUtils.Bash(cmd, wait: true, handleRes: true);
            return (errCode, result);
        }
    }
}
