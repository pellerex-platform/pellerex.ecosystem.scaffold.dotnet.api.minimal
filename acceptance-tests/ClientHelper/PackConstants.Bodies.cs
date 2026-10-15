using System.Collections.Generic;

namespace AcceptanceTests.ClientHelper
{
    public static partial class PackConstants
    {
        /// <summary>
        /// How request and response bodies go into a test's record. A text body is kept as written,
        /// cut at a limit with a note saying how much was left out, so one large answer cannot bloat
        /// the result file. Any other body is described rather than kept.
        /// </summary>
        public static class Bodies
        {
            /// <summary>The most characters of one body the record keeps.</summary>
            public const int MaximumLength = 65536;

            /// <summary>What follows a body that was cut at <see cref="MaximumLength"/>.</summary>
            public const string TruncationNoteFormat = "[truncated: {0} more characters]";

            /// <summary>What stands in the record for a body that is not text.</summary>
            public const string NonTextNoteFormat =
                "[a body of {0} bytes of {1}, not text, is not recorded]";

            /// <summary>The media type of a JSON body, whose fields are redacted by name.</summary>
            public const string JsonMediaType = "application/json";

            /// <summary>The ending of a structured JSON media type, as in application/problem+json, redacted as JSON.</summary>
            public const string JsonMediaTypeSuffix = "+json";

            /// <summary>The media type of a form body, whose fields are redacted by name.</summary>
            public const string FormMediaType = "application/x-www-form-urlencoded";

            /// <summary>Media types whose bodies are text, beyond every text/ type.</summary>
            public static readonly IReadOnlyList<string> TextMediaTypes =
            [
                JsonMediaType,
                "application/xml",
                "application/javascript",
                "application/graphql",
                FormMediaType,
            ];

            /// <summary>The start every text media type has.</summary>
            public const string TextMediaTypeStart = "text/";

            /// <summary>Endings that mark a structured text media type, as in application/problem+json.</summary>
            public static readonly IReadOnlyList<string> TextMediaTypeSuffixes =
            [
                JsonMediaTypeSuffix,
                "+xml",
            ];
        }
    }
}
