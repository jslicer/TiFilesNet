// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace TiFiles.Cli;

using System.Globalization;
using TiFiles;

/// <summary>
/// Holds the entry point of the application.
/// </summary>
internal static class Program
{
    private const int InfoArgumentCount = 2;

    private const int ExtractArgumentCount = 3;

    private const int CreateMinimumArgumentCount = 4;

    private const int OptionPrefixLength = 2;

    private const int UsageErrorExitCode = 2;

    /// <summary>
    /// The entry point of the application.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>Status code of the application.</returns>
    private static async Task<int> Main(string[] args)
    {
        try
        {
            // ReSharper disable once ComplexConditionExpression
            if (args.Length != 0 && !IsHelp(args[0]))
            {
                return args[0].ToUpperInvariant() switch
                {
                    "INFO" => await InfoAsync(args).ConfigureAwait(false),
                    "EXTRACT" => await ExtractAsync(args).ConfigureAwait(false),
                    "CREATE" => await CreateAsync(args).ConfigureAwait(false),
                    _ => await UnknownCommandAsync(args[0]).ConfigureAwait(false),
                };
            }

            await PrintUsageAsync().ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or TiFileFormatException)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
    }

    // ReSharper disable once MethodTooLong
    private static async Task<int> InfoAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args.Length != InfoArgumentCount)
        {
            return await UsageErrorAsync("info requires a TIFILES path.").ConfigureAwait(false);
        }

        string path = args[1];
        ITiFile file = await TiFileReader.ReadAsync(path, cancellationToken).ConfigureAwait(false);
        ITiFileHeader h = file.Header;

        Console.WriteLine($"Path:                 {Path.GetFullPath(path)}");
        Console.WriteLine($"TI filename:          {Display(h.FileName)}");
        Console.WriteLine($"Identifier length:    0x{h.Identifier:X2}");
        Console.WriteLine($"Sectors:              {h.TotalSectors:N0}");
        Console.WriteLine($"Logical data bytes:   {file.Data.Length:N0}");
        Console.WriteLine($"EOF offset:           {h.EndOfFileOffset}");
        Console.WriteLine($"Flags:                0x{(byte)h.Flags:X2} ({DescribeFlags(h)})");
        Console.WriteLine($"Organization:         {h.Organization}");
        Console.WriteLine($"Data type:            {h.DataType}");
        Console.WriteLine($"Records per sector:   {h.RecordsPerSector}");
        Console.WriteLine($"Record length:        {h.RecordLength}");
        Console.WriteLine($"Level-3 record count: {h.Level3RecordCount:N0}");
        Console.WriteLine($"Protected:            {h.IsProtected}");
        Console.WriteLine($"Modified:             {h.IsModified}");
        Console.WriteLine($"MXT:                  0x{h.Mxt:X2}");
        Console.WriteLine($"Extended header:      0x{h.ExtendedHeader:X4}");
        Console.WriteLine($"Created:              {FormatDate(h.Created)}");
        Console.WriteLine($"Updated:              {FormatDate(h.Updated)}");
        return 0;
    }

    private static async Task<int> ExtractAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args.Length != ExtractArgumentCount)
        {
            return await UsageErrorAsync("extract requires an input path and output path.").ConfigureAwait(false);
        }

        ITiFile file = await TiFileReader.ReadAsync(args[1], cancellationToken).ConfigureAwait(false);

        await File.WriteAllBytesAsync(args[2], file.Data, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"Extracted {file.Data.Length:N0} bytes to {Path.GetFullPath(args[2])}");
        return 0;
    }

    // ReSharper disable once MethodTooLong
    private static async Task<int> CreateAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args.Length < CreateMinimumArgumentCount)
        {
            return await UsageErrorAsync(
                "create requires an input path, output path, and --name.").ConfigureAwait(false);
        }

        string input = args[1];
        string output = args[2];
        Dictionary<string, string> options = ParseOptions(args[3..]);

        if (!options.TryGetValue("name", out string? name) || string.IsNullOrWhiteSpace(name))
        {
            return await UsageErrorAsync("create requires --name <TI-NAME>.").ConfigureAwait(false);
        }

        byte[] data = await File.ReadAllBytesAsync(input, cancellationToken).ConfigureAwait(false);
        TiFileFlags flags = ParseFlags(options.GetValueOrDefault("type", "program"));
        byte recordLength = ParseByte(options.GetValueOrDefault("record-length", "0"), "record-length");
        byte recordsPerSector = ParseByte(options.GetValueOrDefault("records-per-sector", "0"), "records-per-sector");
        ushort level3 = ParseUShort(options.GetValueOrDefault("level3-records", "0"), "level3-records");
        DateTime now = DateTime.Now;

        ITiFileHeader header = new TiFileHeader
        {
            FileName = name,
            Flags = flags,
            RecordLength = recordLength,
            RecordsPerSector = recordsPerSector,
            Level3RecordCount = level3,
            Created = now,
            Updated = now,
        };

        await TiFileWriter.WriteAsync(output, new TiFile(header, data), cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"Created {Path.GetFullPath(output)} from {data.Length:N0} bytes.");
        return 0;
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        Dictionary<string, string> result = [with(StringComparer.OrdinalIgnoreCase)];

        for (int i = 0; i < args.Length; i++)
        {
            string token = args[i];

            // ReSharper disable once ComplexConditionExpression
            if (!token.StartsWith("--", StringComparison.Ordinal) || token.Length == OptionPrefixLength)
            {
                throw new ArgumentException($"Invalid option '{token}'.");
            }

#pragma warning disable S127 // "for" loop stop conditions should be invariant
            if (++i >= args.Length)
            {
                throw new ArgumentException($"Missing value for option '{token}'.");
            }
#pragma warning restore S127 // "for" loop stop conditions should be invariant

            result[token[OptionPrefixLength..]] = args[i];
        }

        return result;
    }

    private static TiFileFlags ParseFlags(string value) =>
        value.ToUpperInvariant() switch
        {
            "PROGRAM" => TiFileFlags.Program,
            "DISPLAY-FIXED" => TiFileFlags.None,
            "DISPLAY-VARIABLE" => TiFileFlags.Variable,
            "INTERNAL-FIXED" => TiFileFlags.Internal,
            "INTERNAL-VARIABLE" => TiFileFlags.Internal | TiFileFlags.Variable,
            _ => throw new ArgumentException($"Unknown type '{value}'."),
        };

    private static byte ParseByte(string value, string option) =>
        byte.TryParse(value, CultureInfo.InvariantCulture, out byte parsed)
            ? parsed
            : throw new ArgumentException($"--{option} must be from 0 through 255.");

    private static ushort ParseUShort(string value, string option) =>
        ushort.TryParse(value, CultureInfo.InvariantCulture, out ushort parsed)
            ? parsed
            : throw new ArgumentException($"--{option} must be from 0 through 65535.");

    private static string DescribeFlags(ITiFileHeader h)
    {
        ICollection<string> values = [h.DataType, h.Organization];

        if (h.IsProtected)
        {
            values.Add("Protected");
        }

        if (h.IsModified)
        {
            values.Add("Modified");
        }

        if (h.Flags.HasFlag(TiFileFlags.Emulate))
        {
            values.Add("Emulate");
        }

        return string.Join(", ", values);
    }

    private static string FormatDate(DateTime? value) =>
        value?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "not present/invalid";

    private static string Display(string value) => string.IsNullOrEmpty(value) ? "(not present)" : value;

    private static bool IsHelp(string value) => value is "-h" or "--help" or "help";

    private static async Task<int> UnknownCommandAsync(string command) =>
        await UsageErrorAsync($"Unknown command '{command}'.").ConfigureAwait(false);

    private static async Task<int> UsageErrorAsync(string message)
    {
        await Console.Error.WriteLineAsync($"Error: {message}").ConfigureAwait(false);
        await Console.Error.WriteLineAsync().ConfigureAwait(false);
        await PrintUsageAsync(Console.Error).ConfigureAwait(false);
        return UsageErrorExitCode;
    }

    private static async Task PrintUsageAsync(TextWriter? writer = null)
    {
        writer ??= Console.Out;
        await writer.WriteLineAsync("TIFILES command-line utility").ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);
        await writer.WriteLineAsync("Usage:").ConfigureAwait(false);
        await writer.WriteLineAsync("  tifiles info <file>").ConfigureAwait(false);
        await writer.WriteLineAsync("  tifiles extract <file> <output>").ConfigureAwait(false);
        await writer.WriteLineAsync(
            "  tifiles create <input> <output> --name <TI-NAME> [options]").ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);
        await writer.WriteLineAsync("Create options:").ConfigureAwait(false);
        await writer
            .WriteLineAsync("  --type <program|display-fixed|display-variable|internal-fixed|internal-variable>")
            .ConfigureAwait(false);
        await writer.WriteLineAsync("  --record-length <0-255>").ConfigureAwait(false);
        await writer.WriteLineAsync("  --records-per-sector <0-255>").ConfigureAwait(false);
        await writer.WriteLineAsync("  --level3-records <0-65535>").ConfigureAwait(false);
    }
}