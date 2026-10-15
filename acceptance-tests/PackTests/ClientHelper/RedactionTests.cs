using System;
using System.Net;
using System.Threading.Tasks;
using AcceptanceTests.ClientHelper;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.PackTests.ClientHelper
{
    public class RedactionTests
    {
        private const string Token = "eyJhbGciOiJSUzI1NiJ9.token-of-the-test-identity";
        private const string Password = "pa ss+word/&=?";
        private const string Marker = Secrets.Marker;

        private readonly Redaction redaction = new([Token, Password, "", null]);

        [Test]
        public async Task A_secret_value_is_replaced_wherever_it_shows_in_text()
        {
            var text = $"sent {Token} and again {Token}";

            await Assert
                .That(this.redaction.Text(text))
                .IsEqualTo($"sent {Marker} and again {Marker}");
        }

        [Test]
        public async Task A_secret_value_escaped_for_a_url_a_form_or_json_is_replaced_too()
        {
            await Assert
                .That(this.redaction.Text($"q={Uri.EscapeDataString(Password)}"))
                .IsEqualTo($"q={Marker}");
            await Assert
                .That(this.redaction.Text($"q={WebUtility.UrlEncode(Password)}"))
                .IsEqualTo($"q={Marker}");
            var jsonSecret = new Redaction(["a\"b"]);
            await Assert
                .That(jsonSecret.Text("{\"p\":\"a\\\"b\"}"))
                .IsEqualTo($"{{\"p\":\"{Marker}\"}}");
        }

        [Test]
        public async Task Text_without_a_secret_is_kept_as_written()
        {
            await Assert.That(this.redaction.Text("nothing to hide")).IsEqualTo("nothing to hide");
            await Assert.That(this.redaction.Text(null)).IsEqualTo(string.Empty);
        }

        [Test]
        public async Task A_redaction_with_no_secret_changes_nothing()
        {
            var none = new Redaction([]);

            await Assert.That(none.Text(Token)).IsEqualTo(Token);
        }

        [Test]
        [Arguments("Authorization")]
        [Arguments("authorization")]
        [Arguments("Cookie")]
        [Arguments("Set-Cookie")]
        [Arguments("X-Api-Key")]
        [Arguments("Proxy-Authorization")]
        public async Task A_header_that_carries_credentials_by_its_name_is_replaced_whole(
            string name
        )
        {
            await Assert
                .That(this.redaction.HeaderValue(name, "Basic anything-at-all"))
                .IsEqualTo(Marker);
        }

        [Test]
        [Arguments("X-Client-Secret")]
        [Arguments("X-Refresh-Token")]
        [Arguments("X-Password")]
        public async Task A_header_named_like_a_secret_is_replaced_whole_though_it_is_not_a_known_one(
            string name
        )
        {
            await Assert
                .That(this.redaction.HeaderValue(name, "anything-at-all"))
                .IsEqualTo(Marker);
        }

        [Test]
        public async Task Each_line_of_a_secret_that_runs_over_several_is_replaced_on_its_own()
        {
            var key = new Redaction([
                "-----BEGIN PRIVATE KEY-----\r\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASC\r\n-----END PRIVATE KEY-----",
            ]);

            await Assert
                .That(key.Text("the test printed MIIEvQIBADANBgkqhkiG9w0BAQEFAASC alone"))
                .IsEqualTo($"the test printed {Marker} alone");
        }

        [Test]
        public async Task Any_other_header_keeps_its_value_less_the_secrets_in_it()
        {
            await Assert
                .That(this.redaction.HeaderValue("Accept", "application/json"))
                .IsEqualTo("application/json");
            await Assert.That(this.redaction.HeaderValue("X-Echo", Token)).IsEqualTo(Marker);
        }

        [Test]
        public async Task A_query_parameter_named_like_a_secret_loses_its_value()
        {
            var url = "/v1/items?page=2&access_token=abc&clientSecret=xyz&name=plain";

            await Assert
                .That(this.redaction.PathAndQuery(url))
                .IsEqualTo(
                    $"/v1/items?page=2&access_token={Marker}&clientSecret={Marker}&name=plain"
                );
        }

        [Test]
        public async Task A_secret_value_in_a_path_is_replaced()
        {
            await Assert
                .That(this.redaction.PathAndQuery($"/v1/keys/{Token}"))
                .IsEqualTo($"/v1/keys/{Marker}");
            await Assert.That(this.redaction.PathAndQuery("/v1/hello")).IsEqualTo("/v1/hello");
        }

        [Test]
        public async Task A_json_field_named_like_a_secret_loses_its_value_at_any_depth()
        {
            var body =
                "{\"user\":{\"name\":\"ann\",\"Password\":\"hunter2\"},\"items\":[{\"apiKey\":{\"value\":1}}],\"refresh_token\":null}";

            var redacted = this.redaction.Body(body, "application/json");

            await Assert
                .That(redacted)
                .IsEqualTo(
                    $"{{\"user\":{{\"name\":\"ann\",\"Password\":\"{Marker}\"}},\"items\":[{{\"apiKey\":\"{Marker}\"}}],\"refresh_token\":\"{Marker}\"}}"
                );
        }

        [Test]
        public async Task A_secret_word_inside_a_value_does_not_make_its_field_secret()
        {
            var body = "{\"note\":\"reset the password and the token\",\"score\":110}";

            await Assert.That(this.redaction.Body(body, "application/json")).IsEqualTo(body);
        }

        [Test]
        [Arguments(null)]
        [Arguments("")]
        public async Task A_request_with_no_path_records_none(string pathAndQuery)
        {
            await Assert.That(this.redaction.PathAndQuery(pathAndQuery)).IsEqualTo(string.Empty);
        }

        [Test]
        public async Task A_json_body_with_no_field_named_like_a_secret_is_kept_exactly_as_written()
        {
            var body = "{ \"score\" : 110.0,\n  \"name\": \"ann\" }";

            await Assert
                .That(this.redaction.Body(body, "application/problem+json"))
                .IsEqualTo(body);
        }

        [Test]
        public async Task A_body_that_is_not_json_after_all_is_redacted_by_its_values()
        {
            await Assert
                .That(this.redaction.Body($"not json {Token}", "application/json"))
                .IsEqualTo($"not json {Marker}");
            await Assert
                .That(this.redaction.Body($"{{\"a\":1,\"a\":\"{Token}\"}}", "application/json"))
                .IsEqualTo($"{{\"a\":1,\"a\":\"{Marker}\"}}");
        }

        [Test]
        public async Task A_form_field_named_like_a_secret_loses_its_value()
        {
            var body = "username=ann&password=hunter2&remember=true";

            await Assert
                .That(this.redaction.Body(body, "application/x-www-form-urlencoded"))
                .IsEqualTo($"username=ann&password={Marker}&remember=true");
        }

        [Test]
        public async Task Any_other_body_is_redacted_by_its_values()
        {
            await Assert
                .That(this.redaction.Body($"token is {Token}", "text/plain"))
                .IsEqualTo($"token is {Marker}");
            await Assert
                .That(this.redaction.Body(string.Empty, "text/plain"))
                .IsEqualTo(string.Empty);
        }

        [Test]
        [Arguments("password", true)]
        [Arguments("NewPassword", true)]
        [Arguments("client_secret", true)]
        [Arguments("Authorization", true)]
        [Arguments("X-Api-Key", true)]
        [Arguments("credentials", true)]
        [Arguments("name", false)]
        [Arguments("score", false)]
        public async Task A_name_is_secret_when_it_holds_a_secret_word(string name, bool secret)
        {
            await Assert.That(Redaction.IsSecretName(name)).IsEqualTo(secret);
        }
    }
}
