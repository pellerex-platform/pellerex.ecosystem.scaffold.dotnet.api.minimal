using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AcceptanceTests.ClientHelper;
using AcceptanceTests.ClientHelper.Models;
using TUnit.Core;

namespace AcceptanceTests.Run
{
    /// <summary>
    /// Keeps each test's requests apart from every other test's, even when tests run side by side,
    /// by the test that is running when a request is sent. A request sent outside any test, such as
    /// in a hook for a whole class, belongs to no test's record and is not kept.
    /// </summary>
    public class TestExchangeLog : IExchangeLog
    {
        /// <summary>The one log the run's client records into.</summary>
        public static readonly TestExchangeLog Instance = new();

        private readonly ConcurrentDictionary<
            TestContext,
            ConcurrentQueue<(long sentAt, RecordedExchange exchange)>
        > exchanges = new();

        /// <summary>Keeps one request for the test that is running, as it goes out; its response follows on the same exchange.</summary>
        public void Add(RecordedExchange exchange, long sentAt)
        {
            var test = TestContext.Current;
            if (test is null)
            {
                return;
            }

            this.exchanges.GetOrAdd(test, _ => new ConcurrentQueue<(long, RecordedExchange)>())
                .Enqueue((sentAt, exchange));
        }

        /// <summary>Takes every request the test made, in the order it sent them, leaving none behind.</summary>
        public List<RecordedExchange> TakeFor(TestContext test)
        {
            if (!this.exchanges.TryRemove(test, out var kept))
            {
                return [];
            }

            return kept.OrderBy(entry => entry.sentAt).Select(entry => entry.exchange).ToList();
        }
    }
}
