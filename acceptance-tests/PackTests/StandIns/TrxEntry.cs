using System.Collections.Generic;

namespace AcceptanceTests.PackTests.StandIns
{
    /// <summary>One test as a stand-in TRX report lists it.</summary>
    public class TrxEntry
    {
        public string ClassName { get; set; } = "AcceptanceTests.Examples.SampleRouteTests";

        public string TestName { get; set; }

        public string Outcome { get; set; } = "Passed";

        public string Duration { get; set; } = "00:00:00.0420000";

        public List<string> Types { get; set; } = ["Release", "Acceptance"];

        public string StdOut { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string StackTrace { get; set; } = string.Empty;
    }
}
