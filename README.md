
# GUID Replace

`guidrep` is a simple command-line tool for replacing GUIDs (UUIDs) in text files or standard input. It reads the input, replaces all GUIDs with new randomly generated ones, and ensures that any repeated GUIDs in the original are replaced with the same new GUID.

This tool is especially useful for preparing JSON data for re-import, anonymizing data exports, or resetting identifiers in configuration files.

### Features

- **File Processing**
	- replace GUIDs in a specified file, with options for in-place editing or outputting to a new file
- **Standard Input/Output Support**
	- works with piped input and outputs to standard output
- **Flexible Output**
	- save the results to a specified output file, or automatically generate a new filename based on the original
- **Cross-Platform**
	- runs on Windows, Linux, and macOS

### Download

[![GitHub Release](https://img.shields.io/github/v/release/jurakovic/guidreplace)](https://github.com/jurakovic/guidreplace/releases/latest)

### Usage

```text
$ guidrep -h
Description:
  GUID Replace tool

Usage:
  guidrep [<inputFile>] [options]

Arguments:
  <inputFile>  The input file to process. If not specified, reads from standard input.

Options:
  -i, --in-place         Edit the input file in place
  -o, --output <output>  The output file to write the result to
  -q, --quiet            Do not output messages to standard output
  --version              Show version information
  -?, -h, --help         Show help and usage information
```

Replace GUIDs in a file and save to a new file

```
guidrep input.txt -o output.txt
```

Replace GUIDs in a file and save to a new automatically created file

```
guidrep input.txt
```

Edit a file in place

```
guidrep -i input.txt
```

Using standard input and output

```
cat input.txt | guidrep -q > output.txt
```

Using standard input and save to a new file

```
cat input.txt | guidrep -o output.txt
```

### Example

```
cat <<EOF | ./guidrep.exe -q
{
  "id": "011df4e1-1962-44f0-a21a-06674f4c2374",
  "data": [
    {
      "id": "d49987ee-595c-4fb3-8cea-8da7f281fdbf",
      "nodeId": "011df4e1-1962-44f0-a21a-06674f4c2374",
      "state": [
        {
          "id": "9f5ad759-f9b5-4f2a-a8ec-8979fe05656f",
          "typeId": "d49987ee-595c-4fb3-8cea-8da7f281fdbf"
        }
      ]
    }
  ]
}
EOF
```

Output:

```
{
  "id": "76393731-f856-4bc9-9dca-d7aea4f4fb2a",
  "data": [
    {
      "id": "e88465f8-98f1-417b-894c-ac4549083ffe",
      "nodeId": "76393731-f856-4bc9-9dca-d7aea4f4fb2a",
      "state": [
        {
          "id": "256422d0-a34e-4a8f-8411-e7a5edc6d444",
          "typeId": "e88465f8-98f1-417b-894c-ac4549083ffe"
        }
      ]
    }
  ]
}
```

### Build from source

1. Make sure you have installed [.NET SDK](https://dotnet.microsoft.com/en-us/download) and other [prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=linux-ubuntu%2Cnet8#prerequisites) for Native AOT deployment

2. Clone the repository

	```bash
	git clone https://github.com/jurakovic/guidreplace.git
	cd guidreplace
	```

3. Build (publish) with `dotnet`

	```bash
	cd src
	dotnet publish -c Release -r linux-x64 --self-contained
	bin/Release/net9.0/linux-x64/publish/guidrep -h
	# mv guidrep to desired path
	```

### Credits

Inspired by [this](https://stackoverflow.com/questions/2201740/replacing-all-guids-in-a-file-with-new-guids-from-the-command-line) Stack Overflow question and answers.
