using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.ClientHelper
{
    /// <summary>
    /// The one place credential material is taken out of what a run records. Every request and
    /// response a test makes passes through it before it is written, and so does every line of text
    /// the adapter puts in the result file, so a secret never reaches the result however a test or
    /// the app lets it slip. Two rules: every value of the credential folder is replaced wherever it
    /// shows, written as it is or escaped for a URL, a form or JSON, and each line of a value that runs
    /// over several, as the stand-up tool does; and the value of every header, JSON field, form field
    /// and query parameter named like a secret is replaced, whatever it is.
    /// </summary>
    public class Redaction
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        // Every form of every secret value, longest first, so a longer form is replaced whole
        // before a shorter one could match inside it.
        private readonly IReadOnlyList<string> secretForms;

        public Redaction(IEnumerable<string> secretValues)
        {
            var forms = new HashSet<string>(StringComparer.Ordinal);
            var parts = (secretValues ?? [])
                .Where(value => !string.IsNullOrEmpty(value))
                .SelectMany(value =>
                    value.Split('\n').Select(line => line.TrimEnd('\r')).Append(value)
                )
                .Where(part => part.Length > 0);
            foreach (var part in parts)
            {
                forms.Add(part);
                forms.Add(Uri.EscapeDataString(part));
                forms.Add(WebUtility.UrlEncode(part));
                forms.Add(
                    JsonEncodedText
                        .Encode(part, JavaScriptEncoder.UnsafeRelaxedJsonEscaping)
                        .ToString()
                );
            }

            this.secretForms = forms.OrderByDescending(form => form.Length).ToList();
        }

        /// <summary>Replaces every secret value in a piece of text, such as a log line or a failure message.</summary>
        public string Text(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text ?? string.Empty;
            }

            var redacted = text;
            foreach (var form in this.secretForms)
            {
                redacted = redacted.Replace(form, Secrets.Marker, StringComparison.Ordinal);
            }

            return redacted;
        }

        /// <summary>A header's value as it may be recorded: replaced whole when the header carries credentials by its name.</summary>
        public string HeaderValue(string name, string value)
        {
            return IsSecretHeader(name) ? Secrets.Marker : this.Text(value);
        }

        /// <summary>A request's path and query as it may be recorded, with every query parameter named like a secret replaced.</summary>
        public string PathAndQuery(string pathAndQuery)
        {
            if (string.IsNullOrEmpty(pathAndQuery))
            {
                return pathAndQuery ?? string.Empty;
            }

            var queryStart = pathAndQuery.IndexOf('?');
            if (queryStart < 0)
            {
                return this.Text(pathAndQuery);
            }

            return this.Text(pathAndQuery[..queryStart])
                + "?"
                + this.Text(RedactNamedFields(pathAndQuery[(queryStart + 1)..]));
        }

        /// <summary>
        /// A body as it may be recorded: a form's fields and a JSON document's fields named like a
        /// secret replaced by name, and every secret value replaced wherever it shows.
        /// </summary>
        public string Body(string body, string mediaType)
        {
            if (string.IsNullOrEmpty(body))
            {
                return body ?? string.Empty;
            }

            if (string.Equals(mediaType, Bodies.FormMediaType, StringComparison.OrdinalIgnoreCase))
            {
                return this.Text(RedactNamedFields(body));
            }

            if (IsJsonMediaType(mediaType))
            {
                return this.Text(RedactJsonFields(body));
            }

            return this.Text(body);
        }

        /// <summary>Whether a field's, form field's or query parameter's name marks its value as credential material.</summary>
        public static bool IsSecretName(string name)
        {
            return name is not null
                && Secrets.SecretNameParts.Any(part =>
                    name.Contains(part, StringComparison.OrdinalIgnoreCase)
                );
        }

        /// <summary>
        /// Whether a header carries credential material by its name alone: one of the known
        /// credential headers, or any header named like a secret, as in X-Client-Secret.
        /// </summary>
        public static bool IsSecretHeader(string name)
        {
            return name is not null
                && (
                    Secrets.SecretHeaderNames.Any(secretName =>
                        string.Equals(secretName, name, StringComparison.OrdinalIgnoreCase)
                    ) || IsSecretName(name)
                );
        }

        private static bool IsJsonMediaType(string mediaType)
        {
            return mediaType is not null
                && (
                    string.Equals(
                        mediaType,
                        Bodies.JsonMediaType,
                        StringComparison.OrdinalIgnoreCase
                    )
                    || mediaType.EndsWith(
                        Bodies.JsonMediaTypeSuffix,
                        StringComparison.OrdinalIgnoreCase
                    )
                );
        }

        // name=value pairs joined by ampersands, as in a query or a form body. A pair whose name,
        // decoded, is named like a secret keeps its name and loses its value.
        private static string RedactNamedFields(string fields)
        {
            var pairs = fields.Split('&');
            for (var index = 0; index < pairs.Length; index++)
            {
                var separator = pairs[index].IndexOf('=');
                var name = separator < 0 ? pairs[index] : pairs[index][..separator];
                if (separator >= 0 && IsSecretName(WebUtility.UrlDecode(name)))
                {
                    pairs[index] = name + "=" + Secrets.Marker;
                }
            }

            return string.Join('&', pairs);
        }

        // A JSON body is rewritten only when one of its fields is named like a secret; otherwise it
        // is kept exactly as the app or the test wrote it. The body may not be JSON at all, or may
        // hold a field twice, which a JSON object cannot keep: the parser reports either by throwing,
        // on reading the body or on walking its fields, the one place this class catches. Such a
        // body is redacted by its values alone.
        private static string RedactJsonFields(string body)
        {
            try
            {
                var document = JsonNode.Parse(body);
                return document is not null && RedactNamedNodes(document)
                    ? document.ToJsonString(JsonOptions)
                    : body;
            }
            catch (Exception exception)
                when (exception is JsonException or ArgumentException or InvalidOperationException)
            {
                return body;
            }
        }

        private static bool RedactNamedNodes(JsonNode node)
        {
            var changed = false;
            if (node is JsonObject fields)
            {
                foreach (var (name, value) in fields.ToList())
                {
                    if (IsSecretName(name))
                    {
                        fields[name] = Secrets.Marker;
                        changed = true;
                    }
                    else if (value is not null)
                    {
                        changed |= RedactNamedNodes(value);
                    }
                }
            }
            else if (node is JsonArray items)
            {
                foreach (var item in items.Where(item => item is not null))
                {
                    changed |= RedactNamedNodes(item);
                }
            }

            return changed;
        }
    }
}
