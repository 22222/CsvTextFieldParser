A simple CSV parser based on Microsoft.VisualBasic.FileIO.TextFieldParser.

![Build status](https://github.com/22222/CsvTextFieldParser/actions/workflows/build.yml/badge.svg)

The goal of this library is to provide a simple alternative to the `TextFieldParser` class in the `Microsoft.VisualBasic` assembly that works with .NET Core, or just that doesn't require you to use a VisualBasic assembly to a C# project.


Installation
============
There's just one real file in this library: [CsvTextFieldParser](CsvTextFieldParser/CsvTextFieldParser.cs).  So the recommended way to use this is to just copy that file into your project.

This project is available under either of two licenses: [MIT](LICENSE) or [The Unlicense](UNLICENSE).  If you're able to use the unlicense, then you don't have to provide any attribution at all.  You want to throw this code into your own project and tell everyone you wrote it?  Go for it.

Alternatively, there are a couple more traditional ways to use this library:

* Install the [NuGet package](https://www.nuget.org/packages/CsvTextFieldParser/)
* Download the assembly from the [latest release](https://github.com/22222/CsvTextFieldParser/releases/latest) and reference it manually


Getting Started
===============
Using this library is almost exactly the same as using the VisualBasic `TextFieldParser` class.  Here's an example of using this library to parse CSV input into dictionaries:

```c#
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
```

But let's take a step back.  You've got some CSV data you need to parse:

```csv
Name,Birth Date
Apollo Creed,1942-08-17
Ivan Drago,1961-11-03
```

That's easy, it only takes a few lines of code:

```c#
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
```

If that's the kind of CSV data you have, then throw this library in the garbage and just use that.  Parsing CSV data is simple when your values will never contain any characters that require special handling.

But wait, now they've changed the format of the CSV file.  Some of the values contain commas, so now you have to put those values in quotes:

```csv
Name,Birth Date
"Creed, Apollo",1942-08-17
Ivan Drago,1961-11-03
```

Everything's a little more complicated now  And maybe it turns out they sometimes put newlines in those quoted values, and some of the quoted values also have quote characters in them:

```csv
Name,Birth Date
"Creed, Apollo",1942-08-17
"Ivan 
Drago",1961-11-03
"Robert ""Rocky"" Balboa",1945-07-06
```

Now maybe it's time to consider a CSV library.  And what luck, the .NET standard library happens to have one built-in.  Here's your code now:

```c#
public static void ProcessCsv(string csvInput)
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
```

That works great.  It's kind of a weird library, but it handles the quoting thing so you can have commas and newlines in your fields now.  So once again, this library can go straight into the garbage.

But there's one thing that's kind of gross about this solution: we have to add a dependency on the `Microsoft.VisualBasic` assembly to our C# project.  It's still .NET, so that should all work fine.  But it feels a little wrong, doesn't it?

And there's another problem.  The `Microsoft.VisualBasic` probably isn't in .NET Standard, and probably won't ever be.  So if you want CSV parsing in a .NET Core application, you're probably back to having to pick a library.

One option is to use this one.  It's almost exactly the same as using the VisualBasic TextFieldParser:

```c#
public static void ProcessCsv(string csvInput)
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
```

So you can start with either this parser or the VisualBasic one, and you should be able to freely switch between them with only minimal code changes.


Error Handling
==============
There are two types of input that can cause an error when parsing a CSV file:

* A quoted field has trailing characters after the closing quote character
* A quoted field is started but never closed by another unescaped quote character

Either of these cases will cause a `CsvMalformedLineException` to be thrown.

Getting information about these errors works just like handling `Microsoft.VisualBasic.FileIO.MalformedLineException` exceptions from the  `Microsoft.VisualBasic.FileIO.TextFieldParser` class:

* The exception contains a `LineNumber` property that tells you the number of the line in the file is malformed
* The parser class contains an `ErrorLine` property that provides the actual malformed line
* The parser class also contains an `ErrorLineNumber` property with the same value as the exception's `LineNumber` property

Here's an example of using this parser with error handling:

```c#
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
```


Configuration
=============
The configuration options available in this library are mostly based on the ones in the `Microsoft.VisualBasic.FileIO.TextFieldParser`.  They include:

* `SetDelimiter(char)` or `Delimiters`: change the delimiter character from a comma (`,`) to a different character, such as a pipe (`|`) or a tab (`\t`)
* `SetQuoteCharacter(char)`: change the quote character from a double quote (`"`) to a different character, such as a single quote (`'`)
* `SetQuoteEscapeCharacter(char)`: change the quote character from a double quote (`"`) to a different character, such as a backslash (`\\`)
* `HasFieldsEnclosedInQuotes`: set to false if you want to skip all special character handling
* `TrimWhiteSpace`: set to true if you want to ignore whitespace at the beginning and end of lines and in between fields

Here's an example of setting each of those options:

```c#
var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader);
parser.SetDelimiter('|');
parser.Delimiters = new[] { "|" };
parser.SetQuoteCharacter('\'');
parser.SetQuoteEscapeCharacter('\\');
parser.HasFieldsEnclosedInQuotes = false;
parser.TrimWhiteSpace = true;
```


Compatibility
=============
The goal of this library is to be compatible with `Microsoft.VisualBasic.FileIO.TextFieldParser`.  But if you want to use this library as a drop-in replacement, you'll find that creating and configuring the parser isn't exactly the same.  The differences include:

* The class name and namespace are different: `NotVisualBasic.FileIO.CsvTextFieldParser` instead of `Microsoft.VisualBasic.FileIO.TextFieldParser`
* The exception class thrown for bad input is different: `CsvMalformedLineException` instead of `MalformedLineException`
* Any features not related to parsing CSV files are missing (like allowing multiple or no delimiters, the `SetFieldWidths` option)
* Some options have different default values
    * `Delimiter`: this library defaults to a comma (`,`), TextFieldParser has no default delimiter
    * `TrimWhiteSpace`: this library defaults to false, TextFieldParser defaults to true

After the parser is created and configured, using this parser should be pretty much exactly the same as using TextFieldParser.  But the Visual Basic TextFieldParser has strange behaviour in a few edge cases, mostly related to whitespace with quoted fields.  Some examples:

* Two or more consecutive newlines in a quoted field are collapsed down to just one newline
* Whitespace before a leading quote in a field is ignored even when the `TrimWhitespace` is false (the entire field is parsed as a quoted character)
* Whitespace after a field's end quote character is either ignored or treated as a separate field depending on whether it's the last field in the file
* Non-empty lines of only whitespace are ignored

By default, this parser does not recreate those behaviors.  However, this parser does contain a compatibility mode to exactly match the VB parser for these cases.  The main reason this mode is included to make it possible to run unit tests that compare the results of this parser to the VB version.  But the mode is available to you as well if you need it:

```c#
var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader);
parser.CompatibilityMode = true;
```
