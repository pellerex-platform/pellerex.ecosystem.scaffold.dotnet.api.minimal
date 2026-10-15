using System.Collections.Generic;

namespace AcceptanceTests.ClientHelper
{
    public static partial class PackConstants
    {
        /// <summary>
        /// The two test types. Every test carries one or both, as [Property("Type", "Release")] and
        /// [Property("Type", "Acceptance")], and a run executes exactly the tests of the type it is
        /// asked for. Release tests are the only ones that ever call a deployed copy of the app, so
        /// they must be safe on live; Acceptance tests run only against a local copy.
        /// </summary>
        public static class TestTypes
        {
            /// <summary>The name of the property that carries a test's type.</summary>
            public const string PropertyName = "Type";

            public const string Release = "Release";

            public const string Acceptance = "Acceptance";

            /// <summary>Every type, in the order the result file lists a test's types.</summary>
            public static readonly IReadOnlyList<string> All = [Release, Acceptance];
        }
    }
}
