// <copyright file="TiFileReader.cs" company="Always Elucidated Solution Pioneers, LLC">
// Copyright (c) Always Elucidated Solution Pioneers, LLC. All rights reserved.
// </copyright>

namespace TiFiles;

using System.Buffers.Binary;
using System.Text;

/// <summary>
/// Reads files in the TIFILES format.
/// </summary>
public static class TiFileReader
{
    private const int DefaultFileBufferSize = 4096;

    /// <summary>
    /// Reads a TIFILES-format file from the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The TIFILES-format file.</returns>
    //// ReSharper disable once UnusedMember.Global
    public static ITiFile Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using FileStream stream = File.OpenRead(path);
        return Read(stream);
    }

    /// <summary>
    /// Reads a TIFILES-format file from the specified stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>The TIFILES-format file.</returns>
    public static ITiFile Read(Stream stream)
    {
        ValidateReadableStream(stream);

        byte[] headerBytes = new byte[TiFileHeader.HeaderLength];

        ReadExactly(stream, headerBytes);

        ParsedHeader parsedHeader = ParseHeader(headerBytes);

        ValidateAvailableData(stream, parsedHeader.LogicalLength);

        byte[] data = AllocateDataBuffer(parsedHeader.LogicalLength);

        ReadExactly(stream, data);
        return CreateTiFile(headerBytes, data, parsedHeader);
    }

    /// <summary>
    /// Asynchronously reads a TIFILES-format file from the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="cancellationToken">The optional cancellation token.</param>
    /// <returns>An asynchronous <see cref="Task{T}" /> containing the TIFILES-format file.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static async Task<ITiFile> ReadAsync(string path, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: DefaultFileBufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        return await ReadAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously reads a TIFILES-format file from the specified stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">The optional cancellation token.</param>
    /// <returns>An asynchronous <see cref="Task{T}" /> containing the TIFILES-format file.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static async Task<ITiFile> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        ValidateReadableStream(stream);

        byte[] headerBytes = new byte[TiFileHeader.HeaderLength];

        await ReadExactlyAsync(stream, headerBytes, cancellationToken).ConfigureAwait(false);

        ParsedHeader parsedHeader = ParseHeader(headerBytes);

        ValidateAvailableData(stream, parsedHeader.LogicalLength);

        byte[] data = AllocateDataBuffer(parsedHeader.LogicalLength);

        await ReadExactlyAsync(stream, data, cancellationToken).ConfigureAwait(false);
        return CreateTiFile(headerBytes, data, parsedHeader);
    }

    /// <summary>
    /// Determines whether the specified stream is a TIFILES-format file.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>
    ///   <c>true</c> if the specified stream is a TIFILES-format file; otherwise, <c>false</c>.
    /// </returns>
    // ReSharper disable once UnusedMember.Global
    public static bool IsTiFile(Stream? stream)
    {
#pragma warning disable RCS1256 // Invalid argument null check
        ArgumentNullException.ThrowIfNull(stream);
#pragma warning restore RCS1256 // Invalid argument null check
        if (!stream.CanRead)
        {
            return false;
        }

        long position = stream.CanSeek ? stream.Position : 0L;
        Span<byte> bytes = stackalloc byte[TiFileHeader.PreambleLength];

        try
        {
            int read = ReadUpTo(stream, bytes);

            return read == bytes.Length
                && bytes[TiFileHeader.IdentifierOffset] is TiFileHeader.StandardIdentifier or TiFileHeader.ExtendedIdentifier
                && bytes[TiFileHeader.SignatureOffset..].SequenceEqual(TiFilesSignature.Signature);
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = position;
            }
        }
    }

    /// <summary>
    /// Asynchronously determines whether the specified stream is a TIFILES-format file.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">The optional cancellation token.</param>
    /// <returns>
    ///   <c>true</c> if the specified stream is a TIFILES-format file; otherwise, <c>false</c>.
    /// </returns>
    public static async Task<bool> IsTiFileAsync(Stream? stream, CancellationToken cancellationToken = default)
    {
#pragma warning disable RCS1256 // Invalid argument null check
        ArgumentNullException.ThrowIfNull(stream);
#pragma warning restore RCS1256 // Invalid argument null check
        if (!stream.CanRead)
        {
            return false;
        }

        long position = stream.CanSeek ? stream.Position : 0L;
        byte[] bytes = new byte[TiFileHeader.PreambleLength];

        try
        {
            int read = await ReadUpToAsync(stream, bytes, cancellationToken).ConfigureAwait(false);

            return read == bytes.Length
                && bytes[TiFileHeader.IdentifierOffset] is TiFileHeader.StandardIdentifier or TiFileHeader.ExtendedIdentifier
                && bytes.AsSpan(TiFileHeader.SignatureOffset).SequenceEqual(TiFilesSignature.Signature);
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = position;
            }
        }
    }

    private static void ValidateReadableStream(Stream? stream)
    {
#pragma warning disable RCS1256 // Invalid argument null check
        ArgumentNullException.ThrowIfNull(stream);
#pragma warning restore RCS1256 // Invalid argument null check
        if (!stream.CanRead)
        {
            throw new ArgumentException("The stream must be readable.", nameof(stream));
        }
    }

    private static ParsedHeader ParseHeader(byte[] headerBytes)
    {
        byte identifierLength = headerBytes[TiFileHeader.IdentifierOffset];

        if (identifierLength is not TiFileHeader.StandardIdentifier and not TiFileHeader.ExtendedIdentifier)
        {
            throw new TiFileFormatException(
                $"Invalid TIFILES identifier length 0x{identifierLength:X2}; expected 0x07 or 0x08.");
        }

        if (!headerBytes
            .AsSpan(TiFileHeader.SignatureOffset, TiFileHeader.SignatureLength)
            .SequenceEqual(TiFilesSignature.Signature))
        {
            throw new TiFileFormatException("The file does not contain the TIFILES signature.");
        }

        ushort sectors = BinaryPrimitives.ReadUInt16BigEndian(
            headerBytes.AsSpan(TiFileHeader.TotalSectorsOffset, TiFileHeader.UshortFieldLength));
        byte eofOffset = headerBytes[TiFileHeader.EndOfFileOffsetPosition];
        long logicalLength = CalculateLogicalLength(sectors, eofOffset);

        return new ParsedHeader(identifierLength, sectors, eofOffset, logicalLength);
    }

