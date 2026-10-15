namespace AcceptanceTests.ClientHelper
{
    public static partial class PackConstants
    {
        /// <summary>
        /// The environment variables a run's inputs arrive in. run.sh sets the target, the run id and
        /// the records folder from its own arguments. The invoker sets the credential folder, which
        /// never travels as an argument: the stand-up tool answers it for a local run, and the
        /// platform mounts it for a deployed one.
        /// </summary>
        public static class Inputs
        {
            /// <summary>The address of the copy of the app the tests call: run.sh's --target.</summary>
            public const string TargetVariable = "ACCEPTANCE_TESTS_TARGET";

            /// <summary>
            /// The run's id, which every record the run creates carries in its name. run.sh passes the
            /// one it was given or made; a run started any other way makes its own.
            /// </summary>
            public const string RunIdVariable = "ACCEPTANCE_TESTS_RUN_ID";

            /// <summary>
            /// The folder each test's record of requests and responses is written to, for the adapter
            /// to read. A run without it records nothing, which only a run outside run.sh does.
            /// </summary>
            public const string RecordingsVariable = "ACCEPTANCE_TESTS_RECORDINGS";

            /// <summary>
            /// The folder of the test identity's credential, one file per key. A run without it is
            /// unauthenticated, as a run against an app with no sign-in is.
            /// </summary>
            public const string CredentialsVariable = "ACCEPTANCE_TESTS_CREDENTIALS";

            /// <summary>The file in the credential folder that holds the test identity's token, as the stand-up tool names it.</summary>
            public const string AccessTokenFileName = "AccessToken";
        }
    }
}
