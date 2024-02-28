using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GuidReplace
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine($"run with \"guidreplace filename\" or drag-n-drop file to exe");
				Console.ReadKey();
			}
			else
			{
				string filename = args[0].Trim('"');

				if (!File.Exists(filename))
				{
					Console.WriteLine($"file not found");
					Console.ReadKey();
					return;
				}

				string text = File.ReadAllText(filename);
				string pattern = "[a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12}";

				Dictionary<Guid, Guid> pairs = new Dictionary<Guid, Guid>();
				MatchCollection matches = Regex.Matches(text, pattern);

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

				sb.Append(text.Substring(lastStart));

				FileInfo fi = new FileInfo(filename);
				string newName = $"{Path.GetFileNameWithoutExtension(fi.Name)}_{DateTime.Now.ToString("yyyy-MM-dd_HHmmss")}";
				string newFilename = $"{fi.Directory}\\{newName}{fi.Extension}";

				File.WriteAllText(newFilename, sb.ToString());
				Console.WriteLine($"Done. {matches.Count} guids replaced, {pairs.Count} pairs.");
				Console.ReadKey();
			}
		}
	}
}
