#if !BROWSER
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace QingYi.Core.Shell
{
#if !NETSTANDARD1_6 && !NETSTANDARD1_5
    public static partial class ShellHelper
    {
        private static async Task<ShellResult> ExecuteWindowsCommandAsync(string command, ShellType shellType, bool useAdmin)
        {
            var result = new ShellResult();
            var startInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = !useAdmin,
                RedirectStandardError = !useAdmin,
                UseShellExecute = useAdmin,
                CreateNoWindow = !useAdmin,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            switch (shellType)
            {
                case ShellType.Cmd:
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/c \"{command}\"";
                    break;
                case ShellType.PowerShell:
                    startInfo.FileName = "powershell.exe";
                    startInfo.Arguments = $"-ExecutionPolicy Bypass -Command \"{command}\"";
                    break;
                default:
                    startInfo.FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
                    startInfo.Arguments = $"/c \"{command}\"";
                    break;
            }

            if (useAdmin)
            {
                startInfo.Verb = "runas";
                startInfo.CreateNoWindow = false;
            }

            var output = new StringBuilder();
            var error = new StringBuilder();

            using (var process = new Process { StartInfo = startInfo })
            {
                try
                {
                    if (!useAdmin)
                    {
                        process.OutputDataReceived += (_, e) => AppendSafe(output, e.Data);
                        process.ErrorDataReceived += (_, e) => AppendSafe(error, e.Data);
                    }

                    process.Start();

                    if (!useAdmin)
                    {
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                    }

                    await process.WaitForExitAsync();
                
                    result.ExitCode = process.ExitCode;
                    result.StandardOutput = useAdmin ? "" : output.ToString();
                    result.StandardError = useAdmin ? "" : error.ToString();
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    result.ExitCode = -1;
                    result.StandardError = "User cancelled the UAC prompt.";
                }
                catch (Exception ex)
                {
                    result.ExitCode = -1;
                    result.StandardError = ex.Message;
                }
            }
            return result;
        }
    }
#endif
}
#endif
