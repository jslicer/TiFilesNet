// <copyright file="TiFileWriter.cs" company="Always Elucidated Solution Pioneers, LLC">
// Copyright (c) Always Elucidated Solution Pioneers, LLC. All rights reserved.
// </copyright>

namespace TiFiles;

using System.Buffers.Binary;
using System.Text;

/// <summary>
/// Writes files in the TIFILES format.
/// </summary>
public static class TiFileWriter
{
    private const int DefaultFileBufferSize = 4096;

    /// <summary>
    /// Writes a TIFILES-format file to the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="file">The TIFILES-format file.</param>
    //// ReSharper disable once UnusedMember.Global
    public static void Write(string? path, ITiFile file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using FileStream stream = File.Create(path);
        Write(stream, file);
    }

    /// <summary>
    /// Writes a TIFILES-format file to the specified stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="file">The TIFILES-format file.</param>
    //// ReSharper disable once MethodTooLong
    public static void Write(Stream? stream, ITiFile? file)
    {
#pragma warning disable RCS1256 // Invalid argument null check
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(file);
#pragma warning restore RCS1256 // Invalid argument null check
        if (!stream.CanWrite)
        {
            throw new ArgumentException("The stream must be writable.", nameof(stream));
        }

        int dataLength = file.Data.Length;

        // ReSharper disable once ComplexConditionExpression
        int sectors = dataLength == 0
            ? 0
            : checked((dataLength + TiFileHeader.SectorLength - 1) / TiFileHeader.SectorLength);

        if (sectors > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(file), "TIFILES supports at most 65,535 sectors.");
        }

        //// ReSharper disable ComplexConditionExpression
        byte eofOffset = dataLength == 0 || dataLength % TiFileHeader.SectorLength == 0
            ? (byte)0
            : (byte)(dataLength % TiFileHeader.SectorLength);
        //// ReSharper restore ComplexConditionExpression

        file.Header.TotalSectors = (ushort)sectors;
        file.Header.EndOfFileOffset = eofOffset;

        byte[] header = [.. file.OriginalHeader.Span];

        header[TiFileHeader.IdentifierOffset] =
            file.Header.Identifier is TiFileHeader.StandardIdentifier or TiFileHeader.ExtendedIdentifier
                ? file.Header.Identifier
                : TiFileHeader.StandardIdentifier;
        TiFilesSignature.Signature.CopyTo(header.AsSpan(TiFileHeader.SignatureOffset, TiFileHeader.SignatureLength));
        BinaryPrimitives.WriteUInt16BigEndian(
            header.AsSpan(TiFileHeader.TotalSectorsOffset, TiFileHeader.UshortFieldLength), file.Header.TotalSectors);
        header[TiFileHeader.FlagsOffset] = (byte)file.Header.Flags;
        header[TiFileHeader.RecordsPerSectorOffset] = file.Header.RecordsPerSector;
        header[TiFileHeader.EndOfFileOffsetPosition] = file.Header.EndOfFileOffset;
        header[TiFileHeader.RecordLengthOffset] = file.Header.RecordLength;
        BinaryPrimitives.WriteUInt16LittleEndian(
            header.AsSpan(TiFileHeader.Level3RecordCountOffset, TiFileHeader.UshortFieldLength),
            file.Header.Level3RecordCount);

        Span<byte> nameField = header.AsSpan(TiFileHeader.FileNameOffset, TiFileHeader.FileNameLength);

        nameField.Fill(TiFileHeader.FileNamePaddingByte);

        string normalizedName = NormalizeFileName(file.Header.FileName);

        _ = Encoding.ASCII.GetBytes(normalizedName, nameField);
        header[TiFileHeader.MxtOffset] = file.Header.Mxt;
        header[TiFileHeader.Reserved1BOffset] = file.Header.Reserved1B;
        BinaryPrimitives.WriteUInt16BigEndian(
            header.AsSpan(TiFileHeader.ExtendedHeaderOffset, TiFileHeader.UshortFieldLength),
            file.Header.ExtendedHeader);
        WriteTimestamp(
            header.AsSpan(TiFileHeader.CreatedTimestampOffset, TiFileHeader.TimestampLength),
            file.Header.Created);
        WriteTimestamp(
            header.AsSpan(TiFileHeader.UpdatedTimestampOffset, TiFileHeader.TimestampLength),
            file.Header.Updated);
        stream.Write(header);
        stream.Write(file.Data.Span);

        int padding = (sectors * TiFileHeader.SectorLength) - dataLength;

        if (padding <= 0)
        {
            return;
        }

        Span<byte> zeroes = stackalloc byte[TiFileHeader.SectorLength];

