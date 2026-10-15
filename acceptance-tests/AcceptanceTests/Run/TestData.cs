using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTests.ClientHelper;
using TUnit.Core;

namespace AcceptanceTests.Run
{
    /// <summary>
    /// The three rules every set-up and tear-down keeps. Create through the front door: a test
    /// creates what it needs by calling the app's API, never by writing to its database. Own what
    /// you create: every record a run creates is named with <see cref="Name"/>, so it carries the
    /// run id, parallel runs never collide, and support can find it if a tear-down ever fails. Leave
    /// no trace: a test registers each record it creates with <see cref="RemoveAfterTest"/>, and
    /// tear-down removes them all after the test, newest first, even when the test failed.
    /// </summary>
    public static class TestData
    {
        private static readonly ConcurrentDictionary<
            TestContext,
            ConcurrentStack<PendingRemoval>
        > Removals = new();

        private static int lastNumber;

        /// <summary>
        /// Names a record this test is about to create: its kind, the run id, and a number no other
        /// record of the run shares, as in dsp-run-9f3k2m7q1x-01 for a dispute.
        /// </summary>
        public static string Name(string kind)
        {
            var (name, errorCode) = RunNaming.MakeDataName(
                kind,
                TestRun.RunId,
                Interlocked.Increment(ref lastNumber)
            );
            if (errorCode.Length > 0)
            {
                throw new ArgumentException(
                    PackConstants.ErrorCodes.Messages[errorCode],
                    nameof(kind)
                );
            }

            return name;
        }

        /// <summary>
        /// Registers a record this test created, and the request that removes it through the app's
        /// API. Tear-down sends it after the test, whatever the test's outcome; an answer of 404 or
        /// 410 counts as removed, since the test may have removed the record itself.
        /// </summary>
        public static void RemoveAfterTest(
            string name,
            Func<HttpClient, Task<HttpResponseMessage>> remove
        )
        {
            var test =
                TestContext.Current
                ?? throw new InvalidOperationException(
                    "RemoveAfterTest was called outside a test; call it inside the test that created the record, so tear-down knows whose record it is."
                );
            Removals
                .GetOrAdd(test, _ => new ConcurrentStack<PendingRemoval>())
                .Push(new PendingRemoval { Name = name, Remove = remove });
            test.Output.WriteLine($"Set-up: {name} is registered for tear-down");
        }

        /// <summary>Takes the test's registered records, newest first, leaving none behind.</summary>
        public static IReadOnlyList<PendingRemoval> TakeRemovalsFor(TestContext test)
        {
            return Removals.TryRemove(test, out var pending) ? pending.ToList() : [];
        }
    }
}
