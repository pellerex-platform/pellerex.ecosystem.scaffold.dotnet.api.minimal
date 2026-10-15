using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AcceptanceTests.Run
{
    /// <summary>A record a test created, and how tear-down removes it through the app's own API.</summary>
    public class PendingRemoval
    {
        /// <summary>The record's name, which carries the run id.</summary>
        public string Name { get; set; }

        /// <summary>The request that removes the record, sent with the run's client.</summary>
        public Func<HttpClient, Task<HttpResponseMessage>> Remove { get; set; }
    }
}
