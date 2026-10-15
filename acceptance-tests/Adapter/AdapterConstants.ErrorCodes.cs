using System.Collections.Generic;

namespace AcceptanceTests.Adapter
{
    public static partial class AdapterConstants
    {
        /// <summary>
        /// Every way the adapter can fail, each code paired with the sentence a person reads, and
        /// <see cref="Messages"/> to find the sentence by its code.
        /// </summary>
        public static class ErrorCodes
        {
            // The command line.
            public const string CommandUnknownCode = "adapter_command_unknown";
            public const string CommandUnknownMessage =
                "The first word of the command line is the command: report or build-failed.";

            public const string ArgumentUnknownCode = "adapter_argument_unknown";
            public const string ArgumentUnknownMessage =
                "The command line has an argument this command does not take.";

            public const string ArgumentValueMissingCode = "adapter_argument_value_missing";
            public const string ArgumentValueMissingMessage =
                "An argument is given without its value.";

            public const string ArgumentRepeatedCode = "adapter_argument_repeated";
            public const string ArgumentRepeatedMessage =
                "An argument that is given once is given twice.";

            public const string ArgumentMissingCode = "adapter_argument_missing";
            public const string ArgumentMissingMessage = "A required argument is missing.";

            public const string TestTypeInvalidCode = "adapter_test_type_invalid";
            public const string TestTypeInvalidMessage = "--type is Release or Acceptance.";

            public const string RunIdInvalidCode = "adapter_run_id_invalid";
            public const string RunIdInvalidMessage =
                "--run-id is run- and ten lower-case letters or digits, such as run-9f3k2m7q1x.";

            public const string StartedAtInvalidCode = "adapter_started_at_invalid";
            public const string StartedAtInvalidMessage =
                "--started-at is a UTC time such as 2026-12-03T09:41:07Z.";

            public const string TestsExitCodeInvalidCode = "adapter_tests_exit_code_invalid";
            public const string TestsExitCodeInvalidMessage =
                "--tests-exit-code is the test run's exit code, a whole number from 0 to 255.";

            // What the test run left behind.
            public const string TestReportMissingCode = "adapter_test_report_missing";
            public const string TestReportMissingMessage =
                "The test run wrote no report, so no verdict exists.";

            public const string TestReportUnreadableCode = "adapter_test_report_unreadable";
            public const string TestReportUnreadableMessage =
                "The test run's report cannot be read as a TRX report.";

            public const string LogUnreadableCode = "adapter_log_unreadable";
            public const string LogUnreadableMessage = "A log of the run cannot be read.";

            // The result file.
            public const string ResultUnwritableCode = "adapter_result_unwritable";
            public const string ResultUnwritableMessage = "The result file cannot be written.";

            /// <summary>The sentence for each code, so a code is described the same way wherever it is reported.</summary>
            public static readonly IReadOnlyDictionary<string, string> Messages = new Dictionary<
                string,
                string
            >
            {
                [CommandUnknownCode] = CommandUnknownMessage,
                [ArgumentUnknownCode] = ArgumentUnknownMessage,
                [ArgumentValueMissingCode] = ArgumentValueMissingMessage,
                [ArgumentRepeatedCode] = ArgumentRepeatedMessage,
                [ArgumentMissingCode] = ArgumentMissingMessage,
                [TestTypeInvalidCode] = TestTypeInvalidMessage,
                [RunIdInvalidCode] = RunIdInvalidMessage,
                [StartedAtInvalidCode] = StartedAtInvalidMessage,
                [TestsExitCodeInvalidCode] = TestsExitCodeInvalidMessage,
                [TestReportMissingCode] = TestReportMissingMessage,
                [TestReportUnreadableCode] = TestReportUnreadableMessage,
                [LogUnreadableCode] = LogUnreadableMessage,
                [ResultUnwritableCode] = ResultUnwritableMessage,
            };
        }
    }
}
