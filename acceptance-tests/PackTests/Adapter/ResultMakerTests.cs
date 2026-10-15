using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTests.Adapter;
using AcceptanceTests.Adapter.Models;
using AcceptanceTests.ClientHelper;
using AcceptanceTests.ClientHelper.Models;
using AcceptanceTests.PackTests.StandIns;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static AcceptanceTests.Adapter.AdapterConstants;

namespace AcceptanceTests.PackTests.Adapter
{
    public class ResultMakerTests
    {
        private const string Token = "token-of-the-test-identity";
        private const string SampleClass = "AcceptanceTests.Examples.SampleRouteTests";
        private const string HealthClass = "AcceptanceTests.Examples.HealthRouteTests";

        private static readonly DateTimeOffset FinishedAt = new(
            2026,
            12,
            3,
            9,
            41,
            31,
            TimeSpan.Zero
        );

        private readonly ResultMaker maker = new(new FixedTimeProvider(FinishedAt));
        private readonly Redaction redaction = new([Token]);

        [Test]
        public async Task A_run_whose_tests_all_passed_is_green_with_every_test_in_full()
        {
            var tests = new[]
            {
                Passed(SampleClass, "Sample", "Release", "Acceptance"),
                Passed(HealthClass, "Health", "Acceptance"),
            };

            var result = this.maker.FromTestRun(
                Request("Acceptance", 0),
                tests,
                Recorded(tests),
                [],
                [],
                this.redaction
            );

            await Assert.That(result.Result).IsEqualTo("Green");
            await Assert
                .That((result.SuccessfulTests, result.FailedTests, result.TotalTestsRan))
                .IsEqualTo((2, 0, 2));
            await Assert.That(result.TestResults.Count).IsEqualTo(2);
            await Assert.That(result.Diagnostics).IsNull();
            await Assert.That(result.TestResults[0].Name).IsEqualTo("Health");
            await Assert.That(result.TestResults[1].Name).IsEqualTo("Sample");
            await Assert
                .That(string.Join(",", result.TestResults[1].TestType))
                .IsEqualTo("Release,Acceptance");
            await Assert.That(result.TestResults[1].Requests.Count).IsEqualTo(1);
            await Assert.That(result.TestResults[1].FailureReason).IsEqualTo(string.Empty);
            await Assert.That(result.TestResults[1].StackTrace).IsEqualTo(string.Empty);
        }

        [Test]
        public async Task The_header_says_who_ran_what_where_and_when()
        {
            var tests = new[] { Passed(SampleClass, "Sample", "Release", "Acceptance") };

            var local = this.maker.FromTestRun(
                Request("Acceptance", 0),
                tests,
                Recorded(tests),
                [],
                [],
                this.redaction
            );
            var deployed = this.maker.FromTestRun(
                Request("Release", 0),
                tests,
                Recorded(tests),
                [],
                [],
                this.redaction
            );

            await Assert.That(local.TestResultFormatVersion).IsEqualTo(1);
            await Assert.That(local.Id).IsEqualTo("run-9f3k2m7q1x");
            await Assert.That(local.PipelineRunId).IsEqualTo("4242");
            await Assert.That(local.ProductId).IsEqualTo(ResultFile.ProductId);
            await Assert.That(local.CommitId).IsEqualTo("b7e2c1a");
            await Assert.That(local.TargetUrl).IsEqualTo("http://127.0.0.1:9000");
            await Assert.That(local.StartedAt).IsEqualTo("2026-12-03T09:41:07Z");
            await Assert.That(local.FinishedAt).IsEqualTo("2026-12-03T09:41:31Z");
            await Assert.That(local.AppLogs.Count).IsEqualTo(0);
            await Assert
                .That((local.TestType, local.DeploymentMode))
                .IsEqualTo(("Acceptance", "Local"));
            await Assert
                .That((deployed.TestType, deployed.DeploymentMode))
                .IsEqualTo(("Release", "Deployed"));
        }

        [Test]
        public async Task A_run_with_a_failed_test_is_red_and_tells_the_failure_in_full_without_its_secrets()
        {
            var failed = Passed(SampleClass, "Sample", "Release", "Acceptance");
            failed.Outcome = "Failed";
            failed.Output = $"Calling with {Token}\nAssertion failed\n";
            failed.FailureMessage = $"Expected 110 but was 111 for {Token}";
            failed.StackTrace = "at SampleRouteTests.Sample() in SampleRouteTests.cs:line 24";
            var tests = new[] { failed, Passed(HealthClass, "Health", "Acceptance") };

            var result = this.maker.FromTestRun(
                Request("Acceptance", 2),
                tests,
                Recorded(tests),
                [],
                [],
                this.redaction
            );

            var entry = result.TestResults.Single(test => test.Name == "Sample");
            await Assert.That(result.Result).IsEqualTo("Red");
            await Assert
                .That((result.SuccessfulTests, result.FailedTests, result.TotalTestsRan))
                .IsEqualTo((1, 1, 2));
            await Assert.That(entry.Outcome).IsEqualTo("Failed");
            await Assert
                .That(string.Join("|", entry.Logs))
                .IsEqualTo("Calling with [redacted]|Assertion failed");
            await Assert
                .That(entry.FailureReason)
                .IsEqualTo("Expected 110 but was 111 for [redacted]");
            await Assert.That(entry.StackTrace).Contains("line 24");
        }