#pragma warning disable S3358 // Ternary operators should not be nested
    private static long CalculateLogicalLength(ushort sectors, byte eofOffset) =>
        sectors == 0
            ? 0L
            : eofOffset == 0
                ? (long)sectors * TiFileHeader.SectorLength
                : (((long)sectors - 1) * TiFileHeader.SectorLength) + eofOffset;
#pragma warning restore S3358 // Ternary operators should not be nested

    private static void ValidateAvailableData(Stream stream, long logicalLength)
    {
        if (!stream.CanSeek)
        {
            return;
        }

        long available = stream.Length - stream.Position;

        if (available < logicalLength)
        {
            throw new TiFileFormatException(
                $"Header declares {logicalLength} data bytes, but only {available} are available.");
        }
    }

    private static byte[] AllocateDataBuffer(long logicalLength) =>
        logicalLength > int.MaxValue
            ? throw new TiFileFormatException("The file is too large to load into memory.")
            : new byte[(int)logicalLength];

#pragma warning disable CA1859 // Use concrete types when possible for improved performance
    private static ITiFile CreateTiFile(byte[] headerBytes, byte[] data, ParsedHeader parsedHeader)
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
    {
        string fileName = Encoding
            .ASCII
            .GetString(headerBytes, TiFileHeader.FileNameOffset, TiFileHeader.FileNameLength)
            .TrimEnd(' ', '\0');
        ushort creationTime = BinaryPrimitives.ReadUInt16BigEndian(
            headerBytes.AsSpan(TiFileHeader.CreatedTimestampOffset, TiFileHeader.UshortFieldLength));
        ushort creationDate = BinaryPrimitives.ReadUInt16BigEndian(headerBytes.AsSpan(
            TiFileHeader.CreatedTimestampOffset + TiFileHeader.UshortFieldLength,
            TiFileHeader.UshortFieldLength));
        ushort updateTime = BinaryPrimitives.ReadUInt16BigEndian(
            headerBytes.AsSpan(TiFileHeader.UpdatedTimestampOffset, TiFileHeader.UshortFieldLength));
        ushort updateDate = BinaryPrimitives.ReadUInt16BigEndian(headerBytes.AsSpan(
            TiFileHeader.UpdatedTimestampOffset + TiFileHeader.UshortFieldLength,
            TiFileHeader.UshortFieldLength));
        ITiFileHeader header = new TiFileHeader
        {
            Identifier = parsedHeader.IdentifierLength,
            TotalSectors = parsedHeader.TotalSectors,
            Flags = (TiFileFlags)headerBytes[TiFileHeader.FlagsOffset],
            RecordsPerSector = headerBytes[TiFileHeader.RecordsPerSectorOffset],
            EndOfFileOffset = parsedHeader.EndOfFileOffset,
            RecordLength = headerBytes[TiFileHeader.RecordLengthOffset],
            Level3RecordCount = BinaryPrimitives.ReadUInt16LittleEndian(
                headerBytes.AsSpan(TiFileHeader.Level3RecordCountOffset, TiFileHeader.UshortFieldLength)),
            FileName = fileName,
            Mxt = headerBytes[TiFileHeader.MxtOffset],
            Reserved1B = headerBytes[TiFileHeader.Reserved1BOffset],
            ExtendedHeader = BinaryPrimitives.ReadUInt16BigEndian(
                headerBytes.AsSpan(TiFileHeader.ExtendedHeaderOffset, TiFileHeader.UshortFieldLength)),
            Created = TiTimestamp.Decode(creationTime, creationDate),
            Updated = TiTimestamp.Decode(updateTime, updateDate),
        };

        return new TiFile(header, data, headerBytes);
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        int total = 0;

        while (total < buffer.Length)
        {
            int read = stream.Read(buffer[total..]);

            if (read == 0)
            {
                throw new EndOfStreamException($"Unexpected end of stream after {total} of {buffer.Length} bytes.");
            }

            total += read;
        }
    }

    private static async ValueTask ReadExactlyAsync(
        Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        int total = 0;

        while (total < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[total..], cancellationToken).ConfigureAwait(false);

            if (read == 0)
            {
                throw new EndOfStreamException($"Unexpected end of stream after {total} of {buffer.Length} bytes.");
            }

            total += read;
        }
    }

    private static int ReadUpTo(Stream stream, Span<byte> buffer)
    {
        int total = 0;

        while (total < buffer.Length)
        {
            int read = stream.Read(buffer[total..]);

            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }

    private static async ValueTask<int> ReadUpToAsync(
        Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        int total = 0;

        while (total < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[total..], cancellationToken).ConfigureAwait(false);

            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }

    private readonly record struct ParsedHeader(
        byte IdentifierLength,
        ushort TotalSectors,
        byte EndOfFileOffset,
        long LogicalLength);
}