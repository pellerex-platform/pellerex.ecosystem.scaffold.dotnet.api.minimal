using System;

namespace AcceptanceTests.PackTests.StandIns
{
    /// <summary>A clock that always says the same time, so a result's finish time can be checked.</summary>
    public class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            this.now = now;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return this.now;
        }
    }
}
