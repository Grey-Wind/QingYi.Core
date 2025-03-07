using QingYi.Core.Shell;

namespace ShellTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var result = ShellHelper.ExecuteCommandAsync("echo Hello, World! > example.txt", ShellType.Cmd).Result;
            Console.WriteLine(result.StandardOutput);
            Console.WriteLine("complete");
            Console.ReadLine();
        }
    }
}
