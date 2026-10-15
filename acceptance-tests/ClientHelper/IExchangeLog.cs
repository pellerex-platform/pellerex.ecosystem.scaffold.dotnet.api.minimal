using AcceptanceTests.ClientHelper.Models;

namespace AcceptanceTests.ClientHelper
{
    /// <summary>
    /// Where <see cref="RecordingHandler"/> puts each request and response, already redacted. The
    /// pack's implementation keeps each test's own, so its record holds exactly what it sent.
    /// </summary>
    public interface IExchangeLog
    {
        /// <summary>
        /// Keeps one request for the test that is running, as it goes out. Its response is filled
        /// in on the same exchange when it comes, and stays empty when none does. The time the
        /// request was sent, as a <see cref="System.Diagnostics.Stopwatch"/> timestamp, puts a
        /// test's requests in the order it sent them, even when it sent several at once.
        /// </summary>
        void Add(RecordedExchange exchange, long sentAt);
    }
}
