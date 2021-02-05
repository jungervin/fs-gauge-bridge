using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BridgeClient.DataModel
{
    class ProcessHelper
    {
        public static string RunAndGetResult(string exe, string args)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = exe;
            p.StartInfo.Arguments = args;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }

        public static void Start(string fileName)
        {
            try
            {
                using (Process.Start(fileName))
                {
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ProcessHelper StartNoThrow Failed: {ex}");
            }
        }
    }
}
