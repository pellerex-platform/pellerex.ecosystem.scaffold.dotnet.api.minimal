using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AcceptanceTests.ClientHelper;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.PackTests.ClientHelper
{
    public class RunNamingTests
    {
        private const string RunId = "run-9f3k2m7q1x";

        [Test]
        [Arguments("run-9f3k2m7q1x")]
        [Arguments("run-0000000000")]
        [Arguments("run-zzzzzzzzzz")]
        public async Task A_run_id_is_run_and_ten_lower_case_letters_or_digits(string value)
        {
            await Assert.That(RunNaming.IsRunId(value)).IsTrue();
        }

        [Test]
        [Arguments("")]
        [Arguments("run-9f3k2m")]
        [Arguments("run-9f3k2m7q1")]
        [Arguments("run-9f3k2m7q1xy")]
        [Arguments("run-9F3K2M7Q1X")]
        [Arguments("RUN-9f3k2m7q1x")]
        [Arguments("run_9f3k2m7q1x")]
        [Arguments("run-9f3k2m7q1x\n")]
        [Arguments(" run-9f3k2m7q1x")]
        [Arguments("dsp-run-9f3k2m7q1x-01")]
        public async Task Anything_else_is_not_a_run_id(string value)
        {
            await Assert.That(RunNaming.IsRunId(value)).IsFalse();
        }

        [Test]
        public async Task No_value_is_not_a_run_id()
        {
            await Assert.That(RunNaming.IsRunId(null)).IsFalse();
        }

        [Test]
        public async Task A_made_run_id_follows_the_rule_and_differs_every_time()
        {
            var made = Enumerable.Range(0, 200).Select(_ => RunNaming.MakeRunId()).ToList();

            await Assert.That(made.All(RunNaming.IsRunId)).IsTrue();
            await Assert.That(made.Distinct().Count()).IsEqualTo(made.Count);
        }

        [Test]
        public async Task A_record_name_is_its_kind_the_run_id_and_a_two_digit_number()
        {
            var (name, errorCode) = RunNaming.MakeDataName("dsp", RunId, 1);

            await Assert.That(errorCode).IsEqualTo(string.Empty);
            await Assert.That(name).IsEqualTo("dsp-run-9f3k2m7q1x-01");
            await Assert.That(RunNaming.IsDataName(name)).IsTrue();
        }

        [Test]
        public async Task A_record_number_past_ninety_nine_keeps_every_digit()
        {
            var (name, _) = RunNaming.MakeDataName("pay2", RunId, 100);

            await Assert.That(name).IsEqualTo("pay2-run-9f3k2m7q1x-100");
            await Assert.That(RunNaming.IsDataName(name)).IsTrue();
        }

        [Test]
        [Arguments("")]
        [Arguments("Dsp")]
        [Arguments("1sp")]
        [Arguments("ds-p")]
        [Arguments("ds_p")]
        [Arguments("dsp ")]
        public async Task A_kind_that_cannot_start_a_record_name_is_refused(string kind)
        {
            var (name, errorCode) = RunNaming.MakeDataName(kind, RunId, 1);

            await Assert.That(name).IsNull();
            await Assert.That(errorCode).IsEqualTo(ErrorCodes.DataKindInvalidCode);
        }

        [Test]
        public async Task A_record_cannot_be_named_for_a_value_that_is_not_a_run_id()
        {
            var (_, errorCode) = RunNaming.MakeDataName("dsp", "run-short", 1);

            await Assert.That(errorCode).IsEqualTo(ErrorCodes.RunIdInvalidCode);
        }

        [Test]
        [Arguments(0)]
        [Arguments(-1)]
        public async Task A_record_number_counts_from_one(int number)
        {
            var (_, errorCode) = RunNaming.MakeDataName("dsp", RunId, number);

            await Assert.That(errorCode).IsEqualTo(ErrorCodes.DataNumberInvalidCode);
        }

        [Test]
        public async Task The_patterns_are_the_platforms_rule_word_for_word()
        {
            // The same rule as the platform's own copy, so a name the pack makes is one support can find.
            await Assert.That(RunIds.RunIdPattern).IsEqualTo(@"^run-[0-9a-z]{10}\z");
            await Assert
                .That(RunIds.DataNamePattern)
                .IsEqualTo(@"^[a-z][a-z0-9]*-run-[0-9a-z]{10}-[0-9]{2,}\z");
            await Assert
                .That(Regex.IsMatch("dsp-run-9f3k2m7q1x-01\n", RunIds.DataNamePattern))
                .IsFalse();
        }
    }
}
