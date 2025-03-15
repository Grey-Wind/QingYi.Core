#if NET6_0_OR_GREATER && !BROWSER
#pragma warning disable CA1416
using System;
using System.Diagnostics;
using System.Security;
using Microsoft.Win32;

namespace QingYi.Core.Regedit.PowerSettings
{
    public class PowerConfigurationManager
    {
        // 通用方法：以管理员权限启动进程
        private static void StartProcessAsAdmin(string fileName, string arguments)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = fileName,
                Arguments = arguments,
                Verb = "runas", // 请求管理员权限
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                using Process process = Process.Start(startInfo);
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"执行命令失败: {fileName} {arguments}", ex);
            }
        }

        // 重置默认电源方案（使用PowerShell）
        public static void RestoreDefaultSchemes() => StartProcessAsAdmin("powercfg.exe", "-restoredefaultschemes");

        // 恢复高性能电源计划
        public static void RestoreHighPerformancePlan() => StartProcessAsAdmin("powercfg.exe", "-duplicatescheme 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

        // 恢复平衡电源计划
        public static void RestoreBalancedPlan() => StartProcessAsAdmin("powercfg.exe", "-duplicatescheme 381b4222-f694-41f0-9685-ff5bb260df2e");

        // 恢复省电电源计划
        public static void RestorePowerSaverPlan() => StartProcessAsAdmin("powercfg.exe", "-duplicatescheme a1841308-3541-4fab-bc81-f71556f20b4a");

        // 恢复卓越性能电源计划
        public static void RestoreUltimatePerformancePlan() => StartProcessAsAdmin("powercfg.exe", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");

        // 设置高性能模式
        public static void SetHighPerformanceMode() => StartProcessAsAdmin("powercfg.exe", "/s SCHEME_MIN");

        // 修改注册表禁用Connected Standby
        public static void DisableConnectedStandby()
        {
            try
            {
                // 尝试修改CsEnabled
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power", true))
                {
                    if (key != null)
                    {
                        object csEnabledValue = key.GetValue("CsEnabled");
                        if (csEnabledValue != null)
                        {
                            key.SetValue("CsEnabled", 0, RegistryValueKind.DWord);
                            return;
                        }
                    }
                }

                // 如果CsEnabled不存在，添加PlatformAoAcOverride
                StartProcessAsAdmin("cmd.exe", "/c reg add HKLM\\System\\CurrentControlSet\\Control\\Power /v PlatformAoAcOverride /t REG_DWORD /d 0");
            }
            catch (SecurityException ex)
            {
                throw new InvalidOperationException("需要管理员权限修改注册表。", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException("访问注册表被拒绝。", ex);
            }
        }
    }
}
#endif
