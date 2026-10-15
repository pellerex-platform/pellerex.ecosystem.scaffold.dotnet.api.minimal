using AcceptanceTests.Run;
using TUnit.Core;

[assembly: ParallelLimiter<TestsAtOnceLimit>]
[assembly: EveryTestCarriesAType]
