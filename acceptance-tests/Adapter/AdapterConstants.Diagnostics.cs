namespace AcceptanceTests.Adapter
{
    public static partial class AdapterConstants
    {
        /// <summary>The fixed sentences an error result's diagnostics open with.</summary>
        public static class Diagnostics
        {
            /// <summary>The first line of the error result of a pack that did not build; its build's error lines follow.</summary>
            public const string PackDidNotBuild = "The pack did not build, so no test ran.";

            /// <summary>The line of an error result whose credential folder could not be read, so nothing in it could be redacted.</summary>
            public const string ResultCannotBeMadeSafeFormat =
                "The result cannot be made safe to keep: {0}";
        }
    }
}
