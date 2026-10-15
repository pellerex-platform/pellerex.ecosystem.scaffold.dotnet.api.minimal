using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AcceptanceTests.Adapter.Dtos
{
    /// <summary>
    /// The result file every invoker reads: the run header and one entry per executed test, in the
    /// shape every pack writes whatever its test framework, field for field. An error run carries
    /// <see cref="Diagnostics"/> in place of <see cref="TestResults"/>.
    /// </summary>
    public class TestRunResultDto
    {
        public int TestResultFormatVersion { get; set; }

        public string Id { get; set; }

        public string PipelineRunId { get; set; }

        public string ProductId { get; set; }

        public string CommitId { get; set; }

        public string DeploymentMode { get; set; }

        public string TestType { get; set; }

        public string TargetUrl { get; set; }

        public string StartedAt { get; set; }

        public string FinishedAt { get; set; }

        public string Result { get; set; }

        public int SuccessfulTests { get; set; }

        public int FailedTests { get; set; }

        public int TotalTestsRan { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<TestResultEntryDto> TestResults { get; set; }

        /// <summary>
        /// The app's own logs on a red or error run, which the stand-up tool adds when it tears a
        /// local run down. The adapter writes it empty: a deployed app's logs never belong here.
        /// </summary>
        public List<string> AppLogs { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> Diagnostics { get; set; }
    }
}
