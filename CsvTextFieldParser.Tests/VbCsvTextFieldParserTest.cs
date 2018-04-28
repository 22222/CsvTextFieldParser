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
            public string ErrorLine => parser.ErrorLine;
            public long ErrorLineNumber => parser.ErrorLineNumber;
            public void SetDelimiter(char delimiterChar) => parser.SetDelimiters(delimiterChar.ToString());
            public void Dispose() => parser.Dispose();
        }
    }
}
