using System;

namespace AcceptanceTests.Adapter.Models
{
    /// <summary>What run.sh asks of the adapter, read from its command line by <see cref="CommandLineReader"/>.</summary>
    public class AdapterRequest
    {
        /// <summary>report or build-failed.</summary>
        public string Command { get; set; }

        /// <summary>The set the run executed: Release or Acceptance.</summary>
        public string TestType { get; set; }

        public string TargetUrl { get; set; }

        public string RunId { get; set; }

        /// <summary>The full path of the result file to write.</summary>
        public string OutPath { get; set; }

        public DateTimeOffset StartedAt { get; set; }

        /// <summary>The commit the pack and the app build came from; empty when not known.</summary>
        public string CommitId { get; set; }

        /// <summary>The pipeline's own run id; empty for a run no pipeline started.</summary>
        public string PipelineRunId { get; set; }

        /// <summary>The test framework's TRX report, for report.</summary>
        public string TestReportPath { get; set; }

        /// <summary>The records folder the tests wrote, for report.</summary>
        public string RecordingsFolder { get; set; }

        /// <summary>The test run's own exit code, for report.</summary>
        public int TestsExitCode { get; set; }

        /// <summary>The test run's console output, for report.</summary>
        public string TestLogPath { get; set; }

        /// <summary>The pack's build log, for build-failed.</summary>
        public string BuildLogPath { get; set; }
    }
}
