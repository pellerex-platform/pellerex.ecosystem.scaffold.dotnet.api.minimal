using System;
using Microsoft.Extensions.Hosting;
using TUnit.Core;

namespace RepoUniquePascalIdentifier.UnitTests
{
    /// <summary>
    /// What the app needs from its surroundings before the first test starts it.
    /// </summary>
    public static class TestSessionSetUp
    {
        /// <summary>
        /// The app reads the name of its environment from the process's environment variables on
        /// every request (<see cref="RepoUniquePascalIdentifier.Configuration.LogContextEnrichment"/>,
        /// through <see cref="CommonLibrary.Utils.GeneralFunctions.RequestEnvironment"/>) and fails
        /// the request when it is missing. A deployment always sets it, so the tests set it too, once
        /// before the first test starts and never again: to Development, the environment
        /// <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/> runs the
        /// app in.
        /// </summary>
        [Before(HookType.TestSession)]
        public static void RunTheAppInTheDevelopmentEnvironment()
        {
            Environment.SetEnvironmentVariable(
                CommonLibrary.Constants.EnvironmentVariable,
                Environments.Development
            );
        }
    }
}
