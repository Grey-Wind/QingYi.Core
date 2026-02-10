using NUnitLite;

namespace TestProject
{
    class Program
    {
        static int Main(string[] args)
        {
            PrintHeader();

            // 确保有输出目录
            Directory.CreateDirectory("./TestResults");

            // 设置测试运行参数
            var defaultArgs = new List<string>
            {
                "--noheader",
                "--labels=All",
                "--work=./TestResults",
                "--out=TestResult.txt",
                "--err=TestErrors.txt",
                "--result=TestResult.xml;format=nunit3"
            };

            // 如果命令行没有参数，使用默认参数
            if (args.Length == 0)
            {
                args = defaultArgs.ToArray();
            }

            // 创建测试运行器
            var assembly = typeof(AesCryptoTests).Assembly;
            var runner = new AutoRun(assembly);

            // 运行测试
            var startTime = DateTime.Now;
            var result = runner.Execute(args);
            var elapsedTime = DateTime.Now - startTime;

            // 解析 XML 结果文件获取详细数据
            ParseAndPrintResults(elapsedTime);

            Console.ReadLine();

            return result;
        }

        #region Other methods
        static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                   AES CRYPTO LIBRARY TEST SUITE                      ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine($"测试开始时间: {DateTime.Now:yyyy年MM月dd日 HH:mm:ss}");
            Console.WriteLine($"运行环境: {Environment.OSVersion}");
            Console.WriteLine($"CLR 版本: {Environment.Version}");
            Console.WriteLine();
        }

        static void ParseAndPrintResults(TimeSpan elapsedTime)
        {
            var resultFile = "./TestResults/TestResult.xml";

            if (!File.Exists(resultFile))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("警告: 未找到测试结果文件");
                Console.ResetColor();
                return;
            }

