using System;
using System.IO;
using Xunit;

namespace NotVisualBasic.FileIO
{
    public class CsvTextFieldParserTest_Configuration
    {
        private CsvTextFieldParser CreateParser(string input) => new CsvTextFieldParser(new StringReader(input));

        [Fact]
        public void SetQuoteCharacterToSingleQuote()
        {
            using (var parser = CreateParser($@"1,"""","""",'a,test','a,''test''',2"))
            {
                parser.SetQuoteCharacter('\'');

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1", @"""""", @"""""", @"a,test", @"a,'test'", "2" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void SetQuoteEscapeCharacterToBackslash()
        {
            using (var parser = CreateParser($@"1,"""","""",""a,\""test\"""",2"))
            {
                parser.SetQuoteEscapeCharacter('\\');

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1", "", "", @"a,""test""", "2" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
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

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1", @"""a", @"test""", "a,'test'", "2" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void SetDelimiters_Empty()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<NotSupportedException>(() => parser.Delimiters = new string[0]);
            }
        }

        [Fact]
        public void SetDelimiters_Null()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<NotSupportedException>(() => parser.Delimiters = null);
            }
        }

        [Fact]
        public void SetDelimiters_TwoDelimiters()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<NotSupportedException>(() => parser.Delimiters = new[] { ",", ";" });
            }
        }

        [Fact]
        public void SetDelimiters_TwoCharacterDelimiter()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<NotSupportedException>(() => parser.Delimiters = new[] { "||" });
            }
        }

        [Fact]
        public void SetQuoteCharacter_NewLine()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<ArgumentException>(() => parser.SetQuoteCharacter('\n'));
            }
        }

        [Fact]
        public void SetQuoteCharacter_CarriageReturn()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<ArgumentException>(() => parser.SetQuoteCharacter('\r'));
            }
        }

        [Fact]
        public void SetQuoteEscapeCharacter_NewLine()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<ArgumentException>(() => parser.SetQuoteEscapeCharacter('\n'));
            }
        }

        [Fact]
        public void SetQuoteEscapeCharacter_CarriageReturn()
        {
            using (var parser = CreateParser("test"))
            {
                Assert.Throws<ArgumentException>(() => parser.SetQuoteEscapeCharacter('\r'));
            }
        }
    }
}
