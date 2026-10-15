using System;
using System.Collections.Generic;
using System.IO;
using AcceptanceTests.ClientHelper.Models;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.ClientHelper
{
    /// <summary>
    /// Reads a run's inputs from its environment. The target is the only one a run cannot do
    /// without; a run started without a run id makes its own, a run without a records folder
    /// records nothing, and a run without a credential folder is unauthenticated.
    /// </summary>
    public class RunInputsReader
    {
        /// <summary>
        /// Answers the run's inputs, or none and the first one that is wrong. The target answered
        /// ends in a slash, so a test's relative path is kept under a target's own path, such as a
        /// route through the platform's gateway.
        /// </summary>
        public (RunInputs inputs, string errorCode) Read(Func<string, string> variable)
        {
            var (targetUrl, targetError) = ReadTarget(
                variable(Inputs.TargetVariable) ?? string.Empty
            );
            if (targetError.Length > 0)
            {
                return (null, targetError);
            }

            var (runId, runIdError) = ReadRunId(variable(Inputs.RunIdVariable) ?? string.Empty);
            if (runIdError.Length > 0)
            {
                return (null, runIdError);
            }

            var recordingsFolder = variable(Inputs.RecordingsVariable) ?? string.Empty;
            if (recordingsFolder.Length > 0 && !Directory.Exists(recordingsFolder))
            {
                return (null, ErrorCodes.RecordingsFolderMissingCode);
            }

            var (credentials, credentialsError) = ReadCredentials(
                variable(Inputs.CredentialsVariable) ?? string.Empty
            );
            if (credentialsError.Length > 0)
            {
                return (null, credentialsError);
            }

            return (
                new RunInputs
                {
                    TargetUrl = targetUrl,
                    RunId = runId,
                    RecordingsFolder = recordingsFolder,
                    Credentials = credentials,
                },
                string.Empty
            );
        }

        // A full http or https address. A query or a fragment on it would end up before the tests'
        // own paths, so the target has neither.
        private static (Uri targetUrl, string errorCode) ReadTarget(string target)
        {
            if (target.Length == 0)
            {
                return (null, ErrorCodes.TargetMissingCode);
            }

            var usable =
                Uri.TryCreate(target, UriKind.Absolute, out var targetUrl)
                && (targetUrl.Scheme == Uri.UriSchemeHttp || targetUrl.Scheme == Uri.UriSchemeHttps)
                && targetUrl.Query.Length == 0
                && targetUrl.Fragment.Length == 0;
            return usable
                ? (new Uri(targetUrl.AbsoluteUri.TrimEnd('/') + "/"), string.Empty)
                : (null, ErrorCodes.TargetInvalidCode);
        }

        private static (string runId, string errorCode) ReadRunId(string runId)
        {
            if (runId.Length == 0)
            {
                return (RunNaming.MakeRunId(), string.Empty);
            }

            return RunNaming.IsRunId(runId)
                ? (runId, string.Empty)
                : (null, ErrorCodes.RunIdInvalidCode);
        }

        private static (
            IReadOnlyDictionary<string, string> credentials,
            string errorCode
        ) ReadCredentials(string folder)
        {
            return folder.Length == 0
                ? (new Dictionary<string, string>(), string.Empty)
                : new CredentialFolderReader().Read(folder);
        }
    }
}
