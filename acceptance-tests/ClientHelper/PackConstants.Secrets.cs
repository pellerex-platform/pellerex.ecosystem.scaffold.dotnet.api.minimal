using System.Collections.Generic;

namespace AcceptanceTests.ClientHelper
{
    public static partial class PackConstants
    {
        /// <summary>
        /// What the record of a run never keeps: credential material. Every value of the credential
        /// folder is taken out wherever it shows, and so is the value of every header, field and
        /// query parameter named like a secret, whatever its value is.
        /// </summary>
        public static class Secrets
        {
            /// <summary>What stands in the record where a secret was, as the stand-up tool writes it.</summary>
            public const string Marker = "[redacted]";

            /// <summary>Headers whose whole value is credential material, matched in any case.</summary>
            public static readonly IReadOnlyList<string> SecretHeaderNames =
            [
                "Authorization",
                "Proxy-Authorization",
                "Cookie",
                "Set-Cookie",
                "X-Api-Key",
                "Api-Key",
                "X-Auth-Token",
                "X-Access-Token",
            ];

            /// <summary>
            /// Parts of a JSON field's, form field's or query parameter's name that mark its value as
            /// credential material, matched in any case anywhere in the name, as in Password,
            /// access_token or clientSecret.
            /// </summary>
            public static readonly IReadOnlyList<string> SecretNameParts =
            [
                "password",
                "passwd",
                "secret",
                "token",
                "apikey",
                "api_key",
                "api-key",
                "authorization",
                "credential",
                "cookie",
                "privatekey",
                "private_key",
            ];
        }
    }
}
