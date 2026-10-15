using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AcceptanceTests.ClientHelper;
using AcceptanceTests.ClientHelper.Models;

namespace AcceptanceTests.PackTests.StandIns
{
    /// <summary>An exchange log that keeps everything it is given, in the order it was given, for a test to read.</summary>
    public class RecordingExchangeLog : IExchangeLog
    {
        private readonly ConcurrentQueue<(RecordedExchange exchange, long sentAt)> kept = new();

        public IReadOnlyList<RecordedExchange> Exchanges =>
            this.kept.Select(entry => entry.exchange).ToList();

        public IReadOnlyList<long> SentAt => this.kept.Select(entry => entry.sentAt).ToList();

        public void Add(RecordedExchange exchange, long sentAt)
        {
            this.kept.Enqueue((exchange, sentAt));
        }
    }
}
