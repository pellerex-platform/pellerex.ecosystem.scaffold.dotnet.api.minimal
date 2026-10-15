using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using AcceptanceTests.Adapter.Dtos;
using static AcceptanceTests.Adapter.AdapterConstants;

namespace AcceptanceTests.Adapter
{
    /// <summary>
    /// Writes the result file, indented like the contract's worked example, with only what JSON
    /// itself requires escaped, so the pack's own text reads as it was written.
    /// </summary>
    public class ResultFileWriter
    {
        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>Writes the result to the path run.sh named, answering an error code when it cannot.</summary>
        public string Write(string path, TestRunResultDto result)
        {
            // The path is the invoker's, on a disk outside the adapter's control: the one place
            // this class catches.
            try
            {
                File.WriteAllText(path, JsonSerializer.Serialize(result, WriteOptions));
                return string.Empty;
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                return ErrorCodes.ResultUnwritableCode;
            }
        }
    }
}
