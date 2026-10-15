using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTests.Adapter;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static AcceptanceTests.Adapter.AdapterConstants;

namespace AcceptanceTests.PackTests.Adapter
{
    public class CommandLineReaderTests
    {
        private static readonly string[] Shared =
        [
            "--type",
            "Acceptance",
            "--target",
            "http://127.0.0.1:9000",
            "--run-id",
            "run-9f3k2m7q1x",
            "--out",
            "/work/result.json",
            "--started-at",
            "2026-12-03T09:41:07Z",
            "--commit",
            "b7e2c1a",
            "--pipeline-run-id",
            "",
        ];

        private static readonly string[] Report =
        [
            "report",
            .. Shared,
            "--test-report",
            "/work/tests.trx",
            "--recordings",
            "/work/recordings",
            "--tests-exit-code",
            "2",
            "--test-log",
            "/work/tests.log",
        ];

        [Test]
        public async Task A_report_command_line_is_read_whole()
        {
            var (request, errorCode) = new CommandLineReader().Read(Report);

            await Assert.That(errorCode).IsEqualTo(string.Empty);
            await Assert.That(request.Command).IsEqualTo(Arguments.Report);
            await Assert.That(request.TestType).IsEqualTo("Acceptance");
            await Assert.That(request.TargetUrl).IsEqualTo("http://127.0.0.1:9000");
            await Assert.That(request.RunId).IsEqualTo("run-9f3k2m7q1x");
            await Assert.That(request.OutPath).IsEqualTo("/work/result.json");
            await Assert.That(request.StartedAt.ToUnixTimeSeconds()).IsEqualTo(1796290867L);
            await Assert.That(request.CommitId).IsEqualTo("b7e2c1a");
            await Assert.That(request.PipelineRunId).IsEqualTo(string.Empty);
            await Assert.That(request.TestReportPath).IsEqualTo("/work/tests.trx");
            await Assert.That(request.RecordingsFolder).IsEqualTo("/work/recordings");
            await Assert.That(request.TestsExitCode).IsEqualTo(2);
            await Assert.That(request.TestLogPath).IsEqualTo("/work/tests.log");
        }

        [Test]
        public async Task A_build_failed_command_line_is_read_whole()
        {
            var (request, errorCode) = new CommandLineReader().Read([
                "build-failed",
                .. Shared,
                "--build-log",
                "/work/build.log",
            ]);

            await Assert.That(errorCode).IsEqualTo(string.Empty);
            await Assert.That(request.Command).IsEqualTo(Arguments.BuildFailed);
            await Assert.That(request.BuildLogPath).IsEqualTo("/work/build.log");
        }

        [Test]
        public async Task An_unknown_or_missing_command_is_refused()
        {
            await Assert
                .That(new CommandLineReader().Read([]).errorCode)
                .IsEqualTo(ErrorCodes.CommandUnknownCode);
            await Assert
                .That(new CommandLineReader().Read(["verify", .. Shared]).errorCode)
                .IsEqualTo(ErrorCodes.CommandUnknownCode);
        }

        [Test]
        public async Task An_argument_the_command_does_not_take_is_refused()
        {
            await Assert
                .That(new CommandLineReader().Read([.. Report, "--verbose", "yes"]).errorCode)
                .IsEqualTo(ErrorCodes.ArgumentUnknownCode);
            await Assert
                .That(
                    new CommandLineReader()
                        .Read(["build-failed", .. Shared, "--build-log", "a", "--test-log", "b"])
                        .errorCode
                )
                .IsEqualTo(ErrorCodes.ArgumentUnknownCode);
        }

        [Test]
        public async Task An_argument_without_its_value_is_refused_unless_it_may_be_empty()
        {
            await Assert
                .That(new CommandLineReader().Read([.. Report.Take(Report.Length - 1)]).errorCode)
                .IsEqualTo(ErrorCodes.ArgumentValueMissingCode);
            await Assert
                .That(new CommandLineReader().Read(Replace(Report, "--target", "")).errorCode)
                .IsEqualTo(ErrorCodes.ArgumentValueMissingCode);
            await Assert
                .That(new CommandLineReader().Read(Replace(Report, "--commit", "")).errorCode)
                .IsEqualTo(string.Empty);
        }

        [Test]
        public async Task An_argument_given_twice_or_one_left_out_is_refused()
        {
            await Assert
                .That(new CommandLineReader().Read([.. Report, "--type", "Release"]).errorCode)
                .IsEqualTo(ErrorCodes.ArgumentRepeatedCode);
            await Assert
                .That(new CommandLineReader().Read(Remove(Report, "--recordings")).errorCode)
                .IsEqualTo(ErrorCodes.ArgumentMissingCode);
        }

        [Test]
        [Arguments("--type", "release", ErrorCodes.TestTypeInvalidCode)]
        [Arguments("--type", "Both", ErrorCodes.TestTypeInvalidCode)]
        [Arguments("--run-id", "run-9f3k2m", ErrorCodes.RunIdInvalidCode)]
        [Arguments("--started-at", "2026-12-03 09:41:07", ErrorCodes.StartedAtInvalidCode)]
        [Arguments("--tests-exit-code", "-1", ErrorCodes.TestsExitCodeInvalidCode)]
        [Arguments("--tests-exit-code", "256", ErrorCodes.TestsExitCodeInvalidCode)]
        [Arguments("--tests-exit-code", "two", ErrorCodes.TestsExitCodeInvalidCode)]
        public async Task A_value_that_breaks_its_rule_is_refused(
            string name,
            string value,
            string expected
        )
        {
            await Assert
                .That(new CommandLineReader().Read(Replace(Report, name, value)).errorCode)
                .IsEqualTo(expected);
        }

        private static string[] Replace(string[] arguments, string name, string value)
        {
            var replaced = arguments.ToArray();
            replaced[System.Array.IndexOf(replaced, name) + 1] = value;
            return replaced;
        }

        private static string[] Remove(string[] arguments, string name)
        {
            var kept = new List<string>(arguments);
            var index = kept.IndexOf(name);
            kept.RemoveRange(index, 2);
            return [.. kept];
        }
    }
}
