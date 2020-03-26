using NUnit.Framework;
using System;
using System.IO;

namespace NotVisualBasic.FileIO
{
    public class CsvTextFieldParserTest_Configuration
    {
        private CsvTextFieldParser CreateParser(string input) => new CsvTextFieldParser(new StringReader(input));

        [Test]
        public void SetQuoteCharacterToSingleQuote()
        {
            using (var parser = CreateParser($@"1,"""","""",'a,test','a,''test''',2"))
            {
                parser.SetQuoteCharacter('\'');

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1", @"""""", @"""""", @"a,test", @"a,'test'", "2" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void SetQuoteEscapeCharacterToBackslash()
        {
            using (var parser = CreateParser($@"1,"""","""",""a,\""test\"""",2"))
            {
                parser.SetQuoteEscapeCharacter('\\');

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1", "", "", @"a,""test""", "2" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SetQuoteCharacterToSingleQuote_And_SetQuoteEscapeCharacterToBackslash(bool isSetEscapeCharFirst)
        {
            using (var parser = CreateParser($@"1,""a,test"",'a,\'test\'',2"))
            {
                // Should be able to set these in either order without changing the results
                if (isSetEscapeCharFirst)
                {
                    parser.SetQuoteEscapeCharacter('\\');
                    parser.SetQuoteCharacter('\'');
                }
                else
                {
                    parser.SetQuoteCharacter('\'');
                    parser.SetQuoteEscapeCharacter('\\');
                }

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1", @"""a", @"test""", "a,'test'", "2" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void SetDelimiters_Empty()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<NotSupportedException>(() => parser.Delimiters = new string[0]);
            }
        }

        [Test]
        public void SetDelimiters_Null()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<NotSupportedException>(() => parser.Delimiters = null);
            }
        }

        [Test]
        public void SetDelimiters_TwoDelimiters()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<NotSupportedException>(() => parser.Delimiters = new[] { ",", ";" });
            }
        }

        [Test]
        public void SetQuoteCharacter_NewLine()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<ArgumentException>(() => parser.SetQuoteCharacter('\n'));
            }
        }

        [Test]
        public void SetQuoteCharacter_CarriageReturn()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<ArgumentException>(() => parser.SetQuoteCharacter('\r'));
            }
        }

        [Test]
        public void SetQuoteEscapeCharacter_NewLine()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<ArgumentException>(() => parser.SetQuoteEscapeCharacter('\n'));
            }
        }

        [Test]
        public void SetQuoteEscapeCharacter_CarriageReturn()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<ArgumentException>(() => parser.SetQuoteEscapeCharacter('\r'));
            }
        }
    }
}
