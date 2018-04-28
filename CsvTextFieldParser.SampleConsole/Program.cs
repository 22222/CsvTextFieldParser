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
            parser.SetQuoteCharacter('\'');
            parser.SetQuoteEscapeCharacter('\\');
        }
    }
}
