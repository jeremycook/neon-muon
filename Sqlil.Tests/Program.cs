using Spectre.Console;
using Spectre.Console.Json;
using Sqlil.Core.ExpressionTranslation;
using Sqlil.Core.Syntax;
using Sqlil.Tests;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text.Json;

internal class Program {
    private static void Main(string[] args) {

        var translator = new SelectStmtTranslator();
        var composer = new SqliteComposer();

        var expressions = typeof(Shared).GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            .Select(f => new { f.Name, Lambda = (LambdaExpression)f.GetValue(null)! })
            .ToImmutableArray();

        var table = new Table();
        table.AddColumns(new[] { "Name", "SQL" });

        foreach (var item in expressions) {
            try {
                object translation = translator.Translate(item.Lambda, default);
                var stmt = translation switch {
                    SelectStmt selectStmt => selectStmt,
                    SelectCore selectCore => SelectStmt.Create(selectCore),
                    _ => throw new Exception(translation.GetType().ToString())
                };
                var psql = composer.Compose(stmt);
                var pnum = 1;
                var sql = string.Concat(psql.Segments.Select(x => x switch {
                    SqlRaw raw => raw.Text,
                    SqlConstantParameter constant => "@p" + pnum++,
                    SqlInputParameter input => "@" + (input.SuggestedName != string.Empty ? input.SuggestedName : "p") + pnum++,
                    SqlColumn output => string.Empty,
                    _ => throw new NotSupportedException(x?.GetType().ToString())
                }));
                table.AddRow(new[] { item.Name, sql });
            }
            catch (Exception ex) {
                table.AddRow(new[] { item.Name, $"ERROR: {ex.Message}" });
            }
        }

        AnsiConsole.Write(table);

        //RunTests();
    }

    static TestResultSummary[] RunTests(bool quietly = false, bool reportFailuresOnly = false) {
        using var runner = Xunit.Runners.AssemblyRunner.WithoutAppDomain(typeof(Program).Assembly.Location);

        int totalTests = 0, completedTests = 0, failures = 0;
        runner.OnDiscoveryComplete = info => totalTests = info.TestCasesToRun;

        var tests = new TestResultSummary[0];
        var dc = new { };

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
    }
}
