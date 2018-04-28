using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
