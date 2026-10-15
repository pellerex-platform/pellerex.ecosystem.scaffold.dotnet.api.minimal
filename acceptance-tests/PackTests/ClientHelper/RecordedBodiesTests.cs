using System.Text;
using System.Threading.Tasks;
using AcceptanceTests.ClientHelper;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using static AcceptanceTests.ClientHelper.PackConstants;

namespace AcceptanceTests.PackTests.ClientHelper
{
    public class RecordedBodiesTests
    {
        [Test]
        [Arguments(null, true)]
        [Arguments("", true)]
        [Arguments("text/plain", true)]
        [Arguments("text/html", true)]
        [Arguments("application/json", true)]
        [Arguments("application/problem+json", true)]
        [Arguments("application/xml", true)]
        [Arguments("application/atom+xml", true)]
        [Arguments("application/x-www-form-urlencoded", true)]
        [Arguments("image/png", false)]
        [Arguments("application/octet-stream", false)]
        [Arguments("application/pdf", false)]
        public async Task A_body_is_text_by_its_media_type(string mediaType, bool text)
        {
            await Assert.That(RecordedBodies.IsText(mediaType)).IsEqualTo(text);
        }

        [Test]
        [Arguments(null)]
        [Arguments("")]
        [Arguments("utf-8")]
        [Arguments("\"utf-8\"")]
        [Arguments("utf8")]
        [Arguments("nonsense")]
        public async Task A_text_body_is_read_as_utf8_unless_it_names_a_character_set_net_knows(
            string charSet
        )
        {
            var text = "{\"name\":\"Zoë\"}";

            await Assert
                .That(RecordedBodies.ReadText(Encoding.UTF8.GetBytes(text), charSet))
                .IsEqualTo(text);
        }

        [Test]
        public async Task A_body_in_a_character_set_net_knows_is_read_in_it_without_its_byte_order_mark()
        {
            var utf16 = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);

            await Assert
                .That(
                    RecordedBodies.ReadText(
                        [.. utf16.GetPreamble(), .. utf16.GetBytes("Zoë")],
                        "utf-16"
                    )
                )
                .IsEqualTo("Zoë");
            await Assert
                .That(RecordedBodies.ReadText(Encoding.Latin1.GetBytes("Zoë"), "ISO-8859-1"))
                .IsEqualTo("Zoë");
            await Assert
                .That(
                    RecordedBodies.ReadText(
                        [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("Zoë")],
                        null
                    )
                )
                .IsEqualTo("Zoë");
        }

        [Test]
        public async Task A_body_at_the_limit_is_kept_whole()
        {
            var body = new string('a', Bodies.MaximumLength);

            await Assert.That(RecordedBodies.Cap(body)).IsEqualTo(body);
        }

        [Test]
        public async Task A_body_past_the_limit_is_cut_with_a_note_of_how_much_was_left_out()
        {
            var body = new string('a', Bodies.MaximumLength) + new string('b', 25);

            var capped = RecordedBodies.Cap(body);

            await Assert
                .That(capped)
                .IsEqualTo(
                    new string('a', Bodies.MaximumLength) + "[truncated: 25 more characters]"
                );
        }

        [Test]
        public async Task A_body_that_is_not_text_is_described_not_kept()
        {
            await Assert
                .That(RecordedBodies.DescribeNonText(2048, "image/png"))
                .IsEqualTo("[a body of 2048 bytes of image/png, not text, is not recorded]");
        }
    }
}
