# TiFilesNet

A dependency-free .NET 10 library and console application for the TI-99/4A TIFILES transfer format.

## Projects

- `TiFiles`: reads and writes 128-byte TIFILES headers and sector-padded data.
- `TiFiles.Cli`: displays metadata, extracts logical content, and creates TIFILES containers.
- `TiFiles.Tests`: round-trip and byte-order tests.

## Build

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

## CLI usage

```bash
dotnet run --project TiFiles.Cli -- info SAMPLE.TFI
dotnet run --project TiFiles.Cli -- extract SAMPLE.TFI payload.bin
dotnet run --project TiFiles.Cli -- create payload.bin SAMPLE.TFI --name SAMPLE --type program
```

For record-oriented files:

```bash
dotnet run --project TiFiles.Cli -- create records.bin RECORDS.TFI \
  --name RECORDS \
  --type display-fixed \
  --record-length 80 \
  --records-per-sector 3 \
  --level3-records 120
```

## Library usage

```csharp
using TiFiles;

TiFile file = TiFileReader.Read("SAMPLE.TFI");
Console.WriteLine(file.Header.FileName);
Console.WriteLine(file.Header.DataType);
Console.WriteLine(file.Data.Length);

file.Header.Updated = DateTime.Now;
TiFileWriter.Write("SAMPLE-COPY.TFI", file);
```

To create a file:

```csharp
var header = new TiFileHeader
{
    FileName = "HELLO",
    Flags = TiFileFlags.Program,
    Created = DateTime.Now,
    Updated = DateTime.Now
};

var file = new TiFile(header, File.ReadAllBytes("hello.bin"));
TiFileWriter.Write("HELLO.TFI", file);
```

## Implementation notes

- Header length is 128 bytes; sectors are 256 bytes.
- The signature starts with `07 54 49 46 49 4C 45 53`; `08` is accepted for the multi-file variant.
- Sector count and timestamp words are treated as big-endian TI words.
- The Level-3 record count is explicitly stored little-endian.
- Logical content length is calculated from sector count and EOF offset.
- Writing recalculates sector count and EOF offset and zero-pads the final sector.
- Unknown/reserved header bytes from parsed files are retained during rewriting.
- Timestamp seconds have two-second resolution; odd seconds are rounded down.
- The data buffer represents the logical bytes indicated by the header. For variable-record files, this includes whatever EOF marker bytes are present within that logical range; the library does not parse individual records.
