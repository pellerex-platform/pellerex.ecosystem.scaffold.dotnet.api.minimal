using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using AcceptanceTests.ClientHelper.Models;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.ClientHelper
{
    /// <summary>
    /// The folder between the tests and the adapter: the tests write each test's record and any
    /// problem they found with the run itself, and the adapter reads them once the tests are done.
    /// </summary>
    public class RecordingsFolder
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private readonly string folder;

        public RecordingsFolder(string folder)
        {
            this.folder = folder;
        }

        /// <summary>Writes one test's record, in a file of its own so tests running side by side never share one.</summary>
        public string WriteTestRecording(TestRecording recording)
        {
            var path = Path.Combine(
                this.folder,
                Recordings.TestFilePrefix
                    + Guid.NewGuid().ToString("N")
                    + Recordings.TestFileExtension
            );
            return Write(path, JsonSerializer.Serialize(recording, JsonOptions))
                ? string.Empty
                : ErrorCodes.RecordingUnwritableCode;
        }

        /// <summary>Writes one problem the pack found with the run itself, which makes the run an error.</summary>
        public string WriteProblem(string problem)
        {
            var path = Path.Combine(
                this.folder,
                Recordings.ProblemFilePrefix
                    + Guid.NewGuid().ToString("N")
                    + Recordings.ProblemFileExtension
            );
            return Write(path, problem) ? string.Empty : ErrorCodes.ProblemUnwritableCode;
        }

        /// <summary>Reads every test's record, or none and why when a record cannot be read whole.</summary>
        public (IReadOnlyList<TestRecording> recordings, string errorCode) ReadTestRecordings()
        {
            var (texts, errorCode) = this.ReadAll(
                Recordings.TestFilePrefix,
                Recordings.TestFileExtension
            );
            if (errorCode.Length > 0)
            {
                return (null, errorCode);
            }

            var recordings = new List<TestRecording>();
            foreach (var text in texts)
            {
                var recording = Deserialize(text);
                if (
                    recording?.ClassName is null
                    || recording.TestName is null
                    || recording.Requests is null
                )
                {
                    return (null, ErrorCodes.RecordingUnreadableCode);
                }

                recordings.Add(recording);
            }

            return (recordings, string.Empty);
        }

        /// <summary>Reads every problem the pack wrote, in a stable order.</summary>
        public (IReadOnlyList<string> problems, string errorCode) ReadProblems()
        {
            return this.ReadAll(Recordings.ProblemFilePrefix, Recordings.ProblemFileExtension);
        }

        // The folder sits on a disk outside the pack's control: the one place this class catches
        // what reading or writing it throws.
        private static bool Write(string path, string text)
        {
            try
            {
                File.WriteAllText(path, text);
                return true;
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        private (IReadOnlyList<string> texts, string errorCode) ReadAll(
            string prefix,
            string extension
        )
        {
            try
            {
                var texts = Directory
                    .EnumerateFiles(this.folder, prefix + "*" + extension)
                    .Order(StringComparer.Ordinal)
                    .Select(File.ReadAllText)
                    .ToList();
                return (texts, string.Empty);
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                return (null, ErrorCodes.RecordsFolderUnreadableCode);
            }
        }

        // A record is written by the pack, but a test can break the pack's own code, so a record
        // may not be JSON at all, which the reader reports by throwing.
        private static TestRecording Deserialize(string text)
        {
            try
            {
                return JsonSerializer.Deserialize<TestRecording>(text, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
