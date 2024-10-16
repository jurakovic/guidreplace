
# Guid Replace

`guidrep` is a simple command-line tool for replacing guids in text files or standard input. It reads the input, replaces all guids with new randomly generated ones, and ensures that any repeated guids in the original are replaced with the same new guid.

This tool is especially useful for anonymizing data exports, resetting identifiers in configuration files, or preparing JSON data for re-import.


### Features

- **File Processing**: Replace guids in a specified file, with options for in-place editing or outputting to a new file.
- **Standard Input/Output Support**: Works with piped input and outputs to standard output.
- **Flexible Output**: Save the results to a specified output file, or automatically generate a new filename based on the original.
- **Cross-Platform**: Runs on Windows, Linux, and macOS.

### Usage

```text
$ guidrep -h
Description:
  Guid Replace tool
  For more information, visit https://github.com/jurakovic/guidreplace

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

#### Examples

todo

### Download

[![GitHub Release](https://img.shields.io/github/v/release/jurakovic/guidreplace)](https://github.com/jurakovic/guidreplace/releases/latest)

### Build from source

1. Make sure you have installed [Git](https://git-scm.com) and [.NET SDK](https://dotnet.microsoft.com/en-us/download)

2. Clone the repository

    ```bash
    git clone https://github.com/jurakovic/guidreplace.git
    cd guidreplace
    ```

3. Build (publish) with `dotnet`

    ```bash
    cd src
    dotnet publish -c Release --self-contained
    bin/Release/net8.0/linux-x64/publish/guidrep -h
    # mv to desired path
    ```
