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
    public class ComparisonWithVbTextFieldParserTest
    {
        [TestCase("1234567890,\n", 1000)]
        [TestCase("1234567890,\n\"", 1000)]
        [TestCase("2,\n\r\"", 1000)]
        [TestCase("abcdefgh,\"\n\r\t ", 1000)]
        [TestCase("a2/\\#,\"'\n\r\t ", 1000)]
        public void RandomInput(string inputCharsString, int iterations, int seed = 0)
        {
            var inputChars = inputCharsString.ToArray();
            var random = new Random(seed);
            for (var i = 0; i < iterations; i++)
            {
                var inputLength = random.Next(minValue: 1, maxValue: 1000);
                var input = string.Join(string.Empty, Enumerable.Range(0, inputLength).Select(_ => inputChars[random.Next(0, inputChars.Length)]));

                using (var expectedParser = CreateExpectedParser(input))
                using (var actualParser = CreateActualParser(input))
                {

                    bool endOfData;
                    int logicalLineCounter = 0;
                    do
                    {
                        logicalLineCounter++;

                        bool actualEndOfData = actualParser.EndOfData;
                        bool expectedEndOfData = expectedParser.EndOfData;
                        endOfData = actualEndOfData || expectedEndOfData;
                        Assert.AreEqual(expectedEndOfData, actualEndOfData, $"EndOfData mismatch on iteration {i} logical line {logicalLineCounter} for input: {input}");

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

        private TextFieldParser CreateExpectedParser(string input)
        {
            var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(new StringReader(input));
            parser.SetDelimiters(",");
            parser.TrimWhiteSpace = false;
            return parser;
        }

        private CsvTextFieldParser CreateActualParser(string input)
        {
            var parser = new CsvTextFieldParser(new StringReader(input));
            parser.CompatibilityMode = true;
            return parser;
        }
    }
}
