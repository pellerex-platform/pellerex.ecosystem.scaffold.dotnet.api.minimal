using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.ClientHelper
{
    /// <summary>
    /// The run id and the names of the records a run creates, made and checked by the one rule the
    /// whole platform shares, so tear-down and support can tell this run's records from any other's.
    /// </summary>
    public static class RunNaming
    {
        private static readonly Regex RunIdShape = new(RunIds.RunIdPattern);

        private static readonly Regex KindShape = new(RunIds.KindPattern);

        private static readonly Regex DataNameShape = new(RunIds.DataNamePattern);

        /// <summary>Whether the value is a whole run id and nothing else, in lower case.</summary>
        public static bool IsRunId(string value)
        {
            return value is not null && RunIdShape.IsMatch(value);
        }

        /// <summary>
        /// Makes a fresh run id from the system's random source, every character equally likely, for
        /// a run started without one.
        /// </summary>
        public static string MakeRunId()
        {
            return RunIds.Start + RandomNumberGenerator.GetString(RunIds.Characters, RunIds.Length);
        }

        /// <summary>
        /// Names a record a run creates: its kind, the run id and its number, as in
        /// dsp-run-9f3k2m7q1x-01. Answers no name, and why, when the kind or the number cannot be
        /// part of a record's name.
        /// </summary>
        public static (string name, string errorCode) MakeDataName(
            string kind,
            string runId,
            int number
        )
        {
            if (kind is null || !KindShape.IsMatch(kind))
            {
                return (null, ErrorCodes.DataKindInvalidCode);
            }

            if (!IsRunId(runId))
            {
                return (null, ErrorCodes.RunIdInvalidCode);
            }

            if (number < 1)
            {
                return (null, ErrorCodes.DataNumberInvalidCode);
            }

            return (
                $"{kind}-{runId}-{number.ToString("D2", CultureInfo.InvariantCulture)}",
                string.Empty
            );
        }

        /// <summary>Whether the value is the name of a record a run created, by the rule above.</summary>
        public static bool IsDataName(string value)
        {
            return value is not null && DataNameShape.IsMatch(value);
        }
    }
}
