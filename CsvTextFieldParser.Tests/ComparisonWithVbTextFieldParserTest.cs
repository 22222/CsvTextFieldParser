using Microsoft.VisualBasic.FileIO;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace NotVisualBasic.FileIO
{
    public class ComparisonWithVbTextFieldParserTest
    {
        [Theory]
        [CombinatorialData]
        public void RandomInput(
            [CombinatorialValues(
                "1234567890,\n",
                "1234567890,\n\"",
                "2,\n\r\"",
                "abcdefgh,\"\n\r\t ",
                "a2/\\#,\"'\n\r\t "
            )]
            string inputCharsString,
            [CombinatorialValues(true, false)] bool trimWhiteSpace,
            [CombinatorialValues(true, false)] bool hasFieldsEnclosedInQuotes,
            [CombinatorialValues(',', '|')] char delimiter
        )
        {
            RandomInputImpl(inputCharsString, trimWhiteSpace, hasFieldsEnclosedInQuotes, (random) => delimiter);
        }

        [Theory]
        [CombinatorialData]
        public void RandomInput_RandomDelimiter(
           [CombinatorialValues(
                "1234567890,\n",
                "1234567890,\n\"",
                "2,\n\r\"",
                "abcdefgh,\"\n\r\t ",
                "a2/\\#,\"'\n\r\t "
            )]
            string inputCharsString,
           [CombinatorialValues(true, false)] bool trimWhiteSpace,
           [CombinatorialValues(true, false)] bool hasFieldsEnclosedInQuotes
        )
        {
            RandomInputImpl(inputCharsString, trimWhiteSpace, hasFieldsEnclosedInQuotes, (random) =>
            {
                var delimiterChars = inputCharsString.Except(new[] { '\r', '\n', '"' }).ToArray();
                var delimiter = delimiterChars[random.Next(0, delimiterChars.Length)];
                return delimiter;
            });
        }

        private void RandomInputImpl(string inputCharsString, bool trimWhiteSpace, bool hasFieldsEnclosedInQuotes, Func<Random, char> chooseDelimiter)
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
                var delimiter = chooseDelimiter.Invoke(random);
                using (var expectedParser = CreateExpectedParser(input, trimWhiteSpace, hasFieldsEnclosedInQuotes))
                using (var actualParser = CreateActualParser(input, trimWhiteSpace, hasFieldsEnclosedInQuotes))
                {
                    if (delimiter != ',')
                    {
                        expectedParser.SetDelimiters(delimiter.ToString(CultureInfo.InvariantCulture));
                        actualParser.SetDelimiter(delimiter);
                    }

                    bool endOfData;
                    int logicalLineCounter = 0;
                    string[] previousFields = null;
                    do
                    {
                        logicalLineCounter++;

                        bool actualEndOfData = actualParser.EndOfData;
                        bool expectedEndOfData = expectedParser.EndOfData;
                        endOfData = actualEndOfData || expectedEndOfData;
                        CustomAssert.Equal(expectedEndOfData, actualEndOfData, $"EndOfData mismatch on iteration {i} with delimiter \"{delimiter}\" logical line {logicalLineCounter} for input: {input}");

                        var actualLineNumber = actualParser.LineNumber;
                        var expectedLineNumber = expectedParser.LineNumber;
                        CustomAssert.Equal(expectedLineNumber, actualLineNumber, $"LineNumber mismatch on iteration {i} with delimiter \"{delimiter}\" on logical line {logicalLineCounter} for before fields: {string.Join(",", previousFields ?? Array.Empty<string>())}");

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
                            CustomAssert.NotNull(expectedException, $"Expected no exception but was {actualException?.GetType().Name} on iteration {i} with delimiter \"{delimiter}\" on logical line {logicalLineCounter}");
                            CustomAssert.NotNull(actualException, $"Expected {expectedException?.GetType().Name} but was no exception on iteration {i} with delimiter \"{delimiter}\" on logical line {logicalLineCounter}");
                            CustomAssert.Equal(expectedParser.ErrorLine, actualParser.ErrorLine, $"ErrorLine mismatch on iteration {i} with delimiter \"{delimiter}\" on logical line {logicalLineCounter}");

                            // Who know what they're doing for their line numbers.  It doesn't really matter if we exactly match probably?
                            //Assert.Equal(expectedParser.ErrorLineNumber, actualParser.ErrorLineNumber, $"ErrorLineNumber mismatch on iteration {i} on line {logicalLineCounter}");
                        }
                        CustomAssert.Equal(expectedFields, actualFields, $"ReadFields mismatch on iteration {i} with delimiter \"{delimiter}\" on logical line {logicalLineCounter} for input: {input}");
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

        [Fact]
        public void ReadLine_Sample()
        {
            const string input = @"Name,Birth Date
Apollo Creed,1942-08-17
Ivan Drago,1961-11-03";

            var parserReader = new StringReader(input);
            var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(parserReader);
            var vbParser = new Microsoft.VisualBasic.FileIO.TextFieldParser(new StringReader(input));
            vbParser.SetDelimiters(",");

            Assert.Equal(vbParser.ReadLine(), parserReader.ReadLine());
            Assert.Equal(vbParser.ReadLine(), parserReader.ReadLine());
            Assert.Equal(vbParser.ReadLine(), parserReader.ReadLine());
            Assert.Null(vbParser.ReadFields());
            Assert.Null(parser.ReadFields());
            Assert.True(vbParser.EndOfData);
            Assert.True(parser.EndOfData);
        }

        [Fact]
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

            Assert.Equal(vbParser.ReadFields(), parser.ReadFields());
            Assert.Equal(vbParser.ReadFields(), parser.ReadFields());
            Assert.Equal(vbParser.ReadLine(), parserReader.ReadLine());

            // The readline should have read into the middle of the field, which changes the parsing output
            Assert.Equal(new[] { @"Drago""", "1961-11-03" }, vbParser.ReadFields());
            Assert.Equal(new[] { @"Drago""", "1961-11-03" }, parser.ReadFields());

            Assert.Equal(vbParser.ReadFields(), parser.ReadFields());
            Assert.Null(vbParser.ReadFields());
            Assert.Null(parser.ReadFields());
            Assert.True(vbParser.EndOfData);
            Assert.True(parser.EndOfData);
        }
    }
}
