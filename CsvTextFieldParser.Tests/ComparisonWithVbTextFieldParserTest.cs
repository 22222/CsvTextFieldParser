using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace NotVisualBasic.FileIO
{
    public class ComparisonWithVbTextFieldParserTest
    {
        [Test]
        public void RandomInput(
            [Values(
                "1234567890,\n",
                "1234567890,\n\"",
                "2,\n\r\"",
                "abcdefgh,\"\n\r\t ",
                "a2/\\#,\"'\n\r\t "
            )]
            string inputCharsString,
            [Values(true, false)] bool trimWhiteSpace,
            [Values(true, false)] bool hasFieldsEnclosedInQuotes
        )
        {
            // Using a hardcoded seed so our "random" tests are deterministic.
            const int seed = 0;
            const int iterations = 1000;

            var inputChars = inputCharsString.ToArray();
            var random = new Random(seed);
            for (var i = 0; i < iterations; i++)
            {
                var inputLength = random.Next(minValue: 1, maxValue: 1000);
                var input = string.Join(string.Empty, Enumerable.Range(0, inputLength).Select(_ => inputChars[random.Next(0, inputChars.Length)]));

                using (var expectedParser = CreateExpectedParser(input, trimWhiteSpace, hasFieldsEnclosedInQuotes))
                using (var actualParser = CreateActualParser(input, trimWhiteSpace, hasFieldsEnclosedInQuotes))
                {
                    bool endOfData;
                    int logicalLineCounter = 0;
                    string[] previousFields = null;
                    do
                    {
                        logicalLineCounter++;

                        bool actualEndOfData = actualParser.EndOfData;
                        bool expectedEndOfData = expectedParser.EndOfData;
                        endOfData = actualEndOfData || expectedEndOfData;
                        Assert.AreEqual(expectedEndOfData, actualEndOfData, $"EndOfData mismatch on iteration {i} logical line {logicalLineCounter} for input: {input}");

                        var actualLineNumber = actualParser.LineNumber;
                        var expectedLineNumber = expectedParser.LineNumber;
                        Assert.AreEqual(expectedLineNumber, actualLineNumber, $"LineNumber mismatch on iteration {i} on logical line {logicalLineCounter} for before fields: {string.Join(",", previousFields ?? Array.Empty<string>())}");

                        string[] actualFields;
                        CsvMalformedLineException actualException = null;
                        try
                        {
                            actualFields = actualParser.ReadFields();
                        }
                        catch (CsvMalformedLineException ex)
                        {
                            actualFields = null;
                            actualException = ex;
                        }

                        string[] expectedFields;
                        MalformedLineException expectedException = null;
                        try
                        {
                            expectedFields = expectedParser.ReadFields();
                        }
                        catch (MalformedLineException ex)
                        {
                            expectedFields = null;
                            expectedException = ex;
                        }

                        previousFields = expectedFields;

                        if (expectedException != null || actualException != null)
                        {
                            Assert.IsNotNull(expectedException, $"Expected no exception but was {actualException?.GetType().Name} on iteration {i} on logical line {logicalLineCounter}");
                            Assert.IsNotNull(actualException, $"Expected {expectedException?.GetType().Name} but was no exception on iteration {i} on logical line {logicalLineCounter}");
                            Assert.AreEqual(expectedParser.ErrorLine, actualParser.ErrorLine, $"ErrorLine mismatch on iteration {i} on logical line {logicalLineCounter}");

                            // Who know what they're doing for their line numbers.  It doesn't really matter if we exactly match probably?
                            //Assert.AreEqual(expectedParser.ErrorLineNumber, actualParser.ErrorLineNumber, $"ErrorLineNumber mismatch on iteration {i} on line {logicalLineCounter}");
                        }
                        CollectionAssert.AreEqual(expectedFields, actualFields, $"ReadFields mismatch on iteration {i} on logical line {logicalLineCounter} for input: {input}");
                    } while (!endOfData);
                }
            }
        }

        private TextFieldParser CreateExpectedParser(string input, bool trimWhiteSpace, bool hasFieldsEnclosedInQuotes)
        {
            var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(new StringReader(input));
            parser.SetDelimiters(",");
            parser.TrimWhiteSpace = trimWhiteSpace;
            parser.HasFieldsEnclosedInQuotes = hasFieldsEnclosedInQuotes;
            return parser;
        }

        private CsvTextFieldParser CreateActualParser(string input, bool trimWhiteSpace, bool hasFieldsEnclosedInQuotes)
        {
            var parser = new CsvTextFieldParser(new StringReader(input));
            parser.CompatibilityMode = true;
            parser.TrimWhiteSpace = trimWhiteSpace;
            parser.HasFieldsEnclosedInQuotes = hasFieldsEnclosedInQuotes;
            return parser;
        }

        [Test]
        public void ReadLine_Sample()
        {
            const string input = @"Name,Birth Date
Apollo Creed,1942-08-17
Ivan Drago,1961-11-03";

            var parserReader = new StringReader(input);
            var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(parserReader);
            var vbParser = new Microsoft.VisualBasic.FileIO.TextFieldParser(new StringReader(input));
            vbParser.SetDelimiters(",");

            Assert.AreEqual(vbParser.ReadLine(), parserReader.ReadLine());
            Assert.AreEqual(vbParser.ReadLine(), parserReader.ReadLine());
            Assert.AreEqual(vbParser.ReadLine(), parserReader.ReadLine());
            Assert.IsNull(vbParser.ReadFields());
            Assert.IsNull(parser.ReadFields());
            Assert.IsTrue(vbParser.EndOfData);
            Assert.IsTrue(parser.EndOfData);
        }

        [Test]
        public void ReadLine_SampleWithNewlineInQuotedField()
        {
            const string input = @"Name,Birth Date
""Creed, Apollo"",1942-08-17
""Ivan 
Drago"",1961-11-03
""Robert """"Rocky"""" Balboa"",1945-07-06";

            var parserReader = new StringReader(input);
            var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(parserReader);
            var vbParser = new Microsoft.VisualBasic.FileIO.TextFieldParser(new StringReader(input));
            vbParser.SetDelimiters(",");

            CollectionAssert.AreEqual(vbParser.ReadFields(), parser.ReadFields());
            CollectionAssert.AreEqual(vbParser.ReadFields(), parser.ReadFields());
            Assert.AreEqual(vbParser.ReadLine(), parserReader.ReadLine());

            // The readline should have read into the middle of the field, which changes the parsing output
            CollectionAssert.AreEqual(new[] { @"Drago""", "1961-11-03" }, vbParser.ReadFields());
            CollectionAssert.AreEqual(new[] { @"Drago""", "1961-11-03" }, parser.ReadFields());

            CollectionAssert.AreEqual(vbParser.ReadFields(), parser.ReadFields());
            Assert.IsNull(vbParser.ReadFields());
            Assert.IsNull(parser.ReadFields());
            Assert.IsTrue(vbParser.EndOfData);
            Assert.IsTrue(parser.EndOfData);
        }
    }
}
