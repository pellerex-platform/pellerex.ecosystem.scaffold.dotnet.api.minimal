using TUnit.Core.Interfaces;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.Run
{
    /// <summary>How many tests run at once, so a Release run stays light on the live app.</summary>
    public record TestsAtOnceLimit : IParallelLimit
    {
        public int Limit => Client.MostTestsAtOnce;
    }
}
