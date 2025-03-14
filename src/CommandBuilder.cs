using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GuidReplace
{
	public static class CommandBuilder
	{
		public static RootCommand CreateRootCommand()
		{
			var rootCommand = new RootCommand("GUID Replace tool\nFor more information, visit https://github.com/jurakovic/guidreplace");

			var inPlaceOption = new Option<bool>(["--in-place", "-i"], "Edit the input file in place");

			var inputFileArgument = new Argument<string>("inputFile", "The input file to process. If not specified, reads from standard input.")
			{
				Arity = ArgumentArity.ZeroOrOne
			};

			var outputFileOption = new Option<string>(["--output", "-o"], "The output file to write the result to");

			var quietOption = new Option<bool>(["--quiet", "-q"], "Do not output messages to standard output");

			rootCommand.Add(inPlaceOption);
			rootCommand.Add(inputFileArgument);
			rootCommand.Add(outputFileOption);
			rootCommand.Add(quietOption);

			rootCommand.AddValidator(result =>
			{
				if (result.GetValueForOption(outputFileOption) != null && result.GetValueForOption(inPlaceOption))
					result.ErrorMessage = "Options --output and --in-place cannot be used together.";
			});

			rootCommand.SetHandler(ExecuteAsync, inputFileArgument, inPlaceOption, outputFileOption, quietOption);

			return rootCommand;
		}

		private static async Task<int> ExecuteAsync(string inputFilename, bool inPlaceReplace, string outputFilename, bool quiet)
		{
			//System.Diagnostics.Debugger.Launch();

			string inputText;

			if (!String.IsNullOrEmpty(inputFilename))
			{
				if (!File.Exists(inputFilename))
				{
					if (!quiet)
						Console.Error.WriteLine($"File not found: {inputFilename}");
					return 1;
				}

				inputText = await File.ReadAllTextAsync(inputFilename);
			}
			else
			{
				if (Console.IsInputRedirected)
				{
					inputText = await Console.In.ReadToEndAsync();
				}
				else
				{
					if (!quiet)
						Console.Error.WriteLine("Error: No input file specified and no data in stdin. Run 'guidrep -h' for usage.");
					return 1;
				}
			}

			if (String.IsNullOrWhiteSpace(inputText))
			{
				if (!quiet)
					Console.Error.WriteLine($"Input text empty");
				return 1;
			}

			string outputText = ReplaceGuids(inputText, out int matchesCount, out int pairsCount);

			if (matchesCount <= 0)
			{
				if (!quiet)
					Console.Error.WriteLine($"No GUIDs in file");
				return 1;
			}

			if (!string.IsNullOrEmpty(outputFilename))
			{
				await File.WriteAllTextAsync(outputFilename, outputText);
			}
			else if (inPlaceReplace && !String.IsNullOrEmpty(inputFilename))
			{
				await File.WriteAllTextAsync(inputFilename, outputText);
			}
			else if (!String.IsNullOrEmpty(inputFilename))
			{
				string nameWithoutExt = Path.GetFileNameWithoutExtension(inputFilename);
				string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
				string extension = Path.GetExtension(inputFilename);
				string newFileName = $"{nameWithoutExt}_{timestamp}{extension}";
				await File.WriteAllTextAsync(newFileName, outputText);
			}
			else
			{
				await Console.Out.WriteAsync(outputText);
			}

			if (!quiet)
				Console.WriteLine($"Done. {matchesCount} GUIDs replaced, {pairsCount} pairs.");

			return 0;
		}

		static string ReplaceGuids(string text, out int matchesCount, out int pairsCount)
		{
			string pattern = "[a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12}";

			Dictionary<Guid, Guid> pairs = new Dictionary<Guid, Guid>();
			MatchCollection matches = Regex.Matches(text, pattern);

			matchesCount = matches.Count;
			pairsCount = 0;

			if (matchesCount <= 0)
				return null;

			int lastStart = 0;
			StringBuilder sb = new StringBuilder(text.Length);

			Match firstMatch = matches[0];
			string firstString = text.Substring(firstMatch.Index, 36);
			char firstLetter = firstString.SkipWhile(x => Char.IsDigit(x)).First();
			bool isUpper = Char.IsUpper(firstLetter);

			foreach (Match m in matches)
			{
				sb.Append(text.Substring(lastStart, m.Index - lastStart));
				Guid oldGuid = Guid.Parse(m.Value);

				if (!pairs.ContainsKey(oldGuid))
					pairs.Add(oldGuid, Guid.NewGuid());

				Guid newGuid = pairs[oldGuid];
				string newString = newGuid.ToString();

				if (isUpper)
					newString = newString.ToUpper();

				sb.Append(newString);
				lastStart = m.Index + m.Length;
			}

			pairsCount = pairs.Count;
			sb.Append(text.Substring(lastStart));
			return sb.ToString();
		}
	}
}
