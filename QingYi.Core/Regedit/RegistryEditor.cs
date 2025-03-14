#if !BROWSER && NET6_0_OR_GREATER
#pragma warning disable CA1416
using System;
using System.Text;
using Microsoft.Win32;
using System.Security;
using System.Security.Principal;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace QingYi.Core.Regedit
{
    /// <summary>
    /// <strong>Windows Only! Not tested!</strong>
    /// </summary>
    public static class RegistryEditor
    {
        #region 基本操作
        public static void CreateKey(string keyPath, bool useAdmin = false, RegistryView view = RegistryView.Default)
        {
            CheckAdminPermission(useAdmin);
            using var key = CreateParentKey(keyPath, useAdmin, view, true);
        }

        public static void DeleteKey(string keyPath, bool useAdmin = false, RegistryView view = RegistryView.Default)
        {
            CheckAdminPermission(useAdmin);
            var (parentPath, keyName) = SplitKeyPath(keyPath);
            using var parentKey = OpenKey(parentPath, true, useAdmin, view);
            parentKey.DeleteSubKeyTree(keyName);
        }

        public static void SetValue(string keyPath, string valueName, object value, RegistryValueKind kind, bool useAdmin = false, RegistryView view = RegistryView.Default)
        {
            CheckAdminPermission(useAdmin);
            using var key = OpenKey(keyPath, true, useAdmin, view, true);
            key.SetValue(valueName, value, kind);
        }

        public static object GetValue(string keyPath, string valueName, bool useAdmin = false, RegistryView view = RegistryView.Default)
        {
            using var key = OpenKey(keyPath, false, useAdmin, view);
            return key.GetValue(valueName);
        }

        public static void DeleteValue(string keyPath, string valueName, bool useAdmin = false, RegistryView view = RegistryView.Default)
        {
            CheckAdminPermission(useAdmin);
            using var key = OpenKey(keyPath, true, useAdmin, view);
            key.DeleteValue(valueName);
        }

        public static void RenameValue(string keyPath, string oldName, string newName, bool useAdmin = false, RegistryView view = RegistryView.Default)
        {
            CheckAdminPermission(useAdmin);
            using var key = OpenKey(keyPath, true, useAdmin, view);
            var value = key.GetValue(oldName);
            var kind = key.GetValueKind(oldName);
            key.DeleteValue(oldName);
            key.SetValue(newName, value, kind);
        }

        public static void RenameKey(string oldPath, string newPath, bool useAdmin = false, RegistryView view = RegistryView.Default)
        {
            CheckAdminPermission(useAdmin);
            CopyKey(oldPath, newPath, useAdmin, view);
            DeleteKey(oldPath, useAdmin, view);
        }
        #endregion

        #region 导入导出
        public static string ExportToReg(string keyPath, bool useAdmin = false, RegistryView view = RegistryView.Default)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Windows Registry Editor Version 5.00");
            ExportKeyToReg(keyPath, sb, useAdmin, view);
            return sb.ToString();
        }

        public static void ImportFromReg(string regContent, bool useAdmin = false, RegistryView view = RegistryView.Default)
        {
            // 简化的REG文件解析实现
            var lines = regContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string currentKey = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("["))
                {
                    currentKey = line.Trim('[', ']');
                    CreateKey(currentKey, useAdmin, view);
                }
                else if (!string.IsNullOrWhiteSpace(line) && currentKey != null)
                {
                    ProcessValueLine(currentKey, line, useAdmin, view);
                }
            }
        }

    public static string ExportToJson(string keyPath, bool useAdmin = false, RegistryView view = RegistryView.Default)
    {
        var exportData = new RegistryKeyData { KeyPath = keyPath, Values = new List<RegistryValueData>() };
        
        using (var key = OpenKey(keyPath, false, useAdmin, view))
        {
            foreach (var valueName in key.GetValueNames())
            {
                var value = key.GetValue(valueName);
                var kind = key.GetValueKind(valueName);
                exportData.Values.Add(CreateValueData(valueName, value, kind));
            }
        }

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
    }

    public static void ImportFromJson(string json, bool useAdmin = false, RegistryView view = RegistryView.Default)
    {
        var data = JsonSerializer.Deserialize<RegistryKeyData>(json);
        foreach (var value in data.Values)
        {
            SetValue(data.KeyPath, value.Name, ParseValueData(value), value.Kind, useAdmin, view);
        }
    }
        #endregion

        #region 私有方法
        private static void CheckAdminPermission(bool useAdmin)
        {
            if (useAdmin && !IsAdministrator())
                throw new SecurityException("需要管理员权限");
        }

        private static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static RegistryKey OpenKey(string fullPath, bool writable, bool useAdmin, RegistryView view, bool create = false)
        {
            var (rootName, subKey) = SplitKeyPath(fullPath);
            var rootKey = GetRootKey(rootName, view);
            return create ?
                rootKey.CreateSubKey(subKey, writable) :
                rootKey.OpenSubKey(subKey, writable);
        }

        private static RegistryKey CreateParentKey(string fullPath, bool useAdmin, RegistryView view, bool writable)
        {
            var (parentPath, keyName) = SplitKeyPath(fullPath);
            using var parentKey = OpenKey(parentPath, true, useAdmin, view);
            return parentKey.CreateSubKey(keyName, writable);
        }

        private static void CopyKey(string sourcePath, string destPath, bool useAdmin, RegistryView view)
        {
            using var sourceKey = OpenKey(sourcePath, false, useAdmin, view);
            using var destKey = CreateParentKey(destPath, useAdmin, view, true);

            foreach (var valueName in sourceKey.GetValueNames())
            {
                var value = sourceKey.GetValue(valueName);
                var kind = sourceKey.GetValueKind(valueName);
                destKey.SetValue(valueName, value, kind);
            }

            foreach (var subKeyName in sourceKey.GetSubKeyNames())
            {
                CopyKey(Path.Combine(sourcePath, subKeyName),
                       Path.Combine(destPath, subKeyName),
                       useAdmin, view);
            }
        }

        private static void ExportKeyToReg(string keyPath, StringBuilder sb, bool useAdmin, RegistryView view)
        {
            using var key = OpenKey(keyPath, false, useAdmin, view);
            sb.AppendLine($"[{keyPath}]");

            foreach (var valueName in key.GetValueNames())
            {
                var value = key.GetValue(valueName);
                var kind = key.GetValueKind(valueName);
                sb.AppendLine(ValueToRegString(valueName, value, kind));
            }

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                ExportKeyToReg(Path.Combine(keyPath, subKeyName), sb, useAdmin, view);
            }
        }

        private static string ValueToRegString(string name, object value, RegistryValueKind kind)
        {
            var namePart = string.IsNullOrEmpty(name) ? "@" : $"\"{name}\"";
            return kind switch
            {
                RegistryValueKind.DWord => $"{namePart}=dword:{(uint)value:X8}",
                RegistryValueKind.QWord => $"{namePart}=hex(b):{BitConverter.ToString(BitConverter.GetBytes((ulong)value)).Replace("-", ",").ToLower()}",
                RegistryValueKind.Binary => $"{namePart}=hex:{BitConverter.ToString((byte[])value).Replace("-", ",").ToLower()}",
                RegistryValueKind.ExpandString => $"{namePart}=hex(2):{BytesToHex(Encoding.Unicode.GetBytes((string)value + "\0"))}",
                RegistryValueKind.MultiString => $"{namePart}=hex(7):{BytesToHex(Encoding.Unicode.GetBytes(string.Join("\0", (string[])value) + "\0\0"))}",
                _ => $"{namePart}=\"{EscapeString(value.ToString())}\""
            };
        }

        private static string BytesToHex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", ",").ToLower();
        private static string EscapeString(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static (string Root, string SubKey) SplitKeyPath(string fullPath)
        {
            var parts = fullPath.Split(new[] { '\\' }, 2);
            return parts.Length > 1 ? (parts[0], parts[1]) : (parts[0], "");
        }

        private static RegistryKey GetRootKey(string rootName, RegistryView view) => rootName switch
        {
            "HKEY_CLASSES_ROOT" => RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, view),
            "HKEY_CURRENT_USER" => RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view),
            "HKEY_LOCAL_MACHINE" => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view),
            "HKEY_USERS" => RegistryKey.OpenBaseKey(RegistryHive.Users, view),
            "HKEY_CURRENT_CONFIG" => RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, view),
            _ => throw new ArgumentException("无效的根键")
        };

        private static void ProcessValueLine(string keyPath, string line, bool useAdmin, RegistryView view)
        {
            var parts = line.Split(new[] { '=' }, 2);
            if (parts.Length != 2) return;

            var valueName = parts[0].Trim().Trim('"');
            var valueData = parts[1];

            if (valueName == "@") valueName = "";

            var (kind, value) = ParseRegValue(valueData);
            SetValue(keyPath, valueName, value, kind, useAdmin, view);
        }

        private static (RegistryValueKind kind, object value) ParseRegValue(string valueData)
        {
            if (valueData.StartsWith("dword:"))
                return (RegistryValueKind.DWord, Convert.ToUInt32(valueData[6..], 16));

            if (valueData.StartsWith("hex(b):"))
                return (RegistryValueKind.QWord, HexToQWord(valueData[7..]));

            if (valueData.StartsWith("hex:"))
                return (RegistryValueKind.Binary, HexToBytes(valueData[4..]));

            if (valueData.StartsWith("\""))
                return (RegistryValueKind.String, valueData.Trim('"'));

            return (RegistryValueKind.String, valueData);
        }

        private static byte[] HexToBytes(string hex) =>
            Array.ConvertAll(hex.Split(','), b => Convert.ToByte(b, 16));

        private static ulong HexToQWord(string hex) =>
            BitConverter.ToUInt64(HexToBytes(hex.Replace(",", "")), 0);
        #endregion

        #region JSON支持
        private class RegistryKeyData
        {
            public string KeyPath { get; set; }
            public List<RegistryValueData> Values { get; set; }
        }

        private class RegistryValueData
        {
            public string Name { get; set; }
            public RegistryValueKind Kind { get; set; }
            public object Data { get; set; }
            public string Hex { get; set; }
        }

        private static RegistryValueData CreateValueData(string name, object value, RegistryValueKind kind)
        {
            return new RegistryValueData
            {
                Name = name,
                Kind = kind,
                Data = value,
                Hex = kind switch
                {
                    RegistryValueKind.DWord => $"0x{(uint)value:X8}",
                    RegistryValueKind.QWord => $"0x{(ulong)value:X16}",
                    RegistryValueKind.Binary => BitConverter.ToString((byte[])value).Replace("-", ""),
                    _ => null
                }
            };
        }

        private static object ParseValueData(RegistryValueData value)
        {
            return value.Kind switch
            {
                RegistryValueKind.DWord => (int)(JsonSerializer.Deserialize<JsonElement>(value.Data.ToString()).GetUInt32()),
                RegistryValueKind.QWord => (long)(JsonSerializer.Deserialize<JsonElement>(value.Data.ToString()).GetUInt64()),
                RegistryValueKind.Binary => Convert.FromBase64String(value.Data.ToString()),
                _ => value.Data
            };
        }
        #endregion
    }
}
#endif
