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
    /// These tests are to make sure that the <see cref="CsvTextFieldParser"/> tests also pass when it's in compatibility mode.
    /// </summary>
    public class CsvTextFieldParserTest_CompatibilityMode : CsvTextFieldParserTest
    {
        protected override bool CompatibilityMode => true;
        protected override ITextFieldParser CreateParser(string input) => new CsvTextFieldParserAdapter(new StringReader(input), isCompatibilityMode: true);
    }
}
