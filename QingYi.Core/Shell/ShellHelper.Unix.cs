#if !WINDOWS && !BROWSER
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace QingYi.Core.Shell
{
    public static partial class ShellHelper
    {
        private static async Task<ShellResult> ExecuteUnixCommandAsync(string command, bool useAdmin)
        {
            var result = new ShellResult();
            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash",
                Arguments = $"-c \"{(useAdmin ? $"sudo {command}" : command)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var output = new StringBuilder();
            var error = new StringBuilder();

            using (var process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (_, e) => AppendSafe(output, e.Data);
                process.ErrorDataReceived += (_, e) => AppendSafe(error, e.Data);

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();

                    result.ExitCode = process.ExitCode;
                    result.StandardOutput = output.ToString();
                    result.StandardError = error.ToString();
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
}
#endif