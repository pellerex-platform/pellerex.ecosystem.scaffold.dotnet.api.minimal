using System.Collections.Generic;

namespace AcceptanceTests.ClientHelper.Models
{
    /// <summary>The response a test received, as the result file keeps it: with every secret already taken out.</summary>
    public class RecordedResponse
    {
        public int Status { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        /// <summary>The body as text, cut at the recording limit; empty when the response had none.</summary>
        public string Body { get; set; }

        /// <summary>Wall-clock milliseconds from sending the request to the last byte of the response.</summary>
        public long LatencyMs { get; set; }
    }
}
