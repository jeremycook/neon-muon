using Spectre.Console;
using Spectre.Console.Json;
using Sqlil;
using Sqlil.Tests;
using System.Linq.Expressions;
using System.Text.Json;

internal class Program {
    private static void Main(string[] args) {

        var builder = new SqlilBuilder();
        var sqlil = builder.Build(Shared.multiJoinAnon);

        var sql = string.Join(" ", sqlil);
        Console.WriteLine(sql);

        //RunTests();
    }

    static TestResultSummary[] RunTests(bool quietly = false, bool reportFailuresOnly = false) {
        using var runner = Xunit.Runners.AssemblyRunner.WithoutAppDomain(typeof(Program).Assembly.Location);

        int totalTests = 0, completedTests = 0, failures = 0;
        runner.OnDiscoveryComplete = info => totalTests = info.TestCasesToRun;

        var tests = new TestResultSummary[0];
        var dc = new { };
        //if (!quietly) dc.Dump(collapseTo: 1, repeatHeadersAt: 0);

        runner.OnTestFailed = info => AddTestResult(info);
        runner.OnTestPassed = info => AddTestResult(info);

        using var done = new ManualResetEventSlim();
        runner.OnExecutionComplete = info => {
            if (!quietly) AnsiConsole.WriteLine($"Completed {info.TotalTests} tests in {Math.Round(info.ExecutionTime, 3)}s ({info.TestsFailed} failed)");
            done.Set();
        };
        runner.Start();
        done.Wait();
        return tests;

        void AddTestResult(Xunit.Runners.TestInfo testInfo) {
            var summary = new TestResultSummary(testInfo);
            lock (dc) {
                completedTests++;
                if (summary.Failed()) failures++;

                if (!reportFailuresOnly || summary.Failed())
                    tests = tests
                        .Append(summary)
                        .OrderBy(t => t.Succeeded())
                        .ThenBy(t => t.TypeName)
                        .ThenBy(t => t.MethodName)
                        .ThenBy(t => t.Case)
                        .ToArray();

                var json = new JsonText(JsonSerializer.Serialize(tests));
                AnsiConsole.Write(new Panel(json)
                    .Header($"Test Results - {completedTests} of {totalTests} ({failures} failures)")
                    .Collapse()
                    .RoundedBorder()
                    .BorderColor(Color.Yellow));
            }
        }
    }

    class TestResultSummary {
        Xunit.Runners.TestInfo _testInfo;
        public TestResultSummary(Xunit.Runners.TestInfo testInfo) => _testInfo = testInfo;

        public bool Succeeded() => _testInfo is Xunit.Runners.TestPassedInfo;
        public bool Failed() => _testInfo is Xunit.Runners.TestFailedInfo;

        public string TypeName => _testInfo.TypeName;
        public string MethodName => _testInfo.MethodName;

        public string Case => _testInfo.TestDisplayName.Substring(
            _testInfo.TestDisplayName.StartsWith(TypeName + "." + MethodName) ? TypeName.Length + 1 + MethodName.Length : 0);

        public decimal? Seconds => (_testInfo as Xunit.Runners.TestExecutedInfo)?.ExecutionTime;

        public object Status =>
            _testInfo is Xunit.Runners.TestPassedInfo ? "Succeeded" :
            _testInfo is Xunit.Runners.TestFailedInfo ? "Failed" :
            "";

        public Xunit.Runners.TestFailedInfo? FailureInfo => _testInfo as Xunit.Runners.TestFailedInfo;

        //public object Location => Util.VerticalRun(
        //    from match in Regex.Matches(FailureInfo?.ExceptionStackTrace ?? "", @"(at .+?)\s+in\s+.+?:line\s+(\d+)")
        //    let line = int.Parse(match.Groups[2].Value)
        //    select Util.HorizontalRun(true, match.Groups[1].Value, new Hyperlinq(line - 1, 0, $"line {line}")));
    }
}