        [Test]
        public async Task Any_outcome_but_passed_is_a_failure_and_a_skipped_test_is_not_listed()
        {
            var timedOut = Passed(SampleClass, "Sample", "Acceptance");
            timedOut.Outcome = "Timeout";
            var skipped = Passed(HealthClass, "Health", "Acceptance");
            skipped.Outcome = "NotExecuted";

            var result = this.maker.FromTestRun(
                Request("Acceptance", 2),
                [timedOut, skipped],
                Recorded([timedOut]),
                [],
                [],
                this.redaction
            );

            await Assert.That(result.Result).IsEqualTo("Red");
            await Assert.That(result.TotalTestsRan).IsEqualTo(1);
            await Assert.That(result.TestResults[0].Outcome).IsEqualTo("Failed");
        }

        [Test]
        public async Task A_problem_the_pack_reported_makes_the_run_an_error_whatever_the_tests_did()
        {
            var tests = new[] { Passed(SampleClass, "Sample", "Acceptance") };

            var result = this.maker.FromTestRun(
                Request("Acceptance", 0),
                tests,
                Recorded(tests),
                ["Untyped.Test carries no type."],
                [],
                this.redaction
            );

            await Assert.That(result.Result).IsEqualTo("Error");
            await Assert.That(result.TestResults).IsNull();
            await Assert
                .That(string.Join("|", result.Diagnostics))
                .IsEqualTo("Untyped.Test carries no type.");
            await Assert
                .That((result.SuccessfulTests, result.FailedTests, result.TotalTestsRan))
                .IsEqualTo((0, 0, 0));
        }

        [Test]
        [Arguments(8)]
        [Arguments(0)]
        public async Task A_run_where_no_test_ran_is_an_error_not_green(int exitCode)
        {
            var result = this.maker.FromTestRun(
                Request("Release", exitCode),
                [],
                [],
                [],
                [],
                this.redaction
            );

            await Assert.That(result.Result).IsEqualTo("Error");
            await Assert.That(result.Diagnostics[0]).StartsWith("No Release test ran");
        }

        [Test]
        public async Task A_test_run_that_broke_is_an_error_with_its_last_output()
        {
            var tests = new[] { Passed(SampleClass, "Sample", "Acceptance") };

            var result = this.maker.FromTestRun(
                Request("Acceptance", 3),
                tests,
                Recorded(tests),
                [],
                ["the session was stopped", $"by {Token}"],
                this.redaction
            );

            await Assert.That(result.Result).IsEqualTo("Error");
            await Assert
                .That(string.Join("|", result.Diagnostics))
                .IsEqualTo(
                    "The test run ended with exit code 3: the test session was stopped before it finished.|The test run's last lines of output:|the session was stopped|by [redacted]"
                );
        }

        [Test]
        public async Task A_test_process_stopped_by_a_signal_says_which()
        {
            await Assert
                .That(ResultMaker.DescribeExitCode(137))
                .IsEqualTo(
                    "The test run ended with exit code 137: the test process was stopped by signal 9."
                );
            await Assert
                .That(ResultMaker.DescribeExitCode(42))
                .IsEqualTo("The test run ended with exit code 42: the test run broke.");
        }

        [Test]
        public async Task A_test_run_and_its_report_that_disagree_give_no_verdict()
        {
            var failed = Passed(SampleClass, "Sample", "Acceptance");
            failed.Outcome = "Failed";
            var passed = Passed(SampleClass, "Sample", "Acceptance");

            var greenWithFailure = this.maker.FromTestRun(
                Request("Acceptance", 0),
                [failed],
                Recorded([failed]),
                [],
                [],
                this.redaction
            );
            var redWithoutFailure = this.maker.FromTestRun(
                Request("Acceptance", 2),
                [passed],
                Recorded([passed]),
                [],
                [],
                this.redaction
            );

            await Assert.That(greenWithFailure.Result).IsEqualTo("Error");
            await Assert.That(greenWithFailure.Diagnostics[0]).Contains("said every test passed");
            await Assert.That(redWithoutFailure.Result).IsEqualTo("Error");
            await Assert.That(redWithoutFailure.Diagnostics[0]).Contains("said a test failed");
        }

