using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

#if !BROWSER
namespace QingYi.Core.Shell
{
    public enum ShellType
    {
        Cmd,
        PowerShell,
        Default,
    }

    public class ShellResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
    }

    public static partial class ShellHelper
    {
        public static Task<ShellResult> ExecuteCommandAsync(string command, ShellType shellType, bool useAdmin = false)
        {
#if WINDOWS
            return ExecuteWindowsCommandAsync(command, shellType, useAdmin);
#else
            switch (shellType)
            {
                case ShellType.Cmd:
                case ShellType.PowerShell:
                    return ExecuteWindowsCommandAsync(command, shellType, useAdmin);
                case ShellType.Default:
                    return ExecuteUnixCommandAsync(command, useAdmin);
                default:
                    return ExecuteUnixCommandAsync(command, useAdmin);
            }
#endif
        }

        private static void AppendSafe(StringBuilder builder, string data)
        {
            if (!string.IsNullOrEmpty(data))
                builder.AppendLine(data);
        }

        private static Task<bool> WaitForExitAsync(this Process process)
        {
            var tcs = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => tcs.TrySetResult(true);
            return tcs.Task;
        }
    }
}
#endif
