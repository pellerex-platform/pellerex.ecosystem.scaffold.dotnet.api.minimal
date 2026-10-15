namespace AcceptanceTests.Adapter
{
    public static partial class AdapterConstants
    {
        /// <summary>
        /// The run's three exit codes, fixed forever: 0 green, 1 red, 2 error. Only 0 lets a gate
        /// proceed. The adapter answers the code that agrees with the result file it wrote, and 2
        /// when it could write none.
        /// </summary>
        public static class ExitCodes
        {
            public const int Green = 0;

            public const int Red = 1;

            public const int Error = 2;
        }
    }
}
