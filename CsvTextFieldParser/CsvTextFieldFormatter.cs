using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NotVisualBasic.FileIO
{
    /// <summary>
    /// Formats to comma-delimited text files.
    /// </summary>
    public class CsvTextFieldFormatter : IDisposable
    {
        private TextWriter writer;
        private char delimiterChar = ',';
        private char quoteChar = '"';
        private char quoteEscapeChar = '"';
        private string eol;
        private char[] specialCharsCache;

        /// <summary>
        /// Constructs a formatter from the specified output stream.
        /// </summary>
        public CsvTextFieldFormatter(Stream stream)
            : this(new StreamWriter(stream)) { }

        /// <summary>
        /// Constructs a formatter from the specified output stream with the specified encoding.
        /// </summary>
        public CsvTextFieldFormatter(Stream stream, Encoding encoding)
            : this(new StreamWriter(stream, encoding)) { }

        /// <summary>
        /// Constructs a formatter from the specified output stream with the specified encoding, and optionally leaves the stream open.
        /// </summary>
        public CsvTextFieldFormatter(Stream stream, Encoding encoding, bool leaveOpen)
            : this(new StreamWriter(stream, encoding, 1024, leaveOpen)) { }

        /// <summary>
        /// Constructs a formatter from the specified output file path.
        /// </summary>
        public CsvTextFieldFormatter(string path)
            : this(new StreamWriter(path)) { }

        /// <summary>
        /// Constructs a formatter from the specified output file path with the specified encoding.
        /// If the file exists, it can be either overwritten or appended to.
        /// If the file does not exist, this constructor creates a new file.
        /// </summary>
        public CsvTextFieldFormatter(string path, bool append)
            : this(new StreamWriter(path, append)) { }

        /// <summary>
        /// Constructs a formatter from the specified output file path with the specified encoding.
        /// If the file exists, it can be either overwritten or appended to.
        /// If the file does not exist, this constructor creates a new file.
        /// </summary>
        public CsvTextFieldFormatter(string path, bool append, Encoding encoding)
            : this(new StreamWriter(path, append, encoding)) { }

        /// <summary>
        /// Constructs a formatter from the specified input text writer.
        /// </summary>
        public CsvTextFieldFormatter(TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            this.writer = writer;
        }

        /// <summary>
        /// Writes the fields to a new line.
        /// </summary>
        /// <param name="fields">An array of strings that contains field values for the current line.</param>
        public void WriteFields(IEnumerable<string> fields)
        {
            if (writer == null) throw new ObjectDisposedException(nameof(writer));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            using (var enumerator = fields.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    WriteField(enumerator.Current);
                    while (enumerator.MoveNext())
                    {
                        writer.Write(delimiterChar);
                        WriteField(enumerator.Current);
                    }
                }
            }
            WriteEol();
        }

        private void WriteField(string field)
        {
            var isQuoteNecessary = field.IndexOfAny(SpecialChars) >= 0;
            if (isQuoteNecessary)
            {
                writer.Write(quoteChar);
                foreach (var ch in field)
                {
                    if (ch == quoteChar)
                    {
                        writer.Write(quoteEscapeChar);
                    }
                    writer.Write(ch);
                }
                writer.Write(quoteChar);
            }
            else
            {
                writer.Write(field);
            }
        }

        private void WriteEol()
        {
            if (eol != null)
            {
                writer.Write(eol);
            }
            else
            {
                writer.WriteLine();
            }
        }

        private char[] SpecialChars
        {
            get
            {
                var specialChars = specialCharsCache;
                if (specialChars == null)
                {
                    specialChars = new SortedSet<char> { delimiterChar, quoteChar, quoteEscapeChar, '\n', '\r' }.ToArray();
                    specialCharsCache = specialChars;
                }
                return specialChars;
            }
        }

        /// <summary>
        /// Closes the current <see cref="CsvTextFieldFormatter"/> object.
        /// </summary>
        public void Close()
        {
            if (writer != null)
            {
                writer.Close();
                writer = null;
            }
        }

        /// <summary>
        /// Closes and disposes the current <see cref="CsvTextFieldFormatter"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the current <see cref="CsvTextFieldFormatter"/> object.
        /// </summary>
        /// <param name="disposing">true if called from <see cref="Dispose()"/>, or false if called from a finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
        }

        #region Configuration

        /// <summary>
        /// Defines the delimiters for a text file.
        /// Default is a comma.
        /// </summary>
        /// <remarks>
        /// This is defined as an array of strings for compatibility with <code>Microsoft.VisualBasic.FileIO.TextFieldParser</code>,
        /// but this formatter only supports one single-character delimiter.
        /// </remarks>
        /// <exception cref="ArgumentException">A delimiter value is set to a newline character, an empty string, or null.</exception>
        /// <exception cref="NotSupportedException">The delimiters are set to an array that does not contain exactly one element with exactly one character.</exception>
        public string[] Delimiters
        {
            get
            {
                return new string[] { delimiterChar.ToString(CultureInfo.InvariantCulture) };
            }
            set
            {
                if (value == null || !value.Any())
                {
                    throw new NotSupportedException("This formatter requires a delimiter");
                }
                if (value.Length > 1)
                {
                    throw new NotSupportedException("This formatter does not support multiple delimiters.");
                }

                var delimiterString = value.Single();
                if (string.IsNullOrEmpty(delimiterString))
                {
                    throw new ArgumentException("A delimiter cannot be null or an empty string.");
                }
                if (delimiterString.Length > 1)
                {
                    throw new NotSupportedException("This formatter does not support a delimiter with multiple characters.");
                }
                SetDelimiter(delimiterString.Single());
            }
        }

        /// <summary>
        /// Sets the delimiter character used by this formatter.
        /// Default is a comma.
        /// </summary>
        /// <exception cref="ArgumentException">The delimiter character is set to a newline character.</exception>
        public void SetDelimiter(char delimiterChar)
        {
            if (delimiterChar == '\n' || delimiterChar == '\r')
            {
                throw new ArgumentException("This formatter does not support delimiters that contain end-of-line characters");
            }

            this.delimiterChar = delimiterChar;
            this.specialCharsCache = null;
        }

        /// <summary>
        /// Sets the quote character used by this formatter, and also sets the quote escape character to match if it previously matched.
        /// Default is a double quote character.
        /// </summary>
        /// <exception cref="ArgumentException">The quote character is set to a newline character.</exception>
        public void SetQuoteCharacter(char quoteChar)
        {
            if (quoteChar == '\n' || quoteChar == '\r')
            {
                throw new ArgumentException("This formatter does not support end-of-line characters as a quote character");
            }

            // If the quote and escape characters currently match, then make sure they still match after we change the quote character.
            if (this.quoteChar == this.quoteEscapeChar)
            {
                this.quoteEscapeChar = quoteChar;
            }
            this.quoteChar = quoteChar;
            this.specialCharsCache = null;
        }

        /// <summary>
        /// Sets the quote escape character used by this formatter.
        /// Default is the same as the quote character, a double quote character.
        /// </summary>
        /// <exception cref="ArgumentException">The quote escape character is set to a newline character.</exception>
        public void SetQuoteEscapeCharacter(char quoteEscapeChar)
        {
            if (quoteEscapeChar == '\n' || quoteEscapeChar == '\r')
            {
                throw new ArgumentException("This formatter does not support end-of-line characters as a quote escape character");
            }
            this.quoteEscapeChar = quoteEscapeChar;
            this.specialCharsCache = null;
        }

        /// <summary>
        /// Sets the quote escape character used by this formatter.
        /// Default is <see cref="Environment.NewLine"/>.
        /// </summary>
        /// <param name="eol">The end-of-line character(s).</param>
        /// <exception cref="ArgumentException">The end-of-line characters are not some combination of '\r', '\n', or "\r\n".</exception>
        public void SetEndOfLine(string eol)
        {
            if (string.IsNullOrEmpty(eol))
            {
                this.eol = null;
                return;
            }

            if (!eol.Equals("\n") && !eol.Equals("\r\n") && !eol.Equals("\r"))
            {
                throw new ArgumentException(@"This formatter does not support end-of-line characters other than '\r', '\n', or ""\r\n""");
            }
            this.eol = eol;
        }

        #endregion
    }
}
