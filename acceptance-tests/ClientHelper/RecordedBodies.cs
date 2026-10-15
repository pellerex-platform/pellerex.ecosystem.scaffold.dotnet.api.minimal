using System;
using System.Globalization;
using System.Linq;
using System.Text;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.ClientHelper
{
    /// <summary>How a request or response body goes into a test's record.</summary>
    public static class RecordedBodies
    {
        /// <summary>
        /// Whether a body of this media type is text the record can keep as written. A body with no
        /// media type is taken as text, as most API bodies without one are.
        /// </summary>
        public static bool IsText(string mediaType)
        {
            if (string.IsNullOrEmpty(mediaType))
            {
                return true;
            }

            return mediaType.StartsWith(
                    Bodies.TextMediaTypeStart,
                    StringComparison.OrdinalIgnoreCase
                )
                || Bodies.TextMediaTypes.Any(textType =>
                    string.Equals(textType, mediaType, StringComparison.OrdinalIgnoreCase)
                )
                || Bodies.TextMediaTypeSuffixes.Any(suffix =>
                    mediaType.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                );
        }

        /// <summary>
        /// A text body's text, read in the character set its media type names when .NET knows that
        /// set, and as UTF-8 when it does not, as for an app that writes utf8 for utf-8. .NET's own
        /// reader refuses such a body, and the record must never fail a test the app answered. A
        /// byte order mark at the start is not part of the text.
        /// </summary>
        public static string ReadText(byte[] body, string charSet)
        {
            var name = (charSet ?? string.Empty).Trim('"', ' ');
            var encoding =
                Encoding
                    .GetEncodings()
                    .FirstOrDefault(known =>
                        string.Equals(known.Name, name, StringComparison.OrdinalIgnoreCase)
                    )
                    ?.GetEncoding()
                ?? Encoding.UTF8;
            var preamble = encoding.GetPreamble();
            var start = body.AsSpan().StartsWith(preamble) ? preamble.Length : 0;
            return encoding.GetString(body, start, body.Length - start);
        }

        /// <summary>Cuts a text body at the recording limit, saying how many characters were left out.</summary>
        public static string Cap(string body)
        {
            if (body is null || body.Length <= Bodies.MaximumLength)
            {
                return body ?? string.Empty;
            }

            return body[..Bodies.MaximumLength]
                + string.Format(
                    CultureInfo.InvariantCulture,
                    Bodies.TruncationNoteFormat,
                    body.Length - Bodies.MaximumLength
                );
        }

        /// <summary>What the record keeps for a body that is not text: its size and its media type.</summary>
        public static string DescribeNonText(long length, string mediaType)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                Bodies.NonTextNoteFormat,
                length,
                mediaType
            );
        }
    }
}