        [Test]
        public async Task A_test_of_the_other_type_or_of_no_known_type_makes_the_selection_an_error()
        {
            var releaseOnly = Passed(SampleClass, "Release_only", "Release");
            var misspelled = Passed(HealthClass, "Misspelled", "Acceptance", "Acceptence");

            var result = this.maker.FromTestRun(
                Request("Acceptance", 0),
                [releaseOnly, misspelled],
                Recorded([releaseOnly, misspelled]),
                [],
                [],
                this.redaction
            );

            await Assert.That(result.Result).IsEqualTo("Error");
            await Assert
                .That(
                    result.Diagnostics.Any(line =>
                        line.Contains(
                            "Release_only ran in the Acceptance set, but it is typed Release"
                        )
                    )
                )
                .IsTrue();
            await Assert
                .That(
                    result.Diagnostics.Any(line => line.Contains("carries the type 'Acceptence'"))
                )
                .IsTrue();
        }

        [Test]
        public async Task A_test_whose_requests_were_not_recorded_once_makes_the_run_an_error()
        {
            var unrecorded = Passed(SampleClass, "Sample", "Acceptance");
            var twice = Passed(HealthClass, "Health", "Acceptance");

            var result = this.maker.FromTestRun(
                Request("Acceptance", 0),
                [unrecorded, twice],
                [.. Recorded([twice]), .. Recorded([twice])],
                [],
                [],
                this.redaction
            );

            await Assert.That(result.Result).IsEqualTo("Error");
            await Assert
                .That(
                    result.Diagnostics.Any(line =>
                        line.Contains(
                            "kept no record of the requests of AcceptanceTests.Examples.SampleRouteTests.Sample"
                        )
                    )
                )
                .IsTrue();
            await Assert
                .That(result.Diagnostics.Any(line => line.Contains("kept 2 records")))
                .IsTrue();
        }

        [Test]
        public async Task Two_tests_under_one_name_make_the_run_an_error()
        {
            var first = Passed(SampleClass, "Data(1)", "Acceptance");
            var second = Passed(SampleClass, "Data(1)", "Acceptance");

            var result = this.maker.FromTestRun(
                Request("Acceptance", 0),
                [first, second],
                Recorded([first]),
                [],
                [],
                this.redaction
            );

            await Assert.That(result.Result).IsEqualTo("Error");
            await Assert
                .That(
                    result.Diagnostics.Any(line =>
                        line.Contains("ran more than once under one name")
                    )
                )
                .IsTrue();
        }

        [Test]
        public async Task Two_tests_of_different_classes_under_one_name_make_the_run_an_error()
        {
            var sample = Passed(SampleClass, "Answers_ok", "Acceptance");
            var health = Passed(HealthClass, "Answers_ok", "Acceptance");

            var result = this.maker.FromTestRun(
                Request("Acceptance", 0),
                [sample, health],
                Recorded([sample, health]),
                [],
                [],
                this.redaction
            );

            await Assert.That(result.Result).IsEqualTo("Error");
            await Assert.That(result.TestResults).IsNull();
            await Assert
                .That(string.Join("|", result.Diagnostics))
                .IsEqualTo(
                    $"Answers_ok is the name of a test in {HealthClass} and in {SampleClass}, and the result keeps a test by its name alone: give each a name of its own, so its history can be followed."
                );
        }

        [Test]
        public async Task An_error_result_redacts_its_reasons()
        {
            var result = this.maker.Error(
                Request("Acceptance", 0),
                [$"the build said {Token}"],
                this.redaction
            );

            await Assert.That(result.Result).IsEqualTo("Error");
            await Assert.That(result.Diagnostics[0]).IsEqualTo("the build said [redacted]");
            await Assert.That(result.TestResults).IsNull();
        }

        private static AdapterRequest Request(string testType, int testsExitCode)
        {
            return new AdapterRequest
            {
                Command = Arguments.Report,
                TestType = testType,
                TargetUrl = "http://127.0.0.1:9000",
                RunId = "run-9f3k2m7q1x",
                OutPath = "/work/result.json",
                StartedAt = new DateTimeOffset(2026, 12, 3, 9, 41, 7, TimeSpan.Zero),
                CommitId = "b7e2c1a",
                PipelineRunId = "4242",
                TestsExitCode = testsExitCode,
            };
        }

        private static ReportedTest Passed(string className, string testName, params string[] types)
        {
            return new ReportedTest
            {
                ClassName = className,
                TestName = testName,
                Outcome = "Passed",
                DurationMs = 42,
                Types = [.. types],
                Output = string.Empty,
                FailureMessage = string.Empty,
                StackTrace = string.Empty,
            };
        }

        private static List<TestRecording> Recorded(IEnumerable<ReportedTest> tests)
        {
            return tests
                .Select(test => new TestRecording
                {
                    ClassName = test.ClassName,
                    TestName = test.TestName,
                    Requests =
                    [
                        new RecordedExchange
                        {
                            Request = new RecordedRequest
                            {
                                Method = "GET",
                                Url = "/v1/hello",
                                Headers = [],
                                Body = string.Empty,
                            },
                            Response = new RecordedResponse
                            {
                                Status = 200,
                                Headers = [],
                                Body = "{}",
                                LatencyMs = 3,
                            },
                        },
                    ],
                })
                .ToList();
        }
    }
}
