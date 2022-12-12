using System;
using System.IO;
using Xunit;

namespace NotVisualBasic.FileIO
{
    public class CsvTextFieldFormatterTest
    {
        private static CsvTextFieldFormatterExtended CreateFormatter()
        {
            var formatter = new CsvTextFieldFormatterExtended();
            formatter.SetEndOfLine("\n");
            return formatter;
        }

        private class CsvTextFieldFormatterExtended : CsvTextFieldFormatter
        {
            private StringWriter stringWriter;

            public CsvTextFieldFormatterExtended()
                : this(new StringWriter()) { }

            private CsvTextFieldFormatterExtended(StringWriter stringWriter)
                : base(stringWriter)
            {
                this.stringWriter = stringWriter;
            }

            public string ToCsv()
            {
                return stringWriter.ToString();
            }
        }

        private static CsvTextFieldParser CreateParser(string csv)
        {
            var parser = new CsvTextFieldParser(new StringReader(csv));
            return parser;
        }

        [Fact]
        public void WriteFields_SingleValue()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { "1" });
                csv = formatter.ToCsv();
            }
            Assert.Equal("1\n", csv);

            using (var parser = CreateParser(csv))
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
        public void WriteFields_TwoRows_SingleValue()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { "1" });
                formatter.WriteFields(new[] { "2" });
                csv = formatter.ToCsv();
            }
            Assert.Equal("1\n2\n", csv);

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1" },
                    actual: parser.ReadFields()
                );
                Assert.Equal(
                    expected: new[] { "2" },
                    actual: parser.ReadFields()
                );
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void WriteFields_TwoValues()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { "1", "2" });
                csv = formatter.ToCsv();
            }
            Assert.Equal("1,2\n", csv);

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1", "2" },
                    actual: parser.ReadFields()
                );
                Assert.True(parser.EndOfData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteFields_TwoRows_TwoValues(bool forceFieldsEnclosedInQuotes)
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.ForceFieldsEnclosedInQuotes = forceFieldsEnclosedInQuotes;
                formatter.WriteFields(new[] { "1", "2" });
                formatter.WriteFields(new[] { "3", "4" });
                csv = formatter.ToCsv();
            }

            if (forceFieldsEnclosedInQuotes)
            {
                Assert.Equal("\"1\",\"2\"\n\"3\",\"4\"\n", csv);
            }
            else
            {
                Assert.Equal("1,2\n3,4\n", csv);
            }

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1", "2" },
                    actual: parser.ReadFields()
                );
                Assert.Equal(
                    expected: new[] { "3", "4" },
                    actual: parser.ReadFields()
                );
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void WriteFields_Empty()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                csv = formatter.ToCsv();
            }
            Assert.Equal(string.Empty, csv);

            using (var parser = CreateParser(csv))
            {
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void WriteFields_CommaOnly()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { "", "" });
                csv = formatter.ToCsv();
            }
            Assert.Equal(",\n", csv);

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "", "" },
                    actual: parser.ReadFields()
                );
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void WriteFields_SpaceOnly()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { " " });
                csv = formatter.ToCsv();
            }
            Assert.Equal(" \n", csv);

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { " " },
                    actual: parser.ReadFields()
                );
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void WriteFields_CrOnly()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { "\r" });
                csv = formatter.ToCsv();
            }
            Assert.Equal("\"\r\"\n", csv);

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "\r" },
                    actual: parser.ReadFields()
                );
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void WriteFields_LfOnly()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { "\n" });
                csv = formatter.ToCsv();
            }
            Assert.Equal("\"\n\"\n", csv);

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "\n" },
                    actual: parser.ReadFields()
                );
                Assert.True(parser.EndOfData);
            }
        }


        [Fact]
        public void WriteFields_CrlfOnly()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { "\r\n" });
                csv = formatter.ToCsv();
            }
            Assert.Equal("\"\r\n\"\n", csv);

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "\r\n" },
                    actual: parser.ReadFields()
                );
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void WriteFields_QuoteOnly()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { "\"" });
                csv = formatter.ToCsv();
            }
            Assert.Equal("\"\"\"\"\n", csv);

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "\"" },
                    actual: parser.ReadFields()
                );
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void WriteFields_SampleWithQuotedCommas()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { "1", "test", "a,test", "2" });
                csv = formatter.ToCsv();
            }
            Assert.Equal("1,test,\"a,test\",2\n", csv);

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "1", "test", "a,test", "2" },
                    actual: parser.ReadFields()
                );
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void WriteFields_SampleWithQuotedNewlines()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { "newline\ntest", "2" });
                formatter.WriteFields(new[] { "second line", "3" });
                csv = formatter.ToCsv();
            }
            Assert.Equal("\"newline\ntest\",2\nsecond line,3\n", csv);

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "newline\ntest", "2" },
                    actual: parser.ReadFields()
                );
                Assert.Equal(
                    expected: new[] { "second line", "3" },
                    actual: parser.ReadFields()
                );
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void WriteFields_SampleWithQuotedQuotes()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { "a \"quote\" test", "2" });
                formatter.WriteFields(new[] { "\"completely quoted\"", "3" });
                csv = formatter.ToCsv();
            }
            Assert.Equal("\"a \"\"quote\"\" test\",2\n\"\"\"completely quoted\"\"\",3\n", csv);

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
                Assert.Equal(
                    expected: new[] { "a \"quote\" test", "2" },
                    actual: parser.ReadFields()
                );
                Assert.Equal(
                    expected: new[] { "\"completely quoted\"", "3" },
                    actual: parser.ReadFields()
                );
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void WriteFields_SampleWithLeadingAndTrailingWhitespace()
        {
            string csv;
            using (var formatter = CreateFormatter())
            {
                formatter.WriteFields(new[] { "a", " b", "c ", " d ", " e", "f ", " g " });
                formatter.WriteFields(new[] { " h" });
                formatter.WriteFields(new[] { "i " });
                formatter.WriteFields(new[] { " j " });
                csv = formatter.ToCsv();
            }
            Assert.Equal("a, b,c , d , e,f , g \n h\ni \n j \n", csv);

            using (var parser = CreateParser(csv))
            {
                Assert.False(parser.EndOfData);
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

                Assert.True(parser.EndOfData);
            }
        }
    }
}
