namespace AcceptanceTests.Adapter
{
    public static partial class AdapterConstants
    {
        /// <summary>
        /// How much of a log goes into an error result: a failed build's error lines, which say what
        /// to fix, or a log's last lines, which say how a broken run ended.
        /// </summary>
        public static class Logs
        {
            /// <summary>What every compiler or build error line holds, as in "Tests.cs(12,5): error CS1002".</summary>
            public const string ErrorLineMark = ": error ";

            /// <summary>The most error lines the result keeps.</summary>
            public const int MostErrorLines = 50;

            /// <summary>How many of a log's last lines the result keeps.</summary>
            public const int LastLines = 30;

            /// <summary>The line that introduces the test run's last lines of output in an error result.</summary>
            public const string TestOutputHeading = "The test run's last lines of output:";
        }
    }
}
