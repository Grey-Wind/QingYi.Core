#if !BROWSER
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace QingYi.Core.Shell
{
    /// <summary>
    /// Enum representing different types of shells.<br />
    /// 枚举表示不同类型的命令行终端。
    /// </summary>
    public enum ShellType
    {
        /// <summary>
        /// Command Prompt shell (Cmd) for Windows.<br />
        /// Windows的命令提示符（Cmd）终端。
        /// </summary>
        Cmd,

        /// <summary>
        /// PowerShell shell for Windows.<br />
        /// Windows的PowerShell终端。
        /// </summary>
        PowerShell,

        /// <summary>
        /// Default shell, typically determined by the operating system.<br />
        /// 默认终端，通常由操作系统决定。
        /// </summary>
        Default,
    }

    /// <summary>
    /// Class representing the result of a shell command execution.<br />
    /// 表示命令行命令执行结果的类。
    /// </summary>
    public class ShellResult
    {
        /// <summary>
        /// Gets or sets the exit code of the shell command.<br />
        /// 获取或设置命令行命令的退出代码。
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Gets or sets the standard output produced by the shell command.<br />
        /// 获取或设置命令行命令的标准输出。
        /// </summary>
        public string StandardOutput { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the standard error produced by the shell command.<br />
        /// 获取或设置命令行命令的标准错误。
        /// </summary>
        public string StandardError { get; set; } = string.Empty;
    }

    /// <summary>
    /// A helper class for executing shell commands asynchronously.<br />
    /// 一个用于异步执行命令行命令的帮助类。
    /// </summary>
    public static partial class ShellHelper
    {
        /// <summary>
        /// Executes a shell command asynchronously based on the specified shell type.<br />
        /// 根据指定的命令行类型异步执行命令。
        /// </summary>
        /// <param name="command">The command to execute.<br />要执行的命令。</param>
        /// <param name="shellType">The type of shell to use (Cmd, PowerShell, Default).<br />要使用的shell类型（Cmd, PowerShell，默认）。</param>
        /// <param name="useAdmin">Indicates whether to run the command with administrative privileges. Default is false.<br />指示是否以管理权限运行该命令。默认为false。</param>
        /// <returns>A task that represents the asynchronous operation, with a result of type <see cref="ShellResult"/>.<br />表示异步操作的任务，其结果类型为<see cref="ShellResult"/>。</returns>
        /// <exception cref="ArgumentException">Thrown when an unsupported shell type is provided.<br />当提供不受支持的shell类型时抛出。</exception>
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
