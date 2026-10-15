using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
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
    public class AdapterServiceTests
    {
        private const string Token = "token-of-the-test-identity";
        private const string SampleClass = "AcceptanceTests.Examples.SampleRouteTests";

        [Test]
        public async Task A_finished_green_run_is_written_and_ends_with_0()
        {
            using var run = new TemporaryFolder();
            WriteRun(run, "Passed");

            var (exitCode, result, log) = Adapt(run, Request(run, 0));

            await Assert.That(exitCode).IsEqualTo(ExitCodes.Green);
            await Assert.That(result["Result"].GetValue<string>()).IsEqualTo("Green");
            await Assert
                .That(result["TestResults"][0]["Requests"][0]["Request"]["Url"].GetValue<string>())
                .IsEqualTo("/v1/hello");
            await Assert.That(log).Contains("is Green: 1 passed, 0 failed");
        }

        [Test]
        public async Task A_finished_red_run_ends_with_1_and_its_logs_carry_no_secret_of_the_credential_folder()
        {
            using var run = new TemporaryFolder();
            WriteRun(run, "Failed", stdOut: $"sent {Token}\n");
            Directory.CreateDirectory(run.Inside("credentials"));
            File.WriteAllText(
                run.Inside(Path.Combine("credentials", PackConstants.Inputs.AccessTokenFileName)),
                Token
            );
            var environment = new Dictionary<string, string>
            {
                [PackConstants.Inputs.CredentialsVariable] = run.Inside("credentials"),
            };

            var (exitCode, result, _) = Adapt(run, Request(run, 2), environment);

            await Assert.That(exitCode).IsEqualTo(ExitCodes.Red);
            await Assert
                .That(result["TestResults"][0]["Logs"][0].GetValue<string>())
                .IsEqualTo("sent [redacted]");
            await Assert.That(result.ToJsonString()).DoesNotContain(Token);
        }

        [Test]
        public async Task A_credential_folder_that_cannot_be_read_makes_an_error_result()
        {
            using var run = new TemporaryFolder();
            WriteRun(run, "Passed");
            var environment = new Dictionary<string, string>
            {
                [PackConstants.Inputs.CredentialsVariable] = run.Inside("missing"),
            };

            var (exitCode, result, _) = Adapt(run, Request(run, 0), environment);

            await Assert.That(exitCode).IsEqualTo(ExitCodes.Error);
            await Assert
                .That(result["Diagnostics"][0].GetValue<string>())
                .StartsWith("The result cannot be made safe to keep");
        }

        [Test]
        public async Task A_run_whose_report_is_missing_is_an_error_that_says_how_the_test_run_ended()
        {
            using var run = new TemporaryFolder();
            Directory.CreateDirectory(run.Inside("recordings"));
            File.WriteAllLines(
                run.Inside("tests.log"),
                ["Unhandled exception: the pipe path is too long"]
            );

            var (exitCode, result, _) = Adapt(run, Request(run, 134));

            await Assert.That(exitCode).IsEqualTo(ExitCodes.Error);
            await Assert
                .That(result["Diagnostics"][0].GetValue<string>())
                .IsEqualTo(ErrorCodes.TestReportMissingMessage);
            await Assert
                .That(result["Diagnostics"][1].GetValue<string>())
                .Contains("stopped by signal 6");
            await Assert
                .That(result["Diagnostics"][3].GetValue<string>())
                .Contains("the pipe path is too long");
        }

        [Test]
        public async Task Records_that_cannot_be_read_make_an_error_result()
        {
            using var run = new TemporaryFolder();
            WriteRun(run, "Passed");
            File.WriteAllText(
                run.Inside(
                    Path.Combine(
                        "recordings",
                        PackConstants.Recordings.TestFilePrefix + "broken.json"
                    )
                ),
                "{ not json"
            );

            var (exitCode, result, _) = Adapt(run, Request(run, 0));

            await Assert.That(exitCode).IsEqualTo(ExitCodes.Error);
            await Assert
                .That(result["Diagnostics"][0].GetValue<string>())
                .IsEqualTo(PackConstants.ErrorCodes.RecordingUnreadableMessage);
        }

        [Test]
        public async Task A_pack_that_did_not_build_is_an_error_naming_what_to_fix()
        {
            using var run = new TemporaryFolder();
            File.WriteAllLines(
                run.Inside("build.log"),
                ["  Examples/HelloTests.cs(3,1): error CS1002: ; expected [AcceptanceTests.csproj]"]
            );
            var request = Request(run, 0);
            request.Command = Arguments.BuildFailed;
            request.BuildLogPath = run.Inside("build.log");

            var (exitCode, result, _) = Adapt(run, request);

            await Assert.That(exitCode).IsEqualTo(ExitCodes.Error);
            await Assert
                .That(result["Diagnostics"][0].GetValue<string>())
                .IsEqualTo("The pack did not build, so no test ran.");
            await Assert
                .That(result["Diagnostics"][1].GetValue<string>())
                .StartsWith("Examples/HelloTests.cs(3,1): error CS1002");
        }

        [Test]
        public async Task A_result_that_cannot_be_written_ends_with_2()
        {
            using var run = new TemporaryFolder();
            WriteRun(run, "Passed");
            var request = Request(run, 0);
            request.OutPath = run.Inside(Path.Combine("missing", "result.json"));

            var log = new StringWriter();
            var exitCode = MakeService(log).Run(request, _ => null);

            await Assert.That(exitCode).IsEqualTo(ExitCodes.Error);
            await Assert.That(log.ToString()).Contains(ErrorCodes.ResultUnwritableMessage);
        }

        private static void WriteRun(TemporaryFolder run, string outcome, string stdOut = "")
        {
            var entry = new TrxEntry
            {
                ClassName = SampleClass,
                TestName = "Sample",
                Outcome = outcome,
                StdOut = stdOut,
                Message = outcome == "Passed" ? string.Empty : "Expected 110 but was 111",
                StackTrace = outcome == "Passed" ? string.Empty : "at SampleRouteTests.Sample()",
            };
            File.WriteAllText(run.Inside("tests.trx"), TrxReportWriter.Write([entry]));
            Directory.CreateDirectory(run.Inside("recordings"));
            new RecordingsFolder(run.Inside("recordings")).WriteTestRecording(
                new TestRecording
                {
                    ClassName = SampleClass,
                    TestName = "Sample",
                    Requests =
                    [
                        new RecordedExchange
                        {
                            Request = new RecordedRequest
                            {
                                Method = "GET",
                                Url = "/v1/hello",
                                Headers = [],
                                Body = "",
                            },
                            Response = null,
                        },
                    ],
                }
            );
        }

        private static AdapterRequest Request(TemporaryFolder run, int testsExitCode)
        {
            return new AdapterRequest
            {
                Command = Arguments.Report,
                TestType = "Acceptance",
                TargetUrl = "http://127.0.0.1:9000",
                RunId = "run-9f3k2m7q1x",
                OutPath = run.Inside("result.json"),
                StartedAt = new System.DateTimeOffset(2026, 12, 3, 9, 41, 7, System.TimeSpan.Zero),
                CommitId = string.Empty,
                PipelineRunId = string.Empty,
                TestReportPath = run.Inside("tests.trx"),
                RecordingsFolder = run.Inside("recordings"),
                TestsExitCode = testsExitCode,
                TestLogPath = run.Inside("tests.log"),
            };
        }

        private static (int exitCode, JsonObject result, string log) Adapt(
            TemporaryFolder run,
            AdapterRequest request,
            Dictionary<string, string> environment = null
        )
        {
            var log = new StringWriter();
            var exitCode = MakeService(log)
                .Run(request, name => environment?.GetValueOrDefault(name));
            return (
                exitCode,
                JsonNode.Parse(File.ReadAllText(request.OutPath)).AsObject(),
                log.ToString()
            );
        }

        private static AdapterService MakeService(StringWriter log)
        {
            return new AdapterService(
                new ResultMaker(System.TimeProvider.System),
                new TestReportReader(),
                new LogReader(),
                new ResultFileWriter(),
                log
            );
        }
    }
}
