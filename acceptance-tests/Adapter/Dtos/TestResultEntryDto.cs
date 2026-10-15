using System.Collections.Generic;
using AcceptanceTests.ClientHelper.Models;

namespace AcceptanceTests.Adapter.Dtos
{
    /// <summary>
    /// One executed test in full, passed or failed: its name, its types, its outcome and time, its
    /// own log, why it failed and where, and every request it made with the response it got.
    /// </summary>
    public class TestResultEntryDto
    {
        public string Name { get; set; }

        public List<string> TestType { get; set; }

        public string Outcome { get; set; }

        public long DurationMs { get; set; }

        public List<string> Logs { get; set; }

        public string FailureReason { get; set; }

        public string StackTrace { get; set; }

        public List<RecordedExchange> Requests { get; set; }
    }
}
