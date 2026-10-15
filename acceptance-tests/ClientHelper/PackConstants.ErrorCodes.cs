using System.Collections.Generic;

namespace AcceptanceTests.ClientHelper
{
    public static partial class PackConstants
    {
        /// <summary>
        /// Every way the client helper can fail, each code paired with the sentence a person reads,
        /// and <see cref="Messages"/> to find the sentence by its code.
        /// </summary>
        public static class ErrorCodes
        {
            // The run inputs.
            public const string TargetMissingCode = "pack_target_missing";
            public const string TargetMissingMessage =
                "The run has no target: run.sh hands its --target to the tests in ACCEPTANCE_TESTS_TARGET.";

            public const string TargetInvalidCode = "pack_target_invalid";
            public const string TargetInvalidMessage =
                "The target is not a full http or https address with no query, such as http://localhost:9000.";

            public const string RunIdInvalidCode = "pack_run_id_invalid";
            public const string RunIdInvalidMessage =
                "The run id is not run- and ten lower-case letters or digits, such as run-9f3k2m7q1x.";

            public const string RecordingsFolderMissingCode = "pack_recordings_folder_missing";
            public const string RecordingsFolderMissingMessage =
                "The folder named in ACCEPTANCE_TESTS_RECORDINGS for the tests' records does not exist.";

            public const string CredentialsFolderMissingCode = "pack_credentials_folder_missing";
            public const string CredentialsFolderMissingMessage =
                "The folder named in ACCEPTANCE_TESTS_CREDENTIALS for the test identity's credential does not exist.";

            public const string CredentialUnreadableCode = "pack_credential_unreadable";
            public const string CredentialUnreadableMessage =
                "The credential folder, or a file in it, cannot be read.";

            // The records the tests write and the adapter reads.
            public const string RecordingUnwritableCode = "pack_recording_unwritable";
            public const string RecordingUnwritableMessage =
                "A test's record of its requests cannot be written to the records folder.";

            public const string RecordingUnreadableCode = "pack_recording_unreadable";
            public const string RecordingUnreadableMessage =
                "A test's record of its requests cannot be read as the record the pack writes.";

            public const string ProblemUnwritableCode = "pack_problem_unwritable";
            public const string ProblemUnwritableMessage =
                "A problem the pack found with the run cannot be written to the records folder.";

            public const string RecordsFolderUnreadableCode = "pack_records_folder_unreadable";
            public const string RecordsFolderUnreadableMessage =
                "The records folder, or a file in it, cannot be read.";

            // The names of the records a run creates.
            public const string DataKindInvalidCode = "pack_data_kind_invalid";
            public const string DataKindInvalidMessage =
                "A record's kind is a short lower-case word: a letter, then letters or digits, such as dsp.";

            public const string DataNumberInvalidCode = "pack_data_number_invalid";
            public const string DataNumberInvalidMessage = "A record's number counts from 1.";

            /// <summary>The sentence for each code, so a code is described the same way wherever it is reported.</summary>
            public static readonly IReadOnlyDictionary<string, string> Messages = new Dictionary<
                string,
                string
            >
            {
                [TargetMissingCode] = TargetMissingMessage,
                [TargetInvalidCode] = TargetInvalidMessage,
                [RunIdInvalidCode] = RunIdInvalidMessage,
                [RecordingsFolderMissingCode] = RecordingsFolderMissingMessage,
                [CredentialsFolderMissingCode] = CredentialsFolderMissingMessage,
                [CredentialUnreadableCode] = CredentialUnreadableMessage,
                [RecordingUnwritableCode] = RecordingUnwritableMessage,
                [RecordingUnreadableCode] = RecordingUnreadableMessage,
                [ProblemUnwritableCode] = ProblemUnwritableMessage,
                [RecordsFolderUnreadableCode] = RecordsFolderUnreadableMessage,
                [DataKindInvalidCode] = DataKindInvalidMessage,
                [DataNumberInvalidCode] = DataNumberInvalidMessage,
            };
        }
    }
}
