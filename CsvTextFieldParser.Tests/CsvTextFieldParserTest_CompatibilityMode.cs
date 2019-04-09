using System.IO;

namespace NotVisualBasic.FileIO
{
    /// <summary>
    /// These tests are to make sure that the <see cref="CsvTextFieldParser"/> tests also pass when it's in compatibility mode.
    /// </summary>
    public class CsvTextFieldParserTest_CompatibilityMode : CsvTextFieldParserTest
    {
        protected override bool CompatibilityMode => true;
        protected override ITextFieldParser CreateParser(string input) => new CsvTextFieldParserAdapter(new StringReader(input), isCompatibilityMode: true);
    }
}
