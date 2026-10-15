using System;
using System.Collections.Generic;
using System.IO;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.ClientHelper
{
    /// <summary>
    /// Reads the test identity's credential: a folder of files, one per key, which the stand-up tool
    /// writes for a local run and the platform mounts for a deployed one, so the pack cannot tell
    /// which world filled it.
    /// </summary>
    public class CredentialFolderReader
    {
        /// <summary>
        /// Answers every value by its file name, without the line break a file often ends with.
        /// Entries whose name starts with a dot are skipped: they are the mount's own bookkeeping,
        /// such as the ..data folder a Kubernetes secret mount keeps beside its files.
        /// </summary>
        public (IReadOnlyDictionary<string, string> credentials, string errorCode) Read(
            string folder
        )
        {
            if (!Directory.Exists(folder))
            {
                return (null, ErrorCodes.CredentialsFolderMissingCode);
            }

            // The folder is filled by the invoker, outside the pack's control, and a file in it may
            // be unreadable: the one place this class catches.
            try
            {
                var credentials = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var path in Directory.EnumerateFiles(folder))
                {
                    var name = Path.GetFileName(path);
                    if (name.StartsWith('.'))
                    {
                        continue;
                    }

                    credentials[name] = File.ReadAllText(path).Trim();
                }

                return (credentials, string.Empty);
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                return (null, ErrorCodes.CredentialUnreadableCode);
            }
        }
    }
}