            try
            {
                var xmlContent = File.ReadAllText(resultFile);
                var testResults = ParseNUnitXml(xmlContent);

                PrintSummary(testResults, elapsedTime);
                PrintDetailedResults(testResults);
                PrintTestCategories(testResults);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"解析测试结果时出错: {ex.Message}");
                Console.ResetColor();
            }
        }

        static TestSuiteResult ParseNUnitXml(string xml)
        {
            // 简化的 XML 解析（实际项目中可以使用 XmlDocument 或 XDocument）
            var result = new TestSuiteResult();

            // 这里使用简单的字符串解析，实际应该使用 XML 解析器
            var testCaseNodes = xml.Split(new[] { "<test-case" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var node in testCaseNodes.Skip(1))
            {
                var testCase = new TestCaseResult();

                // 提取测试名称
                var nameStart = node.IndexOf("fullname=\"") + 10;
                var nameEnd = node.IndexOf("\"", nameStart);
                if (nameStart > 10 && nameEnd > nameStart)
                    testCase.FullName = node.Substring(nameStart, nameEnd - nameStart);

                // 提取结果
                if (node.Contains("result=\"Passed\""))
                {
                    testCase.Result = TestResult.Passed;
                    result.PassedCount++;
                }
                else if (node.Contains("result=\"Failed\""))
                {
                    testCase.Result = TestResult.Failed;
                    result.FailedCount++;

                    // 提取失败信息
                    var messageStart = node.IndexOf("<message><![CDATA[");
                    if (messageStart > 0)
                    {
                        messageStart += 18;
                        var messageEnd = node.IndexOf("]]></message>", messageStart);
                        if (messageEnd > messageStart)
                            testCase.Message = node.Substring(messageStart, messageEnd - messageStart);
                    }

                    // 提取堆栈跟踪
                    var stackStart = node.IndexOf("<stack-trace><![CDATA[");
                    if (stackStart > 0)
                    {
                        stackStart += 22;
                        var stackEnd = node.IndexOf("]]></stack-trace>", stackStart);
                        if (stackEnd > stackStart)
                            testCase.StackTrace = node.Substring(stackStart, stackEnd - stackStart);
                    }
                }
                else if (node.Contains("result=\"Skipped\""))
                {
                    testCase.Result = TestResult.Skipped;
                    result.SkippedCount++;
                }

                // 提取持续时间
                var durationStart = node.IndexOf("duration=\"") + 10;
                var durationEnd = node.IndexOf("\"", durationStart);
                if (durationStart > 10 && durationEnd > durationStart)
                {
                    if (double.TryParse(node.Substring(durationStart, durationEnd - durationStart),
                        out double duration))
                        testCase.Duration = TimeSpan.FromSeconds(duration);
                }

                // 提取类别
                var categoryStart = node.IndexOf("<property name=\"Category\"");
                if (categoryStart > 0)
                {
                    var valueStart = node.IndexOf("value=\"", categoryStart) + 7;
                    var valueEnd = node.IndexOf("\"", valueStart);
                    if (valueEnd > valueStart)
                        testCase.Category = node.Substring(valueStart, valueEnd - valueStart);
                }

                result.TestCases.Add(testCase);
                result.TotalCount++;
            }

            return result;
        }

        static void PrintSummary(TestSuiteResult results, TimeSpan elapsedTime)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("测试结果汇总");
            Console.WriteLine(new string('═', 60));
            Console.ResetColor();

            Console.WriteLine($"测试总数:  {results.TotalCount}");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"通过:      {results.PassedCount}");
            Console.ResetColor();
            Console.WriteLine($"  ({CalculatePercentage(results.PassedCount, results.TotalCount)}%)");

            if (results.FailedCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"失败:      {results.FailedCount}");
                Console.ResetColor();
                Console.WriteLine($"  ({CalculatePercentage(results.FailedCount, results.TotalCount)}%)");
            }
            else
            {
                Console.WriteLine($"失败:      {results.FailedCount}");
            }

            if (results.SkippedCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"跳过:      {results.SkippedCount}");
                Console.ResetColor();
                Console.WriteLine($"  ({CalculatePercentage(results.SkippedCount, results.TotalCount)}%)");
            }
            else
            {
                Console.WriteLine($"跳过:      {results.SkippedCount}");
            }

            Console.WriteLine($"总耗时:    {elapsedTime.TotalSeconds:F2} 秒");
            Console.WriteLine($"平均耗时:  {(results.TotalCount > 0 ? elapsedTime.TotalSeconds / results.TotalCount : 0):F3} 秒/测试");

            // 显示进度条
            Console.WriteLine();
            DisplayProgressBar(results);
        }

        static void DisplayProgressBar(TestSuiteResult results)
        {
            const int barWidth = 50;
            var passedWidth = (int)((double)results.PassedCount / results.TotalCount * barWidth);
            var failedWidth = (int)((double)results.FailedCount / results.TotalCount * barWidth);
            var skippedWidth = barWidth - passedWidth - failedWidth;

            Console.Write("进度: [");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(new string('█', passedWidth));

            if (failedWidth > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(new string('█', failedWidth));
            }

            if (skippedWidth > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(new string('░', skippedWidth));
            }

            Console.ResetColor();
            Console.WriteLine("]");
        }

        static void PrintDetailedResults(TestSuiteResult results)
        {
            if (results.FailedCount > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("失败测试详情:");
                Console.WriteLine(new string('─', 60));
                Console.ResetColor();

                foreach (var testCase in results.TestCases.Where(t => t.Result == TestResult.Failed))
                {
                    Console.WriteLine($"\n❌ {testCase.FullName}");
                    if (!string.IsNullOrEmpty(testCase.Message))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine($"   错误: {testCase.Message}");
                        Console.ResetColor();
                    }

                    if (!string.IsNullOrEmpty(testCase.StackTrace))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        var firstLine = testCase.StackTrace.Split('\n').FirstOrDefault();
                        if (!string.IsNullOrEmpty(firstLine))
                            Console.WriteLine($"   位置: {firstLine.Trim()}");
                        Console.ResetColor();
                    }

                    Console.WriteLine($"   耗时: {testCase.Duration.TotalSeconds:F3} 秒");
                }
            }
        }

        static void PrintTestCategories(TestSuiteResult results)
        {
            var categories = results.TestCases
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .GroupBy(t => t.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Passed = g.Count(t => t.Result == TestResult.Passed),
                    Failed = g.Count(t => t.Result == TestResult.Failed),
                    Skipped = g.Count(t => t.Result == TestResult.Skipped)
                })
                .ToList();

            if (categories.Any())
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("测试类别统计:");
                Console.WriteLine(new string('─', 60));
                Console.ResetColor();

                foreach (var category in categories)
                {
                    Console.Write($"  {category.Category,-20} ");
                    Console.Write($"总数: {category.Count,3}  ");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"✓{category.Passed,2} ");
                    Console.ResetColor();

                    if (category.Failed > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"✗{category.Failed,2} ");
                        Console.ResetColor();
                    }

                    if (category.Skipped > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"⚡{category.Skipped,2}");
                        Console.ResetColor();
                    }

                    Console.WriteLine();
                }
            }
        }

        static string CalculatePercentage(int value, int total)
        {
            if (total == 0) return "0.0";
            return ((double)value / total * 100).ToString("F1");
        }
        #endregion
    }

    #region 辅助类
    enum TestResult { Passed, Failed, Skipped }

    class TestCaseResult
    {
        public string FullName { get; set; } = "";
        public TestResult Result { get; set; }
        public TimeSpan Duration { get; set; }
        public string Message { get; set; } = "";
        public string StackTrace { get; set; } = "";
        public string Category { get; set; } = "";
    }

    class TestSuiteResult
    {
        public List<TestCaseResult> TestCases { get; } = new List<TestCaseResult>();
        public int TotalCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
    }
    #endregion
}
