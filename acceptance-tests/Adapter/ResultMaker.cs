using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AcceptanceTests.Adapter.Dtos;
using AcceptanceTests.Adapter.Models;
using AcceptanceTests.ClientHelper;
using AcceptanceTests.ClientHelper.Models;
using static AcceptanceTests.Adapter.AdapterConstants;

namespace AcceptanceTests.Adapter
{
    /// <summary>
    /// Makes the result file from what a run left behind, and decides its verdict. Green when every
    /// test that ran passed, red when at least one failed, and an error whenever what the run left
    /// behind cannot be trusted: a problem the pack reported, a test run that broke, no test of the
    /// asked type, a test of the other type, a test whose requests were not recorded, or two tests
    /// under one name. An error is never passed off as red, and red is never passed off as green.
    /// </summary>
    public class ResultMaker
    {
        private static readonly IReadOnlyDictionary<string, string> DeploymentModeByTestType =
            new Dictionary<string, string>
            {
                [PackConstants.TestTypes.Release] = ResultFile.DeployedDeploymentMode,
                [PackConstants.TestTypes.Acceptance] = ResultFile.LocalDeploymentMode,
            };

        private readonly TimeProvider timeProvider;

        public ResultMaker(TimeProvider timeProvider)
        {
            this.timeProvider = timeProvider;
        }

        /// <summary>
        /// The result of a test run that finished: one entry per executed test, in a stable order,
        /// each joined to its record of requests, with every line of text redacted. Problems the
        /// pack reported, and anything that makes the run's report untrustworthy, make it an error.
        /// </summary>
        public TestRunResultDto FromTestRun(
            AdapterRequest request,
            IReadOnlyList<ReportedTest> reported,
            IReadOnlyList<TestRecording> recordings,
            IReadOnlyList<string> problems,
            IReadOnlyList<string> testOutput,
            Redaction redaction
        )
        {
            var executed = reported
                .Where(test => test.Outcome != TestReport.NotExecutedOutcome)
                .ToList();
            var diagnostics = problems
                .Concat(FindExitCodeProblems(request, executed))
                .Concat(FindTestProblems(request, executed, recordings))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (diagnostics.Count > 0)
            {
                // A test run that broke says how in its own output, which the report does not hold.
                if (IsBrokenTestRun(request.TestsExitCode) && testOutput.Count > 0)
                {
                    diagnostics.Add(Logs.TestOutputHeading);
                    diagnostics.AddRange(testOutput);
                }

                return this.Error(request, diagnostics, redaction);
            }

            var recordingsByTest = recordings.ToLookup(recording =>
                (recording.ClassName, recording.TestName)
            );
            var entries = executed
                .OrderBy(test => test.ClassName, StringComparer.Ordinal)
                .ThenBy(test => test.TestName, StringComparer.Ordinal)
                .Select(test =>
                    MakeEntry(
                        test,
                        recordingsByTest[(test.ClassName, test.TestName)].Single(),
                        redaction
                    )
                )
                .ToList();
            var failed = entries.Count(entry => entry.Outcome == ResultFile.FailedOutcome);

            var result = this.MakeHeader(
                request,
                failed > 0 ? ResultFile.RedResult : ResultFile.GreenResult
            );
            result.SuccessfulTests = entries.Count - failed;
            result.FailedTests = failed;
            result.TotalTestsRan = entries.Count;
            result.TestResults = entries;
            return result;
        }

        /// <summary>
        /// The result of a run that broke before any verdict existed: the header, no test, and the
        /// reasons in place of the tests, redacted like every other line of the file.
        /// </summary>
        public TestRunResultDto Error(
            AdapterRequest request,
            IEnumerable<string> diagnostics,
            Redaction redaction
        )
        {
            var result = this.MakeHeader(request, ResultFile.ErrorResult);
            result.Diagnostics = diagnostics.Select(redaction.Text).ToList();
            return result;
        }

        private TestRunResultDto MakeHeader(AdapterRequest request, string verdict)
        {
            return new TestRunResultDto
            {
                TestResultFormatVersion = ResultFile.TestResultFormatVersion,
                Id = request.RunId,
                PipelineRunId = request.PipelineRunId,
                ProductId = ResultFile.ProductId,
                CommitId = request.CommitId,
                DeploymentMode = DeploymentModeByTestType[request.TestType],
                TestType = request.TestType,
                TargetUrl = request.TargetUrl,
                StartedAt = request.StartedAt.UtcDateTime.ToString(
                    ResultFile.TimeFormat,
                    CultureInfo.InvariantCulture
                ),
                FinishedAt = this
                    .timeProvider.GetUtcNow()
                    .UtcDateTime.ToString(ResultFile.TimeFormat, CultureInfo.InvariantCulture),
                Result = verdict,
                SuccessfulTests = 0,
                FailedTests = 0,
                TotalTestsRan = 0,
                AppLogs = [],
            };
        }

