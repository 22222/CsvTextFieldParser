using NUnit.Framework;
using System;
using System.IO;

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
            void SetDelimiter(string delimiterString);
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
            public void SetDelimiter(string delimiterString) => InnerParser.SetDelimiter(delimiterString);
            public string[] Delimiters { set => InnerParser.Delimiters = value; }
            public bool HasFieldsEnclosedInQuotes { set => InnerParser.HasFieldsEnclosedInQuotes = value; }
            public bool TrimWhiteSpace { set => InnerParser.TrimWhiteSpace = value; }
            public void Dispose() => InnerParser.Dispose();
        }

        [Test]
        public void ReadFields_SingleValue()
        {
            using (var parser = CreateParser("1"))
            {
                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_TwoRows_SingleValue()
        {
            using (var parser = CreateParser($"1{Environment.NewLine}2"))
            {
                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1" },
                    actual: parser.ReadFields()
                );

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "2" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_TwoValues()
        {
            using (var parser = CreateParser("1,2"))
            {
                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1", "2" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_TwoRows_TwoValues()
        {
            using (var parser = CreateParser($"1,2{Environment.NewLine}3,4"))
            {
                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1", "2" },
                    actual: parser.ReadFields()
                );

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "3", "4" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_TwoRows_TwoValues_EmptyLines()
        {
            using (var parser = CreateParser($"{Environment.NewLine}{Environment.NewLine}1,2{Environment.NewLine}{Environment.NewLine}3,4{Environment.NewLine}{Environment.NewLine}"))
            {
                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1", "2" },
                    actual: parser.ReadFields()
                );

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "3", "4" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_TwoRows_TwoValues_WhiteSpaceLines()
        {
            using (var parser = CreateParser($"{Environment.NewLine} {Environment.NewLine}1,2{Environment.NewLine} \t {Environment.NewLine}3,4{Environment.NewLine}\t{Environment.NewLine}"))
            {
                if (!CompatibilityMode)
                {
                    Assert.IsFalse(parser.EndOfData);
                    CollectionAssert.AreEqual(
                        expected: new[] { " " },
                        actual: parser.ReadFields()
                    );
                }

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1", "2" },
                    actual: parser.ReadFields()
                );

                if (!CompatibilityMode)
                {
                    Assert.IsFalse(parser.EndOfData);
                    CollectionAssert.AreEqual(
                        expected: new[] { " \t " },
                        actual: parser.ReadFields()
                    );
                }

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "3", "4" },
                    actual: parser.ReadFields()
                );

                if (!CompatibilityMode)
                {
                    Assert.IsFalse(parser.EndOfData);
                    CollectionAssert.AreEqual(
                        expected: new[] { "\t" },
                        actual: parser.ReadFields()
                    );
                }

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_Empty([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser(string.Empty))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsTrue(parser.EndOfData);
                Assert.IsNull(parser.ReadFields());
            }
        }

        [Test]
        public void ReadFields_CommaOnly()
        {
            using (var parser = CreateParser(","))
            {
                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "", "" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_SpaceOnly([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser(" "))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                if (!CompatibilityMode && !trimWhiteSpace)
                {
                    Assert.IsFalse(parser.EndOfData);
                    CollectionAssert.AreEqual(
                        expected: new[] { " " },
                        actual: parser.ReadFields()
                    );
                }
                Assert.IsTrue(parser.EndOfData);
                Assert.IsNull(parser.ReadFields());
            }
        }

        [Test]
        public void ReadFields_CrOnly([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\r"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsTrue(parser.EndOfData);
                Assert.IsNull(parser.ReadFields());
            }
        }

        [Test]
        public void ReadFields_LfOnly([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\n"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsTrue(parser.EndOfData);
                Assert.IsNull(parser.ReadFields());
            }
        }

        [Test]
        public void ReadFields_CrlfOnly([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\r\n"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsTrue(parser.EndOfData);
                Assert.IsNull(parser.ReadFields());
            }
        }

        [Test]
        public void ReadFields_QuoteAsDelimiter()
        {
            using (var parser = CreateParser(@"""test""""""test"""))
            {
                parser.SetDelimiter("\"");
                parser.HasFieldsEnclosedInQuotes = true;

                Assert.IsFalse(parser.EndOfData);
                Assert.Throws<InvalidOperationException>(() => parser.ReadFields());

                Assert.IsFalse(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_QuoteOnly()
        {
            using (var parser = CreateParser("\""))
            {
                Assert.IsFalse(parser.EndOfData);
                Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_SampleWithQuotedCommas()
        {
            using (var parser = CreateParser(@"1,test,""a,test"",2"))
            {
                Assert.IsFalse(parser.EndOfData);

                var actual = parser.ReadFields();
                CollectionAssert.AreEqual(
                    expected: new[] { "1", "test", "a,test", "2" },
                    actual: actual
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_SampleWithQuotedNewlines([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser($"\"newline{Environment.NewLine}test\",2\nsecond line,3"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { $"newline{Environment.NewLine}test", "2" },
                    actual: parser.ReadFields()
                );

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { $"second line", "3" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_SampleWithQuotedQuotes()
        {
            using (var parser = CreateParser($"\"a \"\"quote\"\" test\",2\n\"\"\"completely quoted\"\"\",3"))
            {
                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { $"a \"quote\" test", "2" },
                    actual: parser.ReadFields()
                );

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { $"\"completely quoted\"", "3" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_BrokenQuotes()
        {
            using (var parser = CreateParser("\"te\"st"))
            {
                Assert.IsFalse(parser.EndOfData);
                Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_BrokenQuotesThenValidLine()
        {
            using (var parser = CreateParser("\"te\"st\n2,22"))
            {
                Assert.IsFalse(parser.EndOfData);
                Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "2", "22" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_QuoteInMiddleOfField()
        {
            using (var parser = CreateParser("te\"st"))
            {
                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "te\"st" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_QuoteInMiddleOfField_ThenNewlineQuotedField()
        {
            using (var parser = CreateParser("te\"st,\"t\ne\rs\r\nt\""))
            {
                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "te\"st", "t\ne\rs\r\nt" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_RandomSample_QuotedTwoConsecutiveCr([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"\r\r2,,\"\n"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsFalse(parser.EndOfData);

                if (trimWhiteSpace)
                {
                    CollectionAssert.AreEqual(
                       expected: new[] { $"2,," },
                       actual: parser.ReadFields()
                    );
                }
                else if (CompatibilityMode)
                {
                    CollectionAssert.AreEqual(
                       expected: new[] { $"\r2,," },
                       actual: parser.ReadFields()
                    );
                }
                else
                {
                    CollectionAssert.AreEqual(
                       expected: new[] { $"\r\r2,," },
                       actual: parser.ReadFields()
                    );
                }
                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_QuotedTwoConsecutiveEOL([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\r\n\r\n2\""))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: CompatibilityMode ? new[] { $"2\r\n2" } : new[] { $"2\r\n\r\n2" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_QuotedManyConsecutiveEOL([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\r\n\r\n\r\r\n\n\n\r\n2\""))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: CompatibilityMode ? new[] { $"2\r\n2" } : new[] { $"2\r\n\r\n\r\r\n\n\n\r\n2" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_SampleWithQuotedNewLineFieldsAndNormalFields()
        {
            using (var parser = CreateParser($"\"quoted \r\n\",test,1\r\n2,\"quoted \r\n\",3\r\n4,5,\"quoted \r\n\"\r\n\r\n,6,7,8"))
            {
                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "quoted \r\n", "test", "1" },
                    actual: parser.ReadFields()
                );

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "2", "quoted \r\n", "3" },
                    actual: parser.ReadFields()
                );

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "4", "5", "quoted \r\n" },
                    actual: parser.ReadFields()
                );

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "", "6", "7", "8" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_RandomSample_TabsAndCr()
        {
            using (var parser = CreateParser(",gc\t\r\n\t\",d\t\tb\rc\"\rd g\"\n\r"))
            {
                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "", "gc\t", },
                    actual: parser.ReadFields()
                );

                if (CompatibilityMode)
                {
                    Assert.IsFalse(parser.EndOfData);
                    CollectionAssert.AreEqual(
                        expected: new[] { ",d\t\tb\rc", },
                        actual: parser.ReadFields()
                    );

                    Assert.IsFalse(parser.EndOfData);
                    CollectionAssert.AreEqual(
                        expected: new[] { "d g\"", },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.IsFalse(parser.EndOfData);
                    CollectionAssert.AreEqual(
                        expected: new[] { "\t\"", "d\t\tb", },
                        actual: parser.ReadFields()
                    );

                    Assert.IsFalse(parser.EndOfData);
                    CollectionAssert.AreEqual(
                        expected: new[] { "c\"", },
                        actual: parser.ReadFields()
                    );

                    Assert.IsFalse(parser.EndOfData);
                    CollectionAssert.AreEqual(
                        expected: new[] { "d g\"", },
                        actual: parser.ReadFields()
                    );
                }

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_SpaceBeforeQuote([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser(" \"2,2\""))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: CompatibilityMode || trimWhiteSpace ? new[] { "2,2" } : new[] { " \"2", "2\"" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_RandomSample_SpaceAfterEndQuote()
        {
            using (var parser = CreateParser("\"\rbhaedbded\ra, h\r\" "))
            {
                Assert.IsFalse(parser.EndOfData);
                if (CompatibilityMode)
                {
                    CollectionAssert.AreEqual(
                        expected: new[] { "\rbhaedbded\ra, h\r", "" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }
                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_SpaceAfterEndQuote([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\" "))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsFalse(parser.EndOfData);
                if (CompatibilityMode || trimWhiteSpace)
                {
                    CollectionAssert.AreEqual(
                        expected: new[] { "2", "" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }
                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_SpaceAfterEndQuoteThenAnotherField([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\" ,3"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsFalse(parser.EndOfData);
                if (CompatibilityMode || trimWhiteSpace)
                {
                    CollectionAssert.AreEqual(
                        expected: new[] { "2", "3" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }
                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_SpaceAfterEndQuoteThenAnotherLine([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\" \n3"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsFalse(parser.EndOfData);
                if (CompatibilityMode || trimWhiteSpace)
                {
                    CollectionAssert.AreEqual(
                        expected: new[] { "2" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "3" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_MultipleSpacesAfterEndQuote([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"2\" \t    \t   "))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsFalse(parser.EndOfData);
                if (CompatibilityMode || trimWhiteSpace)
                {
                    CollectionAssert.AreEqual(
                        expected: new[] { "2", "" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_RandomSample_SpaceBeforeStartQuote_TabAndCharAfterEndQuote()
        {
            using (var parser = CreateParser("edf\r\n \"	 \n,f\n\r\"\rfa,\"f\" 	c\n"))
            {
                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "edf" },
                    actual: parser.ReadFields()
                );

                if (CompatibilityMode)
                {
                    Assert.IsFalse(parser.EndOfData);
                    CollectionAssert.AreEqual(
                        expected: new[] { "\t \n,f\n" },
                        actual: parser.ReadFields()
                    );

                    Assert.IsFalse(parser.EndOfData);
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }
                else
                {
                    Assert.IsFalse(parser.EndOfData);
                    CollectionAssert.AreEqual(
                        expected: new[] { " \"\t " },
                        actual: parser.ReadFields()
                    );

                    Assert.IsFalse(parser.EndOfData);
                    CollectionAssert.AreEqual(
                        expected: new[] { "", "f" },
                        actual: parser.ReadFields()
                    );

                    Assert.IsFalse(parser.EndOfData);
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_RandomSample_TabAndCharAfterEndQuote()
        {
            using (var parser = CreateParser("\"f\" 	c"))
            {
                Assert.IsFalse(parser.EndOfData);
                Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_RandomSample_SpaceAndNewlinesAfterEndQuote()
        {
            using (var parser = CreateParser("eb\"b\tg,ag\"g\"dh\"\"hahb\tc\"\tagcb,\"ba\tdb\rfe\t\ng\t,b\" \r\ne \n"))
            {
                Assert.IsFalse(parser.EndOfData);
                if (CompatibilityMode)
                {
                    CollectionAssert.AreEqual(
                        expected: new[] { "eb\"b\tg", "ag\"g\"dh\"\"hahb\tc\"\tagcb", "ba\tdb\rfe\t\ng\t,b" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "e " },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_SpaceAndNewlinesAfterEndQuote([Values(true, false)]bool trimWhiteSpace)
        {
            using (var parser = CreateParser("\"a\" \r\n"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.IsFalse(parser.EndOfData);
                if (CompatibilityMode || trimWhiteSpace)
                {
                    CollectionAssert.AreEqual(
                        expected: new[] { "a" },
                        actual: parser.ReadFields()
                    );
                }
                else
                {
                    Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                }

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void ReadFields_BrokenQuotesSample()
        {
            using (var parser = CreateParser(@"""a""""b"""",""""c ""d"" e""""f"",""1"",""2, 3"",""4, 5"","""","""",""6"",""7"",""8"",""9"",""0"""))
            {
                Assert.IsFalse(parser.EndOfData);
                Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                Assert.IsTrue(parser.EndOfData);
            }
        }

        [TestCase(",")]
        [TestCase("|")]
        [TestCase("\t")]
        [TestCase("||")]
        public void SetDelimiter_ReadFields_Sample(string delimiterString)
        {
            using (var parser = CreateParser($@"1{delimiterString}2{delimiterString}3{Environment.NewLine}4{delimiterString}5"))
            {
                parser.SetDelimiter(delimiterString);

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1", "2", "3" },
                    actual: parser.ReadFields()
                );

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "4", "5" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [TestCase(",")]
        [TestCase("|")]
        [TestCase("\t")]
        [TestCase("||")]
        public void SetDelimiter_ReadFields_SampleWithQuotedDelimiters(string delimiterString)
        {
            using (var parser = CreateParser($@"1{delimiterString}test{delimiterString}""a{delimiterString}test""{delimiterString}2"))
            {
                parser.SetDelimiter(delimiterString);

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1", "test", $"a{delimiterString}test", "2" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [TestCase("||")]
        public void SetDelimiter_ReadFields_DelimiterLongerThanLine(string delimiterString)
        {
            using (var parser = CreateParser("1"))
            {
                parser.SetDelimiter(delimiterString);

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1" },
                    actual: parser.ReadFields()
                );

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [Test]
        public void SetDelimiters_EmptyString()
        {
            using (var parser = CreateParser("test"))
            {
                var ex = Assert.Throws<ArgumentException>(() => parser.Delimiters = new[] { string.Empty });
            }
        }

        [Test]
        public void SetDelimiters_NullDelimiter()
        {
            using (var parser = CreateParser("test"))
            {
                var ex = Assert.Throws<ArgumentException>(() => parser.Delimiters = new[] { default(string) });
            }
        }

        [Test]
        public void SetDelimiters_NewLine()
        {
            using (var parser = CreateParser("test"))
            {
                var ex = Assert.Throws<ArgumentException>(() => parser.Delimiters = new[] { "\n" });
            }
        }

        [Test]
        public void SetDelimiters_CarriageReturn()
        {
            using (var parser = CreateParser("test"))
            {
                var ex = Assert.Throws<ArgumentException>(() => parser.Delimiters = new[] { "\r" });
            }
        }

        [Test]
        public void SetDelimiter_NewLine()
        {
            using (var parser = CreateParser("test"))
            {
                var ex = Assert.Throws<ArgumentException>(() => parser.SetDelimiter("\n"));
            }
        }

        [Test]
        public void SetDelimiter_CarriageReturn()
        {
            using (var parser = CreateParser("test"))
            {
                var ex = Assert.Throws<ArgumentException>(() => parser.SetDelimiter("\r"));
            }
        }

        [TestCase(",")]
        [TestCase("||")]
        public void ErrorLine_BrokenQuotesThenValidLine(string delimiter)
        {
            using (var parser = CreateParser($"\"te\"st\n2{delimiter}22"))
            {
                parser.SetDelimiter(delimiter);

                Assert.IsFalse(parser.EndOfData);
                var ex = Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                Assert.AreEqual("\"te\"st", parser.ErrorLine);
                Assert.AreEqual(1, parser.ErrorLineNumber);

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "2", "22" },
                    actual: parser.ReadFields()
                );

                Assert.AreEqual("\"te\"st", parser.ErrorLine);
                Assert.AreEqual(1, parser.ErrorLineNumber);

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [TestCase(",")]
        [TestCase("||")]
        public void ErrorLine_QuoteNeverClosed_MultipleLinesInErrorField(string delimiter)
        {
            using (var parser = CreateParser($"1\n\"2{delimiter}22\n3\r\n4"))
            {
                parser.SetDelimiter(delimiter);

                Assert.IsFalse(parser.EndOfData);
                CollectionAssert.AreEqual(
                    expected: new[] { "1" },
                    actual: parser.ReadFields()
                );

                Assert.IsFalse(parser.EndOfData);
                var ex = Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                Assert.AreEqual($"\"2{delimiter}22\n3\r\n4", parser.ErrorLine);
                Assert.AreEqual(2, parser.ErrorLineNumber);

                Assert.IsTrue(parser.EndOfData);
            }
        }

        [TestCase(",")]
        [TestCase("||")]
        public void ErrorLine_MaybeResetAfterClose(string delimiter)
        {
            ITextFieldParser parser;
            using (parser = CreateParser("\"te\"st"))
            {
                parser.SetDelimiter(delimiter);

                Assert.IsFalse(parser.EndOfData);
                var ex = Assert.Throws<CsvMalformedLineException>(() => parser.ReadFields());
                Assert.AreEqual("\"te\"st", parser.ErrorLine);
                Assert.AreEqual(1, parser.ErrorLineNumber);

                Assert.IsTrue(parser.EndOfData);
            }

            if (CompatibilityMode)
            {
                Assert.AreEqual("\"te\"st", parser.ErrorLine);
                Assert.AreEqual(1, parser.ErrorLineNumber);
            }
            else
            {
                Assert.AreEqual(string.Empty, parser.ErrorLine);
                Assert.AreEqual(-1L, parser.ErrorLineNumber);
            }
        }

        [Test]
        public void LineNumber_SingleValue([Values(true, false)]bool checkEndOfData)
        {
            using (var parser = CreateParser("1"))
            {
                Assert.AreEqual(1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsFalse(parser.EndOfData);
                    Assert.AreEqual(1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(-1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsTrue(parser.EndOfData);
                    Assert.AreEqual(-1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(-1, parser.LineNumber);
            }
        }

        [Test]
        public void LineNumber_TwoRows_TwoValues([Values(true, false)]bool checkEndOfData)
        {
            using (var parser = CreateParser($"1,2{Environment.NewLine}3,4"))
            {
                Assert.AreEqual(1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsFalse(parser.EndOfData);
                    Assert.AreEqual(1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(2, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsFalse(parser.EndOfData);
                    Assert.AreEqual(2, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(-1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsTrue(parser.EndOfData);
                    Assert.AreEqual(-1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(-1, parser.LineNumber);
            }
        }

        [Test]
        public void LineNumber_SampleWithQuotedNewlines([Values(true, false)]bool trimWhiteSpace, [Values(true, false)]bool checkEndOfData)
        {
            using (var parser = CreateParser($"\"newline{Environment.NewLine}test\",2\nsecond line,3\n\"third\rline\n\r\n\n\",4"))
            {
                parser.TrimWhiteSpace = trimWhiteSpace;

                Assert.AreEqual(1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsFalse(parser.EndOfData);
                    Assert.AreEqual(1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(3, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsFalse(parser.EndOfData);
                    Assert.AreEqual(3, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(4, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsFalse(parser.EndOfData);
                    Assert.AreEqual(4, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(-1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsTrue(parser.EndOfData);
                    Assert.AreEqual(-1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(-1, parser.LineNumber);
            }
        }

        [Test]
        public void LineNumber_SampleWithBlankLine([Values(true, false)]bool checkEndOfData)
        {
            using (var parser = CreateParser($"1,2\n3,4\n\n5,6\n7"))
            {
                Assert.AreEqual(1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsFalse(parser.EndOfData);
                    Assert.AreEqual(1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(2, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsFalse(parser.EndOfData);
                    Assert.AreEqual(2, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(3, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsFalse(parser.EndOfData);
                    Assert.AreEqual(3, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(5, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsFalse(parser.EndOfData);
                    Assert.AreEqual(5, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(-1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsTrue(parser.EndOfData);
                    Assert.AreEqual(-1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(-1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsTrue(parser.EndOfData);
                    Assert.AreEqual(-1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(-1, parser.LineNumber);
            }
        }

        [Test]
        public void LineNumber_SampleWithTrailingBlankLines([Values(true, false)]bool checkEndOfData)
        {
            using (var parser = CreateParser(",2,3\n,9\n\n\n"))
            {
                Assert.AreEqual(1, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsFalse(parser.EndOfData);
                    Assert.AreEqual(1, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(2, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsFalse(parser.EndOfData);
                    Assert.AreEqual(2, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(3, parser.LineNumber);

                if (checkEndOfData)
                {
                    Assert.IsTrue(parser.EndOfData);
                    Assert.AreEqual(3, parser.LineNumber);
                }

                parser.ReadFields();
                Assert.AreEqual(-1, parser.LineNumber);

                for (var i = 0; i < 10; i++)
                {
                    if (checkEndOfData)
                    {
                        Assert.IsTrue(parser.EndOfData);
                        Assert.AreEqual(-1, parser.LineNumber);
                    }

                    parser.ReadFields();
                    Assert.AreEqual(-1, parser.LineNumber);
                }
            }
        }
    }
}
