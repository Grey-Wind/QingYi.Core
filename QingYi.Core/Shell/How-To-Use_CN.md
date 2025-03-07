# 使用方法

```csharp
// 基本用法 - 同步读取
var result = ShellHelper.ExecuteCommandAsync("echo Hello World").Result;
Console.WriteLine($"Exit Code: {result.ExitCode}");
Console.WriteLine($"Output: {result.StandardOutput}");  // Hello World
Console.WriteLine($"Errors: {result.StandardError}");

// 异步等待输出
async Task GetProcessList()
{
    var psResult = await ShellHelper.ExecuteCommandAsync("Get-Process", ShellType.PowerShell);
    Console.WriteLine("Running processes:");
    Console.WriteLine(psResult.StandardOutput);
}

// 错误处理示例
try
{
    var invalidResult = await ShellHelper.ExecuteCommandAsync("invalid_command");
}
catch (Exception ex)
{
    Console.WriteLine($"Command failed: {ex.Message}");
    if (invalidResult != null)
    {
        Console.WriteLine($"Error Output: {invalidResult.StandardError}");
    }
}

// 实时输出处理（仅限非管理员模式）
var realtimeResult = await ShellHelper.ExecuteCommandAsync("ping 127.0.0.1", 
    useAdmin: false);
// 输出会包含完整的ping结果
Console.WriteLine(realtimeResult.StandardOutput); 

// Windows管理员模式示例（注意无法捕获输出）
var adminResult = await ShellHelper.ExecuteCommandAsync("net user", 
    useAdmin: true);
Console.WriteLine($"Admin command exit code: {adminResult.ExitCode}");
// adminResult.StandardOutput 始终为空

// Linux/macOS sudo执行示例
var serviceStatus = await ShellHelper.ExecuteCommandAsync(
    "systemctl status sshd", 
    useAdmin: true);
if(serviceStatus.ExitCode == 0)
{
    Console.WriteLine(serviceStatus.StandardOutput);
}
else
{
    Console.WriteLine($"Error: {serviceStatus.StandardError}");
}

// 带超时的执行
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    var longCmdResult = await ShellHelper.ExecuteCommandAsync(
        "sleep 10", 
        useAdmin: false)
        .WaitAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Command timed out!");
}

// 流式输出处理（高级用法）
var outputBuffer = new StringBuilder();
var process = new Process();
process.StartInfo = //... 初始化代码
process.OutputDataReceived += (sender, e) => 
{
    outputBuffer.AppendLine(e.Data);
    Console.WriteLine($"实时输出: {e.Data}"); // 实时显示
};
process.Start();
process.BeginOutputReadLine();
await process.WaitForExitAsync();

// 多命令组合执行示例
var commands = new [] {
    "cd /var/log",
    "ls -l",
    "cat system.log"
};
var combinedResult = await ShellHelper.ExecuteCommandAsync(
    string.Join("; ", commands),
    useAdmin: false);

// 二进制输出处理（如图像处理）
var imgResult = await ShellHelper.ExecuteCommandAsync(
    "curl -s https://example.com/image.jpg",
    useAdmin: false);
File.WriteAllBytes("image.jpg", 
    Encoding.Default.GetBytes(imgResult.StandardOutput));

// 性能敏感场景的优化处理
var bigDataResult = await ShellHelper.ExecuteCommandAsync(
    "generate-large-data", 
    useAdmin: false);
using var reader = new StringReader(bigDataResult.StandardOutput);
while(reader.ReadLine() is string line)
{
    // 逐行处理大数据量输出
}

// 跨平台差异处理示例
#if WINDOWS
    var winOnlyResult = await ShellHelper.ExecuteCommandAsync(
        "dir C:\\", 
        ShellType.Cmd);
#else
    var unixResult = await ShellHelper.ExecuteCommandAsync(
        "ls /var");
#endif
```

关键输出处理技巧：

1. **输出编码处理**：
```csharp
// 强制使用UTF-8编码
Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine(result.StandardOutput);
```

2. **错误诊断**：
```csharp
if(result.ExitCode != 0)
{
    Console.Error.WriteLine($"命令执行失败！错误码：{result.ExitCode}");
    Console.Error.WriteLine($"错误信息：{result.StandardError}");
}
```

3. **敏感信息过滤**：
```csharp
var sanitizedOutput = result.StandardOutput
    .Replace("password=123456", "password=******");
```

4. **输出重定向到文件**：
```csharp
await using var logWriter = File.CreateText("output.log");
var result = await ShellHelper.ExecuteCommandAsync("long-running-process");
logWriter.WriteLine(result.StandardOutput);
```

5. **JSON输出解析**：
```csharp
var jsonResult = await ShellHelper.ExecuteCommandAsync(
    "Get-Service | ConvertTo-Json", 
    ShellType.PowerShell);
var services = JsonConvert.DeserializeObject<List<ServiceInfo>>(jsonResult.StandardOutput);
```

各平台注意事项：

| 功能                | Windows                     | Linux/macOS                |
|---------------------|----------------------------|----------------------------|
| 管理员输出捕获       | ❌ 无法获取                 | ✅ 需要配置sudo免密        |
| 实时输出             | ✅ 非管理员模式支持         | ✅ 全支持                  |
| 默认编码            | GBK/系统区域设置           | UTF-8                      |
| 命令分隔符          | `&`                        | `;`                        |
| 特权提升方式        | UAC弹窗                    | sudo前缀                   |

特殊场景处理建议：

1. **长时间运行进程**：
```csharp
var longTask = ShellHelper.ExecuteCommandAsync("build-process");
while(!longTask.IsCompleted)
{
    Console.Write(".");
    await Task.Delay(1000);
}
Console.WriteLine(await longTask);
```

2. **交互式命令处理**（需要自定义扩展）：
```csharp
process.StandardInput.WriteLine("yes"); // 自动确认
process.StandardInput.WriteLine("password123"); // 输入密码
```

3. **输出分块处理**：
```csharp
var chunkedResult = await ShellHelper.ExecuteCommandAsync("streaming-data");
using var splitReader = new StringReader(chunkedResult.StandardOutput);
while (splitReader.ReadLine() is string chunk)
{
    ProcessChunk(chunk);
}
```

通过以上示例，您可以根据具体需求选择合适的输出处理方式，并注意不同平台的特性差异。实际开发时建议封装统一的输出处理模块，以增强代码复用性。