using System;
using System.IO;

namespace AcceptanceTests.PackTests.StandIns
{
    /// <summary>A folder of a test's own, removed with everything in it when the test is done.</summary>
    public sealed class TemporaryFolder : IDisposable
    {
        public TemporaryFolder()
        {
            this.Path = Directory.CreateTempSubdirectory("pack-tests-").FullName;
        }

        public string Path { get; }

        /// <summary>The full path of a file or folder inside this folder.</summary>
        public string Inside(string name)
        {
            return System.IO.Path.Combine(this.Path, name);
        }

        public void Dispose()
        {
            Directory.Delete(this.Path, recursive: true);
        }
    }
}
