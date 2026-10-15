namespace AcceptanceTests.Adapter
{
    public static partial class AdapterConstants
    {
        /// <summary>
        /// The adapter's command line, which only run.sh calls: report turns a finished test run into
        /// its result file, and build-failed writes the error result of a pack that did not build.
        /// </summary>
        public static class Arguments
        {
            public const string Report = "report";

            public const string BuildFailed = "build-failed";

            public const string Type = "--type";

            public const string Target = "--target";

            public const string RunId = "--run-id";

            public const string Out = "--out";

            public const string StartedAt = "--started-at";

            /// <summary>The commit the pack and the app build came from; empty when the checkout is not a git repository.</summary>
            public const string Commit = "--commit";

            /// <summary>The pipeline's own run id; empty for a run no pipeline started.</summary>
            public const string PipelineRunId = "--pipeline-run-id";

            /// <summary>The test framework's TRX report.</summary>
            public const string TestReport = "--test-report";

            public const string Recordings = "--recordings";

            public const string TestsExitCode = "--tests-exit-code";

            /// <summary>The test run's own console output, whose last lines go into the error result of a run that broke.</summary>
            public const string TestLog = "--test-log";

            public const string BuildLog = "--build-log";

            /// <summary>How to call the adapter, shown when a command line is refused.</summary>
            public const string Usage =
                "usage: adapter report --type <Release|Acceptance> --target <url> --run-id <run id> --out <result file>"
                + " --started-at <utc time> --commit <commit> --pipeline-run-id <id>"
                + " --test-report <trx file> --recordings <folder> --tests-exit-code <code> --test-log <file>\n"
                + "       adapter build-failed --type <Release|Acceptance> --target <url> --run-id <run id> --out <result file>"
                + " --started-at <utc time> --commit <commit> --pipeline-run-id <id> --build-log <file>";
        }
    }
}
