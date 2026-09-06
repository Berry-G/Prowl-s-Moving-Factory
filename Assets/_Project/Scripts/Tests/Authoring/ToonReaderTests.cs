/**
 * 목적: ToonReader + ToonParser 단위 테스트. SDD-05 §11 표 케이스 전부 검증.
 * 왜 이 구조인가: 파서가 조용히 무시하는 경로를 발견하면 오류로 바꿔야 하므로 허용·거부 모두 검증.
 * 바꾸면 안 되는 것: 거부 케이스의 행 번호 검증. 번호가 바뀌면 파일 구조가 바뀐 것이다.
 * 근거: SDD-05 §11 [D-05-11], SDD-02 §6 [D-02-07]
 */
using NUnit.Framework;
using PMF.EditorTools.Authoring.Toon;

namespace PMF.Tests
{
    public class ToonReaderTests
    {
        [Test]
        public void Parse_ScalarString_ReturnsValue()
        {
            var r = ToonParser.Parse("key: hello");
            Assert.That(r.Ok, Is.True);
            var s = r.Value.Entries["key"] as ToonReader.ScalarNode;
            Assert.That(s, Is.Not.Null);
            Assert.That(s.Value, Is.EqualTo("hello"));
        }

        [Test]
        public void Parse_ScalarInt_ReturnsNumber()
        {
            var r = ToonParser.Parse("key: 42");
            Assert.That(r.Ok, Is.True);
            var s = r.Value.Entries["key"] as ToonReader.ScalarNode;
            Assert.That(s.Value, Is.EqualTo(42.0));
        }

        [Test]
        public void Parse_ScalarFloat_ReturnsDouble()
        {
            var r = ToonParser.Parse("key: 0.42");
            Assert.That(r.Ok, Is.True);
            var s = r.Value.Entries["key"] as ToonReader.ScalarNode;
            Assert.That(s.Value, Is.EqualTo(0.42));
        }

        [Test]
        public void Parse_ScalarBoolean_ReturnsTrue()
        {
            var r = ToonParser.Parse("key: true");
            Assert.That(r.Ok, Is.True);
            var s = r.Value.Entries["key"] as ToonReader.ScalarNode;
            Assert.That(s.Value, Is.EqualTo(true));
        }

        [Test]
        public void Parse_QuotedString_Unescapes()
        {
            var r = ToonParser.Parse("key: \"hello\\nworld\"");
            Assert.That(r.Ok, Is.True);
            var s = r.Value.Entries["key"] as ToonReader.ScalarNode;
            Assert.That(s.Value, Is.EqualTo("hello\nworld"));
        }

        [Test]
        public void Parse_NestedObject_ReturnsObject()
        {
            var r = ToonParser.Parse("outer:\n  inner: 123");
            Assert.That(r.Ok, Is.True);
            var outer = r.Value.Entries["outer"] as ToonReader.ObjectNode;
            Assert.That(outer, Is.Not.Null);
            var inner = outer.Entries["inner"] as ToonReader.ScalarNode;
            Assert.That(inner.Value, Is.EqualTo(123.0));
        }

        [Test]
        public void Parse_Array_ReturnsItems()
        {
            var r = ToonParser.Parse("items[3]: a,b,c");
            Assert.That(r.Ok, Is.True);
            var arr = r.Value.Entries["items"] as ToonReader.ArrayNode;
            Assert.That(arr, Is.Not.Null);
            Assert.That(arr.Items.Count, Is.EqualTo(3));
            Assert.That(arr.Items[0], Is.EqualTo("a"));
        }

        [Test]
        public void Parse_Table_ReturnsRows()
        {
            var r = ToonParser.Parse("rows[2]{name,val}:\n  x,1\n  y,2");
            Assert.That(r.Ok, Is.True);
            var tbl = r.Value.Entries["rows"] as ToonReader.TableNode;
            Assert.That(tbl, Is.Not.Null);
            Assert.That(tbl.Rows.Count, Is.EqualTo(2));
            Assert.That(tbl.Rows[0][1], Is.EqualTo(1.0));
        }

        [Test]
        public void Parse_CommentLines_AreSkipped()
        {
            var r = ToonParser.Parse("# c\nkey: val\n# d");
            Assert.That(r.Ok, Is.True);
            var s = r.Value.Entries["key"] as ToonReader.ScalarNode;
            Assert.That(s.Value, Is.EqualTo("val"));
        }

        [Test]
        public void Parse_EmptyLines_AreSkipped()
        {
            var r = ToonParser.Parse("key: val\n\nother: 1");
            Assert.That(r.Ok, Is.True);
        }

        [Test]
        public void Reject_TabCharacter()
        {
            var r = ToonParser.Parse("key:\tval");
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Error.Line, Is.EqualTo(1));
        }

        [Test]
        public void Reject_OddIndent()
        {
            var r = ToonParser.Parse("  outer:\n   inner: 1");
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Error.Line, Is.EqualTo(2));
        }

        [Test]
        public void Reject_NullLiteral()
        {
            var r = ToonParser.Parse("key: null");
            Assert.That(r.Ok, Is.False);
        }

        [Test]
        public void Reject_RootArray()
        {
            var r = ToonParser.Parse("[1,2,3]");
            Assert.That(r.Ok, Is.False);
        }

        [Test]
        public void Reject_TableRowCountMismatch()
        {
            var r = ToonParser.Parse("rows[2]{a,b}:\n  x,1");
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Error.Line, Is.EqualTo(1));
        }

        [Test]
        public void Reject_ExtraTableRows()
        {
            var r = ToonParser.Parse("rows[1]{a,b}:\n  x,1\n  y,2");
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Error.Line, Is.EqualTo(3));
        }

        [Test]
        public void Reject_UnknownEscape()
        {
            var r = ToonParser.Parse("key: \"\\z\"");
            Assert.That(r.Ok, Is.False);
        }

        [Test]
        public void Reject_EmptyFile()
        {
            var r = ToonParser.Parse("");
            Assert.That(r.Ok, Is.False);
        }

        [Test]
        public void Reject_UnterminatedQuote()
        {
            var r = ToonParser.Parse("key: \"unclosed");
            Assert.That(r.Ok, Is.False);
        }

        [Test]
        public void Reject_DuplicateKey()
        {
            var r = ToonParser.Parse("key: 1\nkey: 2");
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Error.Message, Does.Contain("중복"));
        }
    }
}