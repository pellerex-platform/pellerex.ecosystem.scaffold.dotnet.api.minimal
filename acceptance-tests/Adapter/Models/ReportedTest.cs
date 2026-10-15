using System.Collections.Generic;

namespace AcceptanceTests.Adapter.Models
{
    /// <summary>One test as the test framework's report tells it: who it is, its types, and how it went.</summary>
    public class ReportedTest
    {
        /// <summary>The full name of the test's class.</summary>
        public string ClassName { get; set; }

        /// <summary>The test's display name, as the pack declares it.</summary>
        public string TestName { get; set; }

        /// <summary>The report's word for its outcome, such as Passed, Failed or NotExecuted.</summary>
        public string Outcome { get; set; }

        public long DurationMs { get; set; }

        /// <summary>The values of the test's Type property, as the pack marks it.</summary>
        public List<string> Types { get; set; }

        /// <summary>Everything the test wrote while it ran, its hooks' lines included.</summary>
        public string Output { get; set; }

        /// <summary>The failure's message: expected against actual. Empty when the test passed.</summary>
        public string FailureMessage { get; set; }

        /// <summary>Where in the pack the failure was raised. Empty when the test passed.</summary>
        public string StackTrace { get; set; }
    }
}
