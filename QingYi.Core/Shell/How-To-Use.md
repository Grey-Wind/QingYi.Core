# Usage method

```csharp
// Basic usage - synchronous reading
var result = ShellHelper.ExecuteCommandAsync("echo Hello World").Result;
Console.WriteLine($"Exit Code: {result.ExitCode}");
Console.WriteLine($"Output: {result.StandardOutput}");  // Hello World
Console.WriteLine($"Errors: {result.StandardError}");

// Asynchronous wait output
async Task GetProcessList()
{
    var psResult = await ShellHelper.ExecuteCommandAsync("Get-Process", ShellType.PowerShell);
    Console.WriteLine("Running processes:");
    Console.WriteLine(psResult.StandardOutput);
}

// Error handling example
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

// Real-time output processing (non-administrator mode only)
var realtimeResult = await ShellHelper.ExecuteCommandAsync("ping 127.0.0.1", 
    useAdmin: false);
// The output will contain the complete ping result
Console.WriteLine(realtimeResult.StandardOutput); 

// Example of Windows Administrator Mode (note that output cannot be captured)
var adminResult = await ShellHelper.ExecuteCommandAsync("net user", 
    useAdmin: true);
Console.WriteLine($"Admin command exit code: {adminResult.ExitCode}");
// AdminResult. StandardOutput always empty

// Linux/macOS sudo execution example
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

// Execution with timeout
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

// Stream output processing (advanced usage)
var outputBuffer = new StringBuilder();
var process = new Process();
process.StartInfo = //... Initialization code
process.OutputDataReceived += (sender, e) => 
{
    outputBuffer.AppendLine(e.Data);
    Console.WriteLine($"Real-time output: {e.Data}"); // Real-time display
};
process.Start();
process.BeginOutputReadLine();
await process.WaitForExitAsync();

// Example of combining multiple commands
var commands = new [] {
    "cd /var/log",
    "ls -l",
    "cat system.log"
};
var combinedResult = await ShellHelper.ExecuteCommandAsync(
    string.Join("; ", commands),
    useAdmin: false);

// Binary output processing (e.g. image processing)
var imgResult = await ShellHelper.ExecuteCommandAsync(
    "curl -s https://example.com/image.jpg",
    useAdmin: false);
File.WriteAllBytes("image.jpg", 
    Encoding.Default.GetBytes(imgResult.StandardOutput));

// Optimization of performance sensitive scenarios
var bigDataResult = await ShellHelper.ExecuteCommandAsync(
    "generate-large-data", 
    useAdmin: false);
using var reader = new StringReader(bigDataResult.StandardOutput);
while(reader.ReadLine() is string line)
{
    // Process large data volume output line by line
}

// Example of cross-platform difference handling
#if WINDOWS
    var winOnlyResult = await ShellHelper.ExecuteCommandAsync(
        "dir C:\\", 
        ShellType.Cmd);
#else
    var unixResult = await ShellHelper.ExecuteCommandAsync(
        "ls /var");
#endif
```

Key output handling skills：

1. **Output coding processing**：
```csharp
// UTF-8 encoding is mandatory
Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine(result.StandardOutput);
```

2. **Error diagnosis**：
```csharp
if(result.ExitCode != 0)
{
    Console.Error.WriteLine($"Command execution failed! Error code: {result.ExitCode}");
    Console.Error.WriteLine($"Error: {result.StandardError}");
}
```

3. **Sensitive information filtering**：
```csharp
var sanitizedOutput = result.StandardOutput
    .Replace("password=123456", "password=******");
```

4. **The output is redirected to a file**：
```csharp
await using var logWriter = File.CreateText("output.log");
var result = await ShellHelper.ExecuteCommandAsync("long-running-process");
logWriter.WriteLine(result.StandardOutput);
```

5. **JSON output parsing**：
```csharp
var jsonResult = await ShellHelper.ExecuteCommandAsync(
    "Get-Service | ConvertTo-Json", 
    ShellType.PowerShell);
var services = JsonConvert.DeserializeObject<List<ServiceInfo>>(jsonResult.StandardOutput);
```

Precautions for each platform: 

| Feature         | Windows                     | Linux/macOS                |
|---------------------|----------------------------|----------------------------|
| Administrator output capture | ❌ unavailable      | ✅ sudo non-encryption needs to be configured |
| Real-time output | ✅ Non-administrator mode support | ✅ Full support      |
| Default coding | GBK / System locale | UTF-8                      |
| Command separator | `&`                        | `;`                        |
| Privilege promotion mode | UAC popup window    | sudo prefix        |

Solution Suggestions for special scenarios：

1. **A long-running process**：
```csharp
var longTask = ShellHelper.ExecuteCommandAsync("build-process");
while(!longTask.IsCompleted)
{
    Console.Write(".");
    await Task.Delay(1000);
}
Console.WriteLine(await longTask);
```

2. **Interactive command processing** (Custom extension required) :
```csharp
process.StandardInput.WriteLine("yes"); // Automatic confirmation
process.StandardInput.WriteLine("password123"); // Enter password
```

3. **Output is processed in blocks**：
```csharp
var chunkedResult = await ShellHelper.ExecuteCommandAsync("streaming-data");
using var splitReader = new StringReader(chunkedResult.StandardOutput);
while (splitReader.ReadLine() is string chunk)
{
    ProcessChunk(chunk);
}
```

Through the examples above, you can choose the appropriate output processing method according to your specific needs, and pay attention to the differences in the characteristics of different platforms. In practice, it is suggested to encapsulate a unified output processing module to enhance code reusability.