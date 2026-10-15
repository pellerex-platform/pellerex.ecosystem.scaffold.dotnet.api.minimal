using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Interfaces;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.Run
{
    /// <summary>
    /// Sees every test the pack holds, whichever set the run selects, and reports each one that
    /// carries no type, or a type that is neither Release nor Acceptance. Such a test would never
    /// run in either set, silently; reported, it makes the run an error until it is typed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class EveryTestCarriesATypeAttribute : Attribute, ITestDiscoveryEventReceiver
    {
        /// <summary>Checks the types of one test as it is discovered.</summary>
        public ValueTask OnTestDiscovered(DiscoveredTestContext context)
        {
            var test = context.TestContext.Metadata;
            var types = test.TestDetails.CustomProperties.TryGetValue(
                TestTypes.PropertyName,
                out var values
            )
                ? values.ToList()
                : [];
            var name = $"{test.TestDetails.ClassType.FullName}.{test.DisplayName}";
            if (types.Count == 0)
            {
                RunProblems.Report(
                    $"{name} carries no type: mark it [Property(\"{TestTypes.PropertyName}\", \"{TestTypes.Release}\")], [Property(\"{TestTypes.PropertyName}\", \"{TestTypes.Acceptance}\")] or both, or it never runs."
                );
            }

            foreach (var unknown in types.Where(type => !TestTypes.All.Contains(type)))
            {
                RunProblems.Report(
                    $"{name} carries the type '{unknown}', which is neither {TestTypes.Release} nor {TestTypes.Acceptance}."
                );
            }

            return ValueTask.CompletedTask;
        }
    }
}
