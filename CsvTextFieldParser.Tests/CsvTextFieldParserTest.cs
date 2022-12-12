using System;
using System.IO;
using Xunit;

namespace NotVisualBasic.FileIO
{
    public class CsvTextFieldParserTest
    {
        protected virtual bool CompatibilityMode => false;
        protected virtual ITextFieldParser CreateParser(string input) => new CsvTextFieldParserAdapter(new StringReader(input));

        /// <summary>
        /// An interface to wrap <see cref="CsvTextFieldParser"/> so different implementations can be used to test against.
        /// </summary>
        protected interface ITextFieldParser : IDisposable
        {
            bool EndOfData { get; }
            string[] ReadFields();
            void SetDelimiter(char delimiterChar);
            string[] Delimiters { set; }
            bool HasFieldsEnclosedInQuotes { set; }
            bool TrimWhiteSpace { set; }
            long LineNumber { get; }
            string ErrorLine { get; }
            long ErrorLineNumber { get; }
        }

        /// <summary>
        /// The default implementation of <see cref="ITextFieldParser"/> using <see cref="CsvTextFieldParser"/>.
        /// </summary>
        protected class CsvTextFieldParserAdapter : ITextFieldParser
        {
            public CsvTextFieldParserAdapter(TextReader reader, bool isCompatibilityMode = false)
            {
                this.InnerParser = new CsvTextFieldParser(reader);
                this.InnerParser.CompatibilityMode = isCompatibilityMode;
            }

            public CsvTextFieldParser InnerParser { get; }
            public bool EndOfData => InnerParser.EndOfData;
            public string[] ReadFields() => InnerParser.ReadFields();
            public long LineNumber => InnerParser.LineNumber;
            public string ErrorLine => InnerParser.ErrorLine;
            public long ErrorLineNumber => InnerParser.ErrorLineNumber;
            public void SetDelimiter(char delimiterChar) => InnerParser.SetDelimiter(delimiterChar);
            public string[] Delimiters { set => InnerParser.Delimiters = value; }
            public bool HasFieldsEnclosedInQuotes { set => InnerParser.HasFieldsEnclosedInQuotes = value; }
            public bool TrimWhiteSpace { set => InnerParser.TrimWhiteSpace = value; }
            public void Dispose() => InnerParser.Dispose();
        }

