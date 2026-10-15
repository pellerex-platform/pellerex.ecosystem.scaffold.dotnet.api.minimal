namespace AcceptanceTests.ClientHelper
{
    public static partial class PackConstants
    {
        /// <summary>
        /// The run id and the names of the records a run creates. Every record a run creates carries
        /// the run id in its name, so parallel runs never collide, tear-down knows exactly what is
        /// its own, and support can find by the run id whatever a failed tear-down left behind. The
        /// pack takes no Pellerex library, so this is its own copy of the platform's rule. The
        /// patterns end in \z, not $, because $ also accepts a value with a line break after it.
        /// </summary>
        public static class RunIds
        {
            /// <summary>The start every run id has, which marks engine data at a glance.</summary>
            public const string Start = "run-";

            /// <summary>The characters a run id is made of after its start, each equally likely.</summary>
            public const string Characters = "0123456789abcdefghijklmnopqrstuvwxyz";

            /// <summary>How many characters follow the start.</summary>
            public const int Length = 10;

            // A run id as it stands inside a longer pattern, written once for both patterns below.
            private const string RunIdPatternPart = "run-[0-9a-z]{10}";

            /// <summary>A whole run id, as in run-9f3k2m7q1x.</summary>
            public const string RunIdPattern = "^" + RunIdPatternPart + @"\z";

            /// <summary>
            /// The word a record's name starts with, saying what kind of record it is, such as dsp for
            /// a dispute: a lower-case letter, then lower-case letters or digits.
            /// </summary>
            public const string KindPattern = @"^[a-z][a-z0-9]*\z";

            /// <summary>
            /// The name of a record a run creates: its kind, the run id, and a number of at least two
            /// digits that no other record of the run shares, joined by hyphens, as in
            /// dsp-run-9f3k2m7q1x-01.
            /// </summary>
            public const string DataNamePattern =
                "^[a-z][a-z0-9]*-" + RunIdPatternPart + @"-[0-9]{2,}\z";
        }
    }
}
