using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Parse to dictionary:");
            var csvDictionaries = ParseCsvWithHeader(@"Name,Birth Date
Apollo Creed,1942-08-17
Ivan Drago,1961-11-03");

            Console.WriteLine(string.Join(Environment.NewLine, csvDictionaries.Select(csvDictionary => string.Join(",", csvDictionary.Select(kv => $"{kv.Key}={kv.Value}")))));
            Console.WriteLine();

            Console.WriteLine("Parse to dictionary with error handling:");
            csvDictionaries = ParseCsvWithHeaderIgnoreErrors(@"Name,Birth Date
Apollo Creed,1942-08-17
""broken""line
Ivan Drago,1961-11-03");

            Console.WriteLine(string.Join(Environment.NewLine, csvDictionaries.Select(csvDictionary => string.Join(",", csvDictionary.Select(kv => $"{kv.Key}={kv.Value}")))));
            Console.WriteLine();

            Console.WriteLine("Basic parser:");
            ProcessCsv(@"Name,Birth Date
Apollo Creed,1942-08-17
Ivan Drago,1961-11-03");

            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("VB parser:");
            ProcessCsv_Vb(@"Name,Birth Date
""Creed, Apollo"",1942-08-17
""Ivan 
Drago"",1961-11-03
""Robert """"Rocky"""" Balboa"",1945-07-06");

            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("This parser:");
            ProcessCsv_NotVb(@"Name,Birth Date
""Creed, Apollo"",1942-08-17
""Ivan 
Drago"",1961-11-03
""Robert """"Rocky"""" Balboa"",1945-07-06");

            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("This parser with an error:");
            ProcessCsv_NotVbWithErrorHandling(@"Name,Birth Date
""Creed, Apollo"",1942-08-17
""Iv""an 
Drago"",1961-11-03
""Robert """"Rocky"""" Balboa"",1945-07-06");

            Console.WriteLine();
            Console.WriteLine();

            CreateWithCompatibilityMode(new StringReader(""));
            CreateWithConfigurationOptions(new StringReader(""));
        }

        public static IEnumerable<string[]> ParseBasicCsv(string input)
        {
            using (var reader = new StringReader(input))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        yield return line.Split(',');
                    }
                }
            }
        }

        public static IEnumerable<IDictionary<string, string>> ParseCsvWithHeader(string csvInput)
        {
            using (var csvReader = new StringReader(csvInput))
            using (var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader))
            {
                if (parser.EndOfData)
                {
                    yield break;
                }
                string[] headerFields = parser.ReadFields();
                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    int fieldCount = Math.Min(headerFields.Length, fields.Length);
                    IDictionary<string, string> fieldDictionary = new Dictionary<string, string>(fieldCount);
                    for (var i = 0; i < fieldCount; i++)
                    {
                        string headerField = headerFields[i];
                        string field = fields[i];
                        fieldDictionary[headerField] = field;
                    }
                    yield return fieldDictionary;
                }
            }
        }

        public static IEnumerable<IDictionary<string, string>> ParseCsvWithHeaderIgnoreErrors(string csvInput)
        {
            using (var csvReader = new StringReader(csvInput))
            using (var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader))
            {
                if (parser.EndOfData)
                {
                    yield break;
                }
                string[] headerFields;
                try
                {
                    headerFields = parser.ReadFields();
                }
                catch (NotVisualBasic.FileIO.CsvMalformedLineException ex)
                {
                    Console.Error.WriteLine($"Failed to parse header line {ex.LineNumber}: {parser.ErrorLine}");
                    yield break;
                }
                while (!parser.EndOfData)
                {
                    string[] fields;
                    try
                    {
                        fields = parser.ReadFields();
                    }
                    catch (NotVisualBasic.FileIO.CsvMalformedLineException ex)
                    {
                        Console.Error.WriteLine($"Failed to parse line {ex.LineNumber}: {parser.ErrorLine}");
                        continue;
                    }

                    int fieldCount = Math.Min(headerFields.Length, fields.Length);
                    IDictionary<string, string> fieldDictionary = new Dictionary<string, string>(fieldCount);
                    for (var i = 0; i < fieldCount; i++)
                    {
                        string headerField = headerFields[i];
                        string field = fields[i];
                        fieldDictionary[headerField] = field;
                    }
                    yield return fieldDictionary;
                }
            }
        }

        public static void ProcessCsv(string csvInput)
        {
            foreach (var csvLine in ParseBasicCsv(csvInput).Skip(1))
            {
                var name = csvLine[0];
                var birthDate = csvLine[1];
                Console.WriteLine($"{name} was born on {birthDate}");
            }
        }

        public static void ProcessCsv_Vb(string csvInput)
        {
            using (var csvReader = new StringReader(csvInput))
            using (var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(csvReader))
            {
                parser.SetDelimiters(",");
                parser.TrimWhiteSpace = false;

                // Skip the header line
                if (!parser.EndOfData) parser.ReadFields();

                while (!parser.EndOfData)
                {
                    var csvLine = parser.ReadFields();
                    var name = csvLine[0];
                    var birthDate = csvLine[1];
                    Console.WriteLine($"{name} was born on {birthDate}");
                }
            }
        }

        public static void ProcessCsv_NotVb(string csvInput)
        {
            using (var csvReader = new StringReader(csvInput))
            using (var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader))
            {
                // Skip the header line
                if (!parser.EndOfData) parser.ReadFields();

                while (!parser.EndOfData)
                {
                    var csvLine = parser.ReadFields();
                    var name = csvLine[0];
                    var birthDate = csvLine[1];
                    Console.WriteLine($"{name} was born on {birthDate}");
                }
            }
        }

        public static void ProcessCsv_NotVbWithErrorHandling(string csvInput)
        {
            using (var csvReader = new StringReader(csvInput))
            using (var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader))
            {
                // Skip the header line
                if (!parser.EndOfData) parser.ReadFields();

                while (!parser.EndOfData)
                {
                    try
                    {
                        var csvLine = parser.ReadFields();
                        var name = csvLine[0];
                        var birthDate = csvLine[1];
                        Console.WriteLine($"{name} was born on {birthDate}");
                    }
                    catch (NotVisualBasic.FileIO.CsvMalformedLineException ex)
                    {
                        Console.Error.WriteLine($"Failed to parse line {ex.LineNumber}: {parser.ErrorLine}");
                    }
                }
            }
        }

        public static void CreateWithCompatibilityMode(TextReader csvReader)
        {
            var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader);
            parser.CompatibilityMode = true;
        }

        public static void CreateWithConfigurationOptions(TextReader csvReader)
        {
            var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader);
            parser.SetDelimiter('|');
            parser.Delimiters = new[] { "|" };
            parser.SetQuoteCharacter('\'');
            parser.SetQuoteEscapeCharacter('\\');
            parser.HasFieldsEnclosedInQuotes = false;
            parser.TrimWhiteSpace = true;
        }

        public static void ProcessReadLineFromReader(string csvInput)
        {
            using (var csvReader = new StringReader(csvInput))
            using (var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader))
            {
                var fields = parser.ReadFields();
                var line = csvReader.ReadLine();
            }
        }

        public static IEnumerable<string> ProcessReadLineExtensionMethod(string csvInput)
        {
            using (var csvReader = new StringReader(csvInput))
            using (var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader))
            {
                string line;
                while ((line = parser.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }

    public static class CsvTextFieldParserExtensions
    {
        public static string ReadLine(this NotVisualBasic.FileIO.CsvTextFieldParser parser)
        {
            var fields = parser.ReadFields();
            if (fields == null) return null;
            
            return string.Join(",", fields.Select(SerializeValue));
        }

        private static string SerializeValue(string value)
        {
            if (value == null) return null;

            if (value.IndexOfAny(new char[] { ',', '"', '\n', '\r' }) >= 0)
            {
                return '"' + value.Replace("\"", "\"\"") + '"';
            }
            return value;
        }
    }
}
