using System.Collections.Generic;

namespace AcceptanceTests.ClientHelper.Models
{
    /// <summary>
    /// One test's record in the records folder: the test, named as the test report names it, and
    /// every request it made, in order, its tear-down's included. The adapter joins it to the test's
    /// entry in the report by the class and the test's name.
    /// </summary>
    public class TestRecording
    {
        /// <summary>The full name of the test's class, as the report's class name.</summary>
        public string ClassName { get; set; }

        /// <summary>The test's display name, as the report's test name.</summary>
        public string TestName { get; set; }

        public List<RecordedExchange> Requests { get; set; }
    }
}
