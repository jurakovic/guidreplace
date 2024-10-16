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
			var rootCommand = new RootCommand("Guid Replace tool\nFor more information, visit https://github.com/jurakovic/guidreplace");

			var inPlaceOption = new Option<bool>(new[] { "--in-place", "-i" }, "Edit the input file in place.");

			var inputFileArgument = new Argument<string>("inputFile", "The input file to process. If not specified, reads from standard input.")
			{
				Arity = ArgumentArity.ZeroOrOne
			};

			var outputFileOption = new Option<string?>(new[] { "--output", "-o" }, "The output file to write the result to.");


			rootCommand.Add(inPlaceOption);
			rootCommand.Add(inputFileArgument);
			rootCommand.Add(outputFileOption);

			rootCommand.AddValidator(result =>
			{
				if (result.GetValueForOption(outputFileOption) != null &&
					result.GetValueForOption(inPlaceOption))
				{
					result.ErrorMessage = "Options --output and --in-place cannot be used together.";
				}
			});

			rootCommand.SetHandler(ExecuteAsync, inputFileArgument, inPlaceOption, outputFileOption);

			return rootCommand;
		}

		private static Task ExecuteAsync(string inputFilename, bool inPlaceReplace, string outputFilename)
		{
			//if (args.Length == 0)
			//{
			//	Console.WriteLine($"Run with \"guidreplace filename\" or drag-n-drop file to exe");
			//	Console.ReadKey();
			//	return;
			//}

			//string filename = args[0].Trim('"');

			if (!File.Exists(inputFilename))
			{
				Console.WriteLine($"File not found");
				//Console.WriteLine($"Press any key to exit");
				//Console.ReadKey();
				return Task.CompletedTask;
			}

			string text = File.ReadAllText(inputFilename);
			string output = ReplaceGuids(text, out int matchesCount, out int pairsCount);

			if (matchesCount <= 0)
			{
				Console.WriteLine($"No guids in file");
				//Console.WriteLine($"Press any key to exit");
				//Console.ReadKey();
				return Task.CompletedTask;
			}
			else
			{
				FileInfo fi = new FileInfo(inputFilename);
				string newName = $"{Path.GetFileNameWithoutExtension(fi.Name)}_{DateTime.Now.ToString("yyyy-MM-dd_HHmmss")}";
				string newFilename = $"{fi.Directory}\\{newName}{fi.Extension}";

				File.WriteAllText(newFilename, output);
				Console.WriteLine($"Done. {matchesCount} guids replaced, {pairsCount} pairs.");
				//Console.WriteLine($"Press any key to exit");

				return Task.CompletedTask;
			}
		}

		static string ReplaceGuids(string text, out int matchesCount, out int pairsCount)
		{
			matchesCount = 0;
			pairsCount = 0;

			string pattern = "[a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12}";

			Dictionary<Guid, Guid> pairs = new Dictionary<Guid, Guid>();
			MatchCollection matches = Regex.Matches(text, pattern);
			matchesCount = matches.Count;

			if (matchesCount <= 0)
				return String.Empty;

			int lastStart = 0;
			StringBuilder sb = new StringBuilder();

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
