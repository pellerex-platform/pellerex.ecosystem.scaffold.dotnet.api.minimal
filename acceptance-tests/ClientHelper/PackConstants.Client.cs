using System;

namespace AcceptanceTests.ClientHelper
{
    public static partial class PackConstants
    {
        /// <summary>How every request a test sends goes out.</summary>
        public static class Client
        {
            /// <summary>How long one request may take before the test sees it fail: no wait is endless.</summary>
            public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

            /// <summary>The scheme the test identity's token is sent with.</summary>
            public const string BearerScheme = "Bearer";

            /// <summary>
            /// The user agent every request carries, with the run id, so the app's own logs show which
            /// requests were test traffic and which run sent them, even when the run is unauthenticated.
            /// </summary>
            public const string UserAgentFormat = "pellerex-acceptance-tests ({0})";

            /// <summary>
            /// The most tests that run at once. The Release set runs against the copy of the app
            /// customers use, so a run stays light: a few requests at a time, never a load test.
            /// </summary>
            public const int MostTestsAtOnce = 4;
        }
    }
}