        // Only exit codes 0 and 2 are a finished test run, and each must agree with the report: a
        // verdict the test run and its report disagree on is no verdict at all.
        private static IEnumerable<string> FindExitCodeProblems(
            AdapterRequest request,
            List<ReportedTest> executed
        )
        {
            var exitCode = request.TestsExitCode;
            var anyFailed = executed.Any(test => test.Outcome != TestReport.PassedOutcome);
            if (exitCode == TestReport.NoTestsExitCode || executed.Count == 0)
            {
                yield return $"No {request.TestType} test ran, so the run has no verdict: the pack holds no test of that type, or every one was skipped.";
            }
            else if (IsBrokenTestRun(exitCode))
            {
                yield return DescribeExitCode(exitCode);
            }
            else if (exitCode == TestReport.AllPassedExitCode && anyFailed)
            {
                yield return "The test run said every test passed, but its report lists a failed test.";
            }
            else if (exitCode == TestReport.SomeFailedExitCode && !anyFailed)
            {
                yield return "The test run said a test failed, but its report lists none.";
            }
        }

        private static IEnumerable<string> FindTestProblems(
            AdapterRequest request,
            List<ReportedTest> executed,
            IReadOnlyList<TestRecording> recordings
        )
        {
            foreach (var test in executed)
            {
                var name = $"{test.ClassName}.{test.TestName}";
                foreach (
                    var unknown in test.Types.Where(type =>
                        !PackConstants.TestTypes.All.Contains(type)
                    )
                )
                {
                    yield return $"{name} carries the type '{unknown}', which is neither {PackConstants.TestTypes.Release} nor {PackConstants.TestTypes.Acceptance}.";
                }

                if (!test.Types.Contains(request.TestType))
                {
                    var types = test.Types.Count > 0 ? string.Join(" and ", test.Types) : "nothing";
                    yield return $"{name} ran in the {request.TestType} set, but it is typed {types}.";
                }

                var records = recordings.Count(recording =>
                    recording.ClassName == test.ClassName && recording.TestName == test.TestName
                );
                if (records == 0)
                {
                    yield return $"The pack kept no record of the requests of {name}: its after-test hook did not finish.";
                }
                else if (records > 1)
                {
                    yield return $"The pack kept {records} records of the requests of {name}, where one test keeps one.";
                }
            }

            // The result keeps each test by its name alone, and history follows a test by that name,
            // so no two runs in one result may share a name: not two runs of one test, and not two
            // tests in different classes.
            foreach (
                var shared in executed
                    .GroupBy(test => test.TestName, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
            )
            {
                var classes = shared
                    .Select(test => test.ClassName)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToList();
                yield return classes.Count == 1
                    ? $"{classes[0]}.{shared.Key} ran more than once under one name: give each of its runs a name of its own, so its history can be followed."
                    : $"{shared.Key} is the name of a test in {string.Join(" and in ", classes)}, and the result keeps a test by its name alone: give each a name of its own, so its history can be followed.";
            }
        }

        /// <summary>Says in a sentence how a test run that ended with this exit code ended.</summary>
        public static string DescribeExitCode(int exitCode)
        {
            if (exitCode > TestReport.SignalExitCodeBase)
            {
                return $"The test run ended with exit code {exitCode}: the test process was stopped by signal {exitCode - TestReport.SignalExitCodeBase}.";
            }

            var meaning = TestReport.ExitCodeMeanings.GetValueOrDefault(
                exitCode,
                "the test run broke"
            );
            return $"The test run ended with exit code {exitCode}: {meaning}.";
        }

        /// <summary>Whether a test run that ended with this exit code broke before it finished: only 0, 2 and 8 are a finished run.</summary>
        public static bool IsBrokenTestRun(int exitCode)
        {
            return exitCode != TestReport.AllPassedExitCode
                && exitCode != TestReport.SomeFailedExitCode
                && exitCode != TestReport.NoTestsExitCode;
        }

        private static TestResultEntryDto MakeEntry(
            ReportedTest test,
            TestRecording recording,
            Redaction redaction
        )
        {
            var passed = test.Outcome == TestReport.PassedOutcome;
            return new TestResultEntryDto
            {
                Name = test.TestName,
                TestType = PackConstants.TestTypes.All.Where(test.Types.Contains).ToList(),
                Outcome = passed ? ResultFile.PassedOutcome : ResultFile.FailedOutcome,
                DurationMs = test.DurationMs,
                Logs = SplitLines(redaction.Text(test.Output)),
                FailureReason = passed ? string.Empty : redaction.Text(test.FailureMessage),
                StackTrace = passed ? string.Empty : redaction.Text(test.StackTrace),
                Requests = recording.Requests,
            };
        }

        // The test's output is one text; the result keeps it line by line, without the empty line
        // its last line break leaves.
        private static List<string> SplitLines(string output)
        {
            var lines = output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
            while (lines.Count > 0 && lines[^1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return lines;
        }
    }
}