        stream.Write(zeroes[..padding]);
    }

    /// <summary>
    /// Writes a TIFILES-format file asynchronously to the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="file">The TIFILES-format file.</param>
    /// <param name="cancellationToken">The optional cancellation token.</param>
    /// <returns>An asynchronous <see cref="Task" />.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static async Task WriteAsync(string path, ITiFile file, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using FileStream stream = new(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: DefaultFileBufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        await WriteAsync(stream, file, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a TIFILES-format file asynchronously to the specified stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="file">The TIFILES-format file.</param>
    /// <param name="cancellationToken">The optional cancellation token.</param>
    /// <returns>An asynchronous <see cref="Task" />.</returns>
    //// ReSharper disable once MethodTooLong
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static async Task WriteAsync(Stream? stream, ITiFile? file, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
#pragma warning disable RCS1256 // Invalid argument null check
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(file);
#pragma warning restore RCS1256 // Invalid argument null check
        if (!stream.CanWrite)
        {
            throw new ArgumentException("The stream must be writable.", nameof(stream));
        }

        int dataLength = file.Data.Length;

        // ReSharper disable once ComplexConditionExpression
        int sectors = dataLength == 0
            ? 0
            : checked((dataLength + TiFileHeader.SectorLength - 1) / TiFileHeader.SectorLength);

        if (sectors > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(file), "TIFILES supports at most 65,535 sectors.");
        }

        //// ReSharper disable ComplexConditionExpression
        byte eofOffset = dataLength == 0 || dataLength % TiFileHeader.SectorLength == 0
            ? (byte)0
            : (byte)(dataLength % TiFileHeader.SectorLength);
        //// ReSharper restore ComplexConditionExpression

        file.Header.TotalSectors = (ushort)sectors;
        file.Header.EndOfFileOffset = eofOffset;

        byte[] header = [.. file.OriginalHeader.Span];

        header[TiFileHeader.IdentifierOffset] = file.Header.Identifier is TiFileHeader.StandardIdentifier or TiFileHeader.ExtendedIdentifier
            ? file.Header.Identifier
            : TiFileHeader.StandardIdentifier;
        TiFilesSignature.Signature.CopyTo(header.AsSpan(TiFileHeader.SignatureOffset, TiFileHeader.SignatureLength));
        BinaryPrimitives.WriteUInt16BigEndian(
            header.AsSpan(TiFileHeader.TotalSectorsOffset, TiFileHeader.UshortFieldLength),
            file.Header.TotalSectors);
        header[TiFileHeader.FlagsOffset] = (byte)file.Header.Flags;
        header[TiFileHeader.RecordsPerSectorOffset] = file.Header.RecordsPerSector;
        header[TiFileHeader.EndOfFileOffsetPosition] = file.Header.EndOfFileOffset;
        header[TiFileHeader.RecordLengthOffset] = file.Header.RecordLength;
        BinaryPrimitives.WriteUInt16LittleEndian(
            header.AsSpan(TiFileHeader.Level3RecordCountOffset, TiFileHeader.UshortFieldLength),
            file.Header.Level3RecordCount);

        Span<byte> nameField = header.AsSpan(TiFileHeader.FileNameOffset, TiFileHeader.FileNameLength);

        nameField.Fill(TiFileHeader.FileNamePaddingByte);

        string normalizedName = NormalizeFileName(file.Header.FileName);

        _ = Encoding.ASCII.GetBytes(normalizedName, nameField);
        header[TiFileHeader.MxtOffset] = file.Header.Mxt;
        header[TiFileHeader.Reserved1BOffset] = file.Header.Reserved1B;
        BinaryPrimitives.WriteUInt16BigEndian(
            header.AsSpan(TiFileHeader.ExtendedHeaderOffset, TiFileHeader.UshortFieldLength),
            file.Header.ExtendedHeader);
        WriteTimestamp(
            header.AsSpan(TiFileHeader.CreatedTimestampOffset, TiFileHeader.TimestampLength),
            file.Header.Created);
        WriteTimestamp(
            header.AsSpan(TiFileHeader.UpdatedTimestampOffset, TiFileHeader.TimestampLength),
            file.Header.Updated);
        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(file.Data, cancellationToken).ConfigureAwait(false);

        int padding = (sectors * TiFileHeader.SectorLength) - dataLength;

        if (padding <= 0)
        {
            return;
        }

        byte[] zeroes = new byte[TiFileHeader.SectorLength];

        await stream.WriteAsync(zeroes.AsMemory(0, padding), cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeFileName(string? value)
    {
        string name = (value ?? string.Empty).Trim();

#pragma warning disable S3358 // Ternary operators should not be nested
        // ReSharper disable once ComplexConditionExpression
        return name.Length > TiFileHeader.FileNameLength
            ? throw new ArgumentException("A TI filename cannot exceed 10 characters.", nameof(value))
            : name.Any(c => c > TiFileHeader.MaximumAsciiCodePoint || char.IsControl(c))
                ? throw new ArgumentException(
                    "A TI filename must contain printable ASCII characters only.",
                    nameof(value))
                : name;
#pragma warning restore S3358 // Ternary operators should not be nested
    }

    private static void WriteTimestamp(Span<byte> destination, DateTime? value)
    {
        (ushort timeWord, ushort dateWord) = TiTimestamp.Encode(value);

        BinaryPrimitives.WriteUInt16BigEndian(destination[..TiFileHeader.UshortFieldLength], timeWord);
        BinaryPrimitives.WriteUInt16BigEndian(destination[TiFileHeader.UshortFieldLength..], dateWord);
    }
}