namespace AcceptanceTests.ClientHelper.Models
{
    /// <summary>
    /// One request a test made and the response it received. The response is empty when none came,
    /// as when the app could not be reached or the request ran out of time; the test's failure then
    /// says why.
    /// </summary>
    public class RecordedExchange
    {
        public RecordedRequest Request { get; set; }

        public RecordedResponse Response { get; set; }
    }
}
