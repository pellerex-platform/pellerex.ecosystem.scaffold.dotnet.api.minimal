using System.Collections.Generic;

namespace AcceptanceTests.ClientHelper.Models
{
    /// <summary>A request a test sent, as the result file keeps it: with every secret already taken out.</summary>
    public class RecordedRequest
    {
        public string Method { get; set; }

        /// <summary>The path and query the request went to, under the run's target.</summary>
        public string Url { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        /// <summary>The body as text, cut at the recording limit; empty when the request had none.</summary>
        public string Body { get; set; }
    }
}
