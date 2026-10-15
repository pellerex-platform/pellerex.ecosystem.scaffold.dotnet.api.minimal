namespace AcceptanceTests.Adapter
{
    public static partial class AdapterConstants
    {
        /// <summary>
        /// The result file's values that are fixed by the contract every pack keeps, whatever its
        /// test framework: the edition of its shape, and the words its fields hold.
        /// </summary>
        public static class ResultFile
        {
            /// <summary>
            /// The edition of the result file's shape this adapter writes: nothing to do with the app
            /// under test. It moves only when the shape gains a field, and every stored run keeps the
            /// number its writer stamped.
            /// </summary>
            public const int TestResultFormatVersion = 1;

            /// <summary>The app's product id, filled in when the repository is made from its template.</summary>
            public const string ProductId = "<marketplace-product-id>";

            /// <summary>Where a run's tests ran. The type is the mode: Acceptance runs are local, Release runs are deployed.</summary>
            public const string LocalDeploymentMode = "Local";

            public const string DeployedDeploymentMode = "Deployed";

            public const string GreenResult = "Green";

            public const string RedResult = "Red";

            public const string ErrorResult = "Error";

            public const string PassedOutcome = "Passed";

            public const string FailedOutcome = "Failed";

            /// <summary>The run's wall-clock bounds, in UTC, as the contract writes them.</summary>
            public const string TimeFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";
        }
    }
}
