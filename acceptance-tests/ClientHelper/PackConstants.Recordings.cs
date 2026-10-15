namespace AcceptanceTests.ClientHelper
{
    public static partial class PackConstants
    {
        /// <summary>
        /// The files in the records folder, which the tests write and the adapter reads: one record
        /// per test with every request it made, and one file per problem the pack found with the run
        /// itself, such as a test that carries no type. A problem makes the run an error, never red.
        /// </summary>
        public static class Recordings
        {
            public const string TestFilePrefix = "test-";

            public const string TestFileExtension = ".json";

            public const string ProblemFilePrefix = "problem-";

            public const string ProblemFileExtension = ".txt";
        }
    }
}