        [Fact]
        public void ReadFields_SingleValue()
        {
            using (var parser = CreateParser("1"))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_TwoRows_SingleValue()
        {
            using (var parser = CreateParser($"1{Environment.NewLine}2"))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1" },
                    actual: parser.ReadFields()
                );

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "2" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_TwoValues()
        {
            using (var parser = CreateParser("1,2"))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1", "2" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_TwoRows_TwoValues()
        {
            using (var parser = CreateParser($"1,2{Environment.NewLine}3,4"))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1", "2" },
                    actual: parser.ReadFields()
                );

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "3", "4" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_TwoRows_TwoValues_EmptyLines()
        {
            using (var parser = CreateParser($"{Environment.NewLine}{Environment.NewLine}1,2{Environment.NewLine}{Environment.NewLine}3,4{Environment.NewLine}{Environment.NewLine}"))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1", "2" },
                    actual: parser.ReadFields()
                );

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "3", "4" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_TwoRows_TwoValues_WhiteSpaceLines()
        {
            using (var parser = CreateParser($"{Environment.NewLine} {Environment.NewLine}1,2{Environment.NewLine} \t {Environment.NewLine}3,4{Environment.NewLine}\t{Environment.NewLine}"))
            {
                if (!CompatibilityMode)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { " " },
                        actual: parser.ReadFields()
                    );
                }

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1", "2" },
                    actual: parser.ReadFields()
                );

                if (!CompatibilityMode)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { " \t " },
                        actual: parser.ReadFields()
                    );
                }

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "3", "4" },
                    actual: parser.ReadFields()
                );

                if (!CompatibilityMode)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { "\t" },
                        actual: parser.ReadFields()
                    );
                }

                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_Empty(bool trimWhiteSpace)
        {
            using (var parser = CreateParser(string.Empty))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.True(parser.EndOfData);
                Assert.Null(parser.ReadFields());
            }
        }

        [Fact]
        public void ReadFields_CommaOnly()
        {
            using (var parser = CreateParser(","))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "", "" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_SpaceOnly(bool trimWhiteSpace)
        {
            using (var parser = CreateParser(" "))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                if (!CompatibilityMode && !trimWhiteSpace)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { " " },
                        actual: parser.ReadFields()
                    );
                }
                Assert.True(parser.EndOfData);
                Assert.Null(parser.ReadFields());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_CrOnly(bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\r"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.True(parser.EndOfData);
                Assert.Null(parser.ReadFields());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_LfOnly(bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\n"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.True(parser.EndOfData);
                Assert.Null(parser.ReadFields());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_CrlfOnly(bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\r\n"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.True(parser.EndOfData);
                Assert.Null(parser.ReadFields());
            }
        }

        [Fact]
        public void ReadFields_QuoteOnly()
        {
            using (var parser = CreateParser("\""))
            {
                Assert.False(parser.EndOfData);
                Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_SampleWithQuotedCommas()
        {
            using (var parser = CreateParser(@"1,test,""a,test"",2"))
            {
                Assert.False(parser.EndOfData);

                var actual = parser.ReadFields();
                Assert.Equal(
                    expected: new[] { "1", "test", "a,test", "2" },
                    actual: actual
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_SampleWithQuotedNewlines(bool trimWhiteSpace)
        {
            using (var parser = CreateParser($"\"newline{Environment.NewLine}test\",2\nsecond line,3"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { $"newline{Environment.NewLine}test", "2" },
                    actual: parser.ReadFields()
                );

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { $"second line", "3" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_SampleWithQuotedQuotes()
        {
            using (var parser = CreateParser($"\"a \"\"quote\"\" test\",2\n\"\"\"completely quoted\"\"\",3"))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { $"a \"quote\" test", "2" },
                    actual: parser.ReadFields()
                );

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { $"\"completely quoted\"", "3" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_SampleWithLeadingAndTrailingWhitespace(bool trimWhiteSpace)
        {
            using (var parser = CreateParser($"a, b,c , d ,\" e\",\"f \",\" g \"{Environment.NewLine} h{Environment.NewLine}i {Environment.NewLine} j "))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.False(parser.EndOfData);
                if (trimWhiteSpace)
                {
                    Assert.Equal(
                        expected: new[] { "a", "b", "c", "d", "e", "f", "g" },
                        actual: parser.ReadFields()
                    );

                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { "h" },
                        actual: parser.ReadFields()
                    );

                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { "i" },
                        actual: parser.ReadFields()
                    );

                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { "j" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Equal(
                        expected: new[] { "a", " b", "c ", " d ", " e", "f ", " g " },
                        actual: parser.ReadFields()
                    );

                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { " h" },
                        actual: parser.ReadFields()
                    );

                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { "i " },
                        actual: parser.ReadFields()
                    );

                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { " j " },
                        actual: parser.ReadFields()
                    );
                }

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_BrokenQuotes()
        {
            using (var parser = CreateParser("\"te\"st"))
            {
                Assert.False(parser.EndOfData);
                Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_BrokenQuotesThenValidLine()
        {
            using (var parser = CreateParser("\"te\"st\n2,22"))
            {
                Assert.False(parser.EndOfData);
                Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "2", "22" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_QuoteInMiddleOfField()
        {
            using (var parser = CreateParser("te\"st"))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "te\"st" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_QuoteInMiddleOfField_ThenNewlineQuotedField()
        {
            using (var parser = CreateParser("te\"st,\"t\ne\rs\r\nt\""))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "te\"st", "t\ne\rs\r\nt" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_RandomSample_QuotedTwoConsecutiveCr(bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"\r\r2,,\"\n"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.False(parser.EndOfData);

                if (trimWhiteSpace)
                {
                    Assert.Equal(
                       expected: new[] { $"2,," },
                       actual: parser.ReadFields()
                    );
                }
                else if (CompatibilityMode)
                {
                    Assert.Equal(
                       expected: new[] { $"\r2,," },
                       actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Equal(
                       expected: new[] { $"\r\r2,," },
                       actual: parser.ReadFields()
                    );
                }
                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_QuotedTwoConsecutiveEOL(bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\r\n\r\n2\""))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: CompatibilityMode ? new[] { $"2\r\n2" } : new[] { $"2\r\n\r\n2" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_QuotedManyConsecutiveEOL(bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\r\n\r\n\r\r\n\n\n\r\n2\""))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: CompatibilityMode ? new[] { $"2\r\n2" } : new[] { $"2\r\n\r\n\r\r\n\n\n\r\n2" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_SampleWithQuotedNewLineFieldsAndNormalFields()
        {
            using (var parser = CreateParser($"\"quoted \r\n\",test,1\r\n2,\"quoted \r\n\",3\r\n4,5,\"quoted \r\n\"\r\n\r\n,6,7,8"))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "quoted \r\n", "test", "1" },
                    actual: parser.ReadFields()
                );

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "2", "quoted \r\n", "3" },
                    actual: parser.ReadFields()
                );

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "4", "5", "quoted \r\n" },
                    actual: parser.ReadFields()
                );

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "", "6", "7", "8" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_RandomSample_TabsAndCr()
        {
            using (var parser = CreateParser(",gc\t\r\n\t\",d\t\tb\rc\"\rd g\"\n\r"))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "", "gc\t", },
                    actual: parser.ReadFields()
                );

                if (CompatibilityMode)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { ",d\t\tb\rc", },
                        actual: parser.ReadFields()
                    );

                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { "d g\"", },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { "\t\"", "d\t\tb", },
                        actual: parser.ReadFields()
                    );

                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { "c\"", },
                        actual: parser.ReadFields()
                    );

                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { "d g\"", },
                        actual: parser.ReadFields()
                    );
                }

                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_SpaceBeforeQuote(bool trimWhiteSpace)
        {
            using (var parser = CreateParser(" \"2,2\""))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: CompatibilityMode || trimWhiteSpace ? new[] { "2,2" } : new[] { " \"2", "2\"" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_RandomSample_SpaceAfterEndQuote()
        {
            using (var parser = CreateParser("\"\rbhaedbded\ra, h\r\" "))
            {
                Assert.False(parser.EndOfData);
                if (CompatibilityMode)
                {
                    Assert.Equal(
                        expected: new[] { "\rbhaedbded\ra, h\r", "" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }
                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_SpaceAfterEndQuote(bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\" "))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.False(parser.EndOfData);
                if (CompatibilityMode || trimWhiteSpace)
                {
                    Assert.Equal(
                        expected: new[] { "2", "" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }
                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_SpaceAfterEndQuoteThenAnotherField(bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\" ,3"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.False(parser.EndOfData);
                if (CompatibilityMode || trimWhiteSpace)
                {
                    Assert.Equal(
                        expected: new[] { "2", "3" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }
                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_SpaceAfterEndQuoteThenAnotherLine(bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\" \n3"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.False(parser.EndOfData);
                if (CompatibilityMode || trimWhiteSpace)
                {
                    Assert.Equal(
                        expected: new[] { "2" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "3" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_MultipleSpacesAfterEndQuote(bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\" \t    \t   "))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.False(parser.EndOfData);
                if (CompatibilityMode || trimWhiteSpace)
                {
                    Assert.Equal(
                        expected: new[] { "2", "" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }

                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_MultipleSpacesAfterEndQuote_CustomDelimiter(bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\" \t    \t   "))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;
                parser.SetDelimiter('|');

                Assert.False(parser.EndOfData);
                if (CompatibilityMode || trimWhiteSpace)
                {
                    Assert.Equal(
                        expected: new[] { "2", "" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_RandomSample_SpaceBeforeStartQuote_TabAndCharAfterEndQuote()
        {
            using (var parser = CreateParser("edf\r\n \"	 \n,f\n\r\"\rfa,\"f\" 	c\n"))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "edf" },
                    actual: parser.ReadFields()
                );

                if (CompatibilityMode)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { "\t \n,f\n" },
                        actual: parser.ReadFields()
                    );

                    Assert.False(parser.EndOfData);
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }
                else
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { " \"\t " },
                        actual: parser.ReadFields()
                    );

                    Assert.False(parser.EndOfData);
                    Assert.Equal(
                        expected: new[] { "", "f" },
                        actual: parser.ReadFields()
                    );

                    Assert.False(parser.EndOfData);
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_RandomSample_TabAndCharAfterEndQuote()
        {
            using (var parser = CreateParser("\"f\" 	c"))
            {
                Assert.False(parser.EndOfData);
                Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_RandomSample_SpaceAndNewlinesAfterEndQuote()
        {
            using (var parser = CreateParser("eb\"b\tg,ag\"g\"dh\"\"hahb\tc\"\tagcb,\"ba\tdb\rfe\t\ng\t,b\" \r\ne \n"))
            {
                Assert.False(parser.EndOfData);
                if (CompatibilityMode)
                {
                    Assert.Equal(
                        expected: new[] { "eb\"b\tg", "ag\"g\"dh\"\"hahb\tc\"\tagcb", "ba\tdb\rfe\t\ng\t,b" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "e " },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadFields_SpaceAndNewlinesAfterEndQuote(bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"a\" \r\n"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.False(parser.EndOfData);
                if (CompatibilityMode || trimWhiteSpace)
                {
                    Assert.Equal(
                        expected: new[] { "a" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_BrokenQuotesSample()
        {
            using (var parser = CreateParser(@"""a""""b"""",""""c ""d"" e""""f"",""1"",""2, 3"",""4, 5"","""","""",""6"",""7"",""8"",""9"",""0"""))
            {
                Assert.False(parser.EndOfData);
                Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ReadFields_SampleWithTabDelimiterAndTrimWhiteSpace()
        {
            using (var parser = CreateParser(@"a		""\,
2a,\,,,'
a# 
22#' '\/\	,a/ /'# 2
 \,2/""""
22# 	\ 	,/""
2'	2 \\,/a\a/\
	
\,,2'a2""""2	 \2/,,""\#\2 \a,2	\
'""2/2
2""""'
""#2#, "))
            {
                parser.SetDelimiter('\t');
                parser.TrimWhiteSpace = true;
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "a", "", "\\,\r\n2a,\\,,,'\r\na# \r\n22#' '\\/\\\t,a/ /'# 2\r\n \\,2/\"\r\n22# \t\\ \t,/" },
                    actual: parser.ReadFields()
                );
            }
        }

        [Theory]
        [InlineData(',')]
        [InlineData('|')]
        [InlineData('\t')]
        public void SetDelimiter_ReadFields_Sample(char delimiterChar)
        {
            using (var parser = CreateParser($@"1{delimiterChar}2{delimiterChar}3{Environment.NewLine}4{delimiterChar}5"))
            {
                parser.SetDelimiter(delimiterChar);

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1", "2", "3" },
                    actual: parser.ReadFields()
                );

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "4", "5" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(',')]
        [InlineData('|')]
        [InlineData('\t')]
        public void SetDelimiter_ReadFields_SampleWithQuotedDelimiters(char delimiterChar)
        {
            using (var parser = CreateParser($@"1{delimiterChar}test{delimiterChar}""a{delimiterChar}test""{delimiterChar}2"))
            {
                parser.SetDelimiter(delimiterChar);

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1", "test", $"a{delimiterChar}test", "2" },
                    actual: parser.ReadFields()
                );

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void SetDelimiters_EmptyString()
        {
            using (var parser = CreateParser("test"))
            {
                var ex = Assert.Throws<ArgumentException>(() => parser.Delimiters = new[] { string.Empty });
            }
        }

        [Fact]
        public void SetDelimiters_NullDelimiter()
        {
            using (var parser = CreateParser("test"))
            {
                var ex = Assert.Throws<ArgumentException>(() => parser.Delimiters = new[] { default(string) });
            }
        }

        [Fact]
        public void SetDelimiters_NewLine()
        {
            using (var parser = CreateParser("test"))
            {
                var ex = Assert.Throws<ArgumentException>(() => parser.Delimiters = new[] { "\n" });
            }
        }

        [Fact]
        public void SetDelimiters_CarriageReturn()
        {
            using (var parser = CreateParser("test"))
            {
                var ex = Assert.Throws<ArgumentException>(() => parser.Delimiters = new[] { "\r" });
            }
        }

        [Fact]
        public void SetDelimiter_NewLine()
        {
            using (var parser = CreateParser("test"))
            {
                var ex = Assert.Throws<ArgumentException>(() => parser.SetDelimiter('\n'));
            }
        }

        [Fact]
        public void SetDelimiter_CarriageReturn()
        {
            using (var parser = CreateParser("test"))
            {
                var ex = Assert.Throws<ArgumentException>(() => parser.SetDelimiter('\r'));
            }
        }

        [Fact]
        public void ErrorLine_BrokenQuotesThenValidLine()
        {
            using (var parser = CreateParser("\"te\"st\n2,22"))
            {
                Assert.False(parser.EndOfData);
                var ex = Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                Assert.Equal("\"te\"st", parser.ErrorLine);
                Assert.Equal(1, parser.ErrorLineNumber);

                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "2", "22" },
                    actual: parser.ReadFields()
                );

                Assert.Equal("\"te\"st", parser.ErrorLine);
                Assert.Equal(1, parser.ErrorLineNumber);

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ErrorLine_QuoteNeverClosed_MultipleLinesInErrorField()
        {
            using (var parser = CreateParser("1\n\"2,22\n3\r\n4"))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1" },
                    actual: parser.ReadFields()
                );

                Assert.False(parser.EndOfData);
                var ex = Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                Assert.Equal("\"2,22\n3\r\n4", parser.ErrorLine);
                Assert.Equal(2, parser.ErrorLineNumber);

                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void ErrorLine_MaybeResetAfterClose()
        {
            ITextFieldParser parser;
            using (parser = CreateParser("\"te\"st"))
            {
                Assert.False(parser.EndOfData);
                var ex = Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                Assert.Equal("\"te\"st", parser.ErrorLine);
                Assert.Equal(1, parser.ErrorLineNumber);

                Assert.True(parser.EndOfData);
            }

            if (CompatibilityMode)
            {
                Assert.Equal("\"te\"st", parser.ErrorLine);
                Assert.Equal(1, parser.ErrorLineNumber);
            }
            else
            {
                Assert.Equal(string.Empty, parser.ErrorLine);
                Assert.Equal(-1L, parser.ErrorLineNumber);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LineNumber_SingleValue(bool checkEndOfData)
        {
            using (var parser = CreateParser("1"))
            {
                Assert.Equal(1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(-1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.True(parser.EndOfData);
                    Assert.Equal(-1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(-1, parser.LineNumber);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LineNumber_TwoRows_TwoValues(bool checkEndOfData)
        {
            using (var parser = CreateParser($"1,2{Environment.NewLine}3,4"))
            {
                Assert.Equal(1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(2, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(2, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(-1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.True(parser.EndOfData);
                    Assert.Equal(-1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(-1, parser.LineNumber);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public void LineNumber_SampleWithQuotedNewlines(bool trimWhiteSpace, bool checkEndOfData)
        {
            using (var parser = CreateParser($"\"newline{Environment.NewLine}test\",2\nsecond line,3\n\"third\rline\n\r\n\n\",4"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.Equal(1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(3, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(3, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(4, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(4, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(-1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.True(parser.EndOfData);
                    Assert.Equal(-1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(-1, parser.LineNumber);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LineNumber_SampleWithBlankLine(bool checkEndOfData)
        {
            using (var parser = CreateParser($"1,2\n3,4\n\n5,6\n7"))
            {
                Assert.Equal(1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(2, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(2, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(3, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(3, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(5, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(5, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(-1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.True(parser.EndOfData);
                    Assert.Equal(-1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(-1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.True(parser.EndOfData);
                    Assert.Equal(-1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(-1, parser.LineNumber);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LineNumber_SampleWithTrailingBlankLines(bool checkEndOfData)
        {
            using (var parser = CreateParser(",2,3\n,9\n\n\n"))
            {
                Assert.Equal(1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(2, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.False(parser.EndOfData);
                    Assert.Equal(2, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(3, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.True(parser.EndOfData);
                    Assert.Equal(3, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.Equal(-1, parser.LineNumber);

                for (var i = 0; i < 10; i++)
                {
                    if (checkEndOfData)
                    {
                        Assert.True(parser.EndOfData);
                        Assert.Equal(-1, parser.LineNumber);
                    }

                    parser.ReadFields();
                    Assert.Equal(-1, parser.LineNumber);
                }
            }
        }
    }
}
