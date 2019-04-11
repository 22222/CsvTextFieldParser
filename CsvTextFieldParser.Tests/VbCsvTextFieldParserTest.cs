using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using System.IO;

namespace NotVisualBasic.FileIO
{
    /// <summary>
    /// These tests are just to make sure tests for our <see cref="CsvTextFieldParser"/> also pass for <see cref="TextFieldParser"/>.
    /// </summary>
    public class VbCsvTextFieldParserTest : CsvTextFieldParserTest
    {
        protected override bool CompatibilityMode => true;
        protected override ITextFieldParser CreateParser(string input) => new TextFieldParserAdapter(new StringReader(input));
        private class TextFieldParserAdapter : ITextFieldParser
        {
            private readonly TextFieldParser parser;

            public TextFieldParserAdapter(TextReader reader)
            {
                parser = new TextFieldParser(reader);
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TrimWhiteSpace = false;
            }

            public bool EndOfData => parser.EndOfData;
            public string[] ReadFields()
            {
                try
                {
                    return parser.ReadFields();
                }
                catch (MalformedLineException ex)
                {
                    throw new CsvMalformedLineException(ex.Message, ex.LineNumber, ex);
                }
            }
            public long LineNumber => parser.LineNumber;
            public string ErrorLine => parser.ErrorLine;
            public long ErrorLineNumber => parser.ErrorLineNumber;
            public void SetDelimiter(char delimiterChar) => parser.SetDelimiters(delimiterChar.ToString());
            public string[] Delimiters { set => parser.Delimiters = value; }
            public bool HasFieldsEnclosedInQuotes { set => parser.HasFieldsEnclosedInQuotes = value; }
            public bool TrimWhiteSpace { set => parser.TrimWhiteSpace = value; }
            public void Dispose() => parser.Dispose();
        }

        [Test]
        public void SetDelimiters_Empty()
        {
            using (var parser = new TextFieldParser(new StringReader("test")))
            {
                parser.Delimiters = new string[0];
                var actual = parser.Delimiters;
                Assert.AreEqual(0, actual.Length);
            }
        }

        [Test]
        public void SetDelimiters_Null()
        {
            using (var parser = new TextFieldParser(new StringReader("test")))
            {
                parser.Delimiters = null;
                var actual = parser.Delimiters;
                Assert.IsNull(actual);
            }
        }
    }
}
