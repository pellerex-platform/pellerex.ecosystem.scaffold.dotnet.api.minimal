using System;
using System.Collections.Generic;

namespace AcceptanceTests.ClientHelper.Models
{
    /// <summary>A run's inputs, read from its environment by <see cref="RunInputsReader"/>.</summary>
    public class RunInputs
    {
        /// <summary>The address of the copy of the app the tests call, ending in a slash so a path prefix is kept.</summary>
        public Uri TargetUrl { get; set; }

        public string RunId { get; set; }

        /// <summary>The folder each test's record goes to; empty when the run records nothing.</summary>
        public string RecordingsFolder { get; set; }

        /// <summary>Every value of the credential folder by its file name; empty when the run is unauthenticated.</summary>
        public IReadOnlyDictionary<string, string> Credentials { get; set; }
    }
}
