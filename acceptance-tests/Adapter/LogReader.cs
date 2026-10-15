using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static AcceptanceTests.Adapter.AdapterConstants;

namespace AcceptanceTests.Adapter
{
    /// <summary>Reads what a failed build or a broken test run wrote, for the error result.</summary>
    public class LogReader
    {
        /// <summary>Answers a build log's error lines, or its last lines when it has none, or none and why.</summary>
        public (IReadOnlyList<string> lines, string errorCode) ReadBuildErrors(string path)
        {
            var (all, readable) = ReadLines(path);
            if (!readable)
            {
                return (null, ErrorCodes.LogUnreadableCode);
            }

            var errors = all.Where(line =>
                    line.Contains(Logs.ErrorLineMark, StringComparison.Ordinal)
                )
                .Select(line => line.Trim())
                .Distinct(StringComparer.Ordinal)
                .Take(Logs.MostErrorLines)
                .ToList();
            return (
                errors.Count > 0 ? errors : all.TakeLast(Logs.LastLines).ToList(),
                string.Empty
            );
        }

        /// <summary>Answers a log's last lines, or none and why.</summary>
        public (IReadOnlyList<string> lines, string errorCode) ReadLastLines(string path)
        {
            var (all, readable) = ReadLines(path);
            return readable
                ? (all.TakeLast(Logs.LastLines).ToList(), string.Empty)
                : (null, ErrorCodes.LogUnreadableCode);
        }

        // A log is written by the build or the test run, outside the adapter's control: the one
        // place this class catches.
        private static (IReadOnlyList<string> lines, bool readable) ReadLines(string path)
        {
            try
            {
                return (File.ReadAllLines(path), true);
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                return (null, false);
            }
        }
    }
}
