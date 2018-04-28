A simple CSV parser based on Microsoft.VisualBasic.FileIO.TextFieldParser.

The goal of this library is to provide a simple alternative to the `TextFieldParser` class in the `Microsoft.VisualBasic` assembly that works with .NET Core, or just that doesn't require you to use a VisualBasic assembly to a C# project.


Installation
============
There's just one real file in this library: [CsvTextFieldParser](CsvTextFieldParser/CsvTextFieldParser.cs).  So the recommended way to use this is to just copy that file into your project.

This project is available under either of two licenses: [MIT](LICENSE) or [The Unlicense](UNLICENSE).  If you're able to use the unlicense, then you don't have to provide any attribution at all.  You want to throw this code into your own project and tell everyone you wrote it?  Go for it.

If you'd prefer to have a NuGet package, that might be an option in the future.


Getting Started
===============
You've got some CSV data you need to parse:

```csv
Name,Birth Date
Apollo Creed,1942-08-17
Ivan Drago,1961-11-03
```

That's easy, it only takes a few lines of code:

```c#
public IEnumerable<string[]> ParseBasicCsv(string input)
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

public void ProcessCsv(string csvInput)
{
	foreach (var csvLine in ParseBasicCsv(csvInput))
	{
		var name = csvLine[0];
		var birthDate = csvLine[1];
		Console.WriteLine($"{name} was born on {birthDate}");
	}
}
```

If that's the kind of CSV data you have, then throw this library in the garbage and just use that.

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

Now maybe it's time to consider a CSV library.  And what luck, the .NET library happens to have one built-in.  Here's your code now:

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

There's just one thing that's kind of gross about this solution: we have to add a dependency on the `Microsoft.VisualBasic` assembly to our C# project.  It's still .NET, so that should all work fine.  But it feels a little wrong, doesn't it?

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
* An quoted field is started but never closed by another unescaped quote character

Either of these cases will cause a `CsvMalformedLineException` to be thrown.

Getting information about these errors works just like handling `Microsoft.VisualBasic.FileIO.MalformedLineException` exceptions from the  `Microsoft.VisualBasic.FileIO.TextFieldParser` class:

* The exception contains a `LineNumber` property that tells you the number of the line in the file is malformed
* The parser class contains an `ErrorLine` property that provides the actual malformed line
* The parser class also contains an `ErrorLineNumber` property with the same value as the exception's `LineNumber` property

Here's an example of using this parser with error handling:

```c#
public void ProcessCsv(string csvInput)
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
```


Compatibility
=============
The goal of this library is to be compatible with `Microsoft.VisualBasic.FileIO.TextFieldParser`.  But the Visual Basic version of the parser has some strange behaviour in a few edge cases, mostly related to whitespace with quoted fields.  Some examples:

* Two or more consecutive newlines in a quoted field are collapsed down to just one newline
* Whitespace before a leading quote in a field is ignored (the entire field is parsed as a quoted character)
* Whitespace after a field's end quote character is either ignored or treated as a separate field (depending on whether it's the last field in the file)
* Non-empty lines of only whitespace are ignored

By default, this parser does not recreate those behaviors.

However, this parser does contain a compatibility mode to exactly match the VB parser for these cases.  The main reason this mode is included to make it possible to run unit tests that compare the results of this parser to the VB version.  But the mode is available to you as well if you need it:

```c#
var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader);
parser.CompatibilityMode = true;
```


Configuration
=============
There aren't a lot of configuration options available in this library, especially compared to the `Microsoft.VisualBasic.FileIO.TextFieldParser`.  But there are a few:

* `SetDelimiter(char)`: change the delimiter character from a comma (`,`) to a different character, such as a pipe (`|`) or a tab (`\t`)
* `SetQuoteCharacter(char)`: change the quote character from a double quote (`"`) to a different character, such as a single quote (`'`)
* `SetQuoteEscapeCharacter(char)`: change the quote character from a double quote (`"`) to a different character, such as a backslash (`\\`)

Here's an example of setting each of those options:

```c#
var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader);
parser.SetDelimiter('|');
parser.SetQuoteCharacter('\'');
parser.SetQuoteEscapeCharacter('\\');
```
