// <copyright file="TiFileTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

// Ignore Spelling: Endian
namespace TiFiles.Tests;

using TiFiles;

using Xunit;

/// <summary>
/// Tests for the <see cref="TiFile" /> class.
/// </summary>
public sealed class TiFileTests
{
    /// <summary>
    /// Tests that a program file can round-trip and pad to the sector size.
    /// </summary>
    [Fact]
    public void ProgramFileRoundTripsAndPadsToSector()
    {
        byte[] data = [.. Enumerable.Range(0, 300).Select(i => (byte)i)];
        ITiFile original = new TiFile(
            new TiFileHeader
            {
                FileName = "HELLO",
                Flags = TiFileFlags.Program | TiFileFlags.Protected,
                //// ReSharper disable RedundantArgument
                Created = new DateTime(2026, 7, 27, 8, 15, 13, DateTimeKind.Unspecified),
                Updated = new DateTime(2026, 7, 27, 9, 16, 18, DateTimeKind.Unspecified),
                //// ReSharper restore RedundantArgument
            },
            data);

        using MemoryStream stream = new();
        TiFileWriter.Write(stream, original);
        Assert.Equal(128 + 512, stream.Length);
        stream.Position = 0;

        ITiFile parsed = TiFileReader.Read(stream);

        Assert.Equal("HELLO", parsed.Header.FileName);
        Assert.Equal((ushort)2, parsed.Header.TotalSectors);
        Assert.Equal((byte)44, parsed.Header.EndOfFileOffset);
        Assert.Equal(data, parsed.Data);
        Assert.True(parsed.Header.IsProgram);
        Assert.True(parsed.Header.IsProtected);

        // ReSharper disable once RedundantArgument
        Assert.Equal(new DateTime(2026, 7, 27, 8, 15, 12, DateTimeKind.Unspecified), parsed.Header.Created);
    }

    /// <summary>
    /// Tests that a program file can round-trip and pad to the sector size asynchronously.
    /// </summary>
    /// <returns>An asynchronous <see cref="Task" />.</returns>
    [Fact]
    public async Task ProgramFileRoundTripsAndPadsToSectorAsync()
    {
        byte[] data = [.. Enumerable.Range(0, 300).Select(i => (byte)i)];
        ITiFile original = new TiFile(
            new TiFileHeader
            {
                FileName = "HELLO",
                Flags = TiFileFlags.Program | TiFileFlags.Protected,
                //// ReSharper disable RedundantArgument
                Created = new DateTime(2026, 7, 27, 8, 15, 13, DateTimeKind.Unspecified),
                Updated = new DateTime(2026, 7, 27, 9, 16, 18, DateTimeKind.Unspecified),
                //// ReSharper restore RedundantArgument
            },
            data);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using MemoryStream stream = new();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        await TiFileWriter.WriteAsync(stream, original);
        Assert.Equal(128 + 512, stream.Length);
        stream.Position = 0;

        ITiFile parsed = await TiFileReader.ReadAsync(stream);

        Assert.Equal("HELLO", parsed.Header.FileName);
        Assert.Equal((ushort)2, parsed.Header.TotalSectors);
        Assert.Equal((byte)44, parsed.Header.EndOfFileOffset);
        Assert.Equal(data, parsed.Data);
        Assert.True(parsed.Header.IsProgram);
        Assert.True(parsed.Header.IsProtected);

        // ReSharper disable once RedundantArgument
        Assert.Equal(new DateTime(2026, 7, 27, 8, 15, 12, DateTimeKind.Unspecified), parsed.Header.Created);
    }

    /// <summary>
    /// Tests that the Level-3 Record Count is stored in little-endian format in the file header.
    /// </summary>
    [Fact]
    public void Level3RecordCountIsLittleEndian()
    {
        ITiFile file = new TiFile(new TiFileHeader { FileName = "RECORDS", Level3RecordCount = 0x1234, }, default);

        using MemoryStream stream = new();
        TiFileWriter.Write(stream, file);

        byte[] bytes = stream.ToArray();

        Assert.Equal(0x34, bytes[0x0E]);
        Assert.Equal(0x12, bytes[0x0F]);
    }

    /// <summary>
    /// Tests that the Level-3 Record Count is stored in little-endian format in the file header asynchronously.
    /// </summary>
    /// <returns>An asynchronous <see cref="Task" />.</returns>
    [Fact]
    public async Task Level3RecordCountIsLittleEndianAsync()
    {
        ITiFile file = new TiFile(new TiFileHeader { FileName = "RECORDS", Level3RecordCount = 0x1234, }, default);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using MemoryStream stream = new();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        await TiFileWriter.WriteAsync(stream, file);

        byte[] bytes = stream.ToArray();

        Assert.Equal(0x34, bytes[0x0E]);
        Assert.Equal(0x12, bytes[0x0F]);
    }

    /// <summary>
    /// Tests that the reader throws an exception when the file does not have a valid header.
    /// </summary>
    [Fact]
    public void ReaderRejectsInvalidSignature()
    {
        byte[] bytes = new byte[128];

        using MemoryStream stream = new(bytes);
        _ = Assert.Throws<TiFileFormatException>(() => TiFileReader.Read(stream));
    }

    /// <summary>
    /// Tests that the reader throws an exception when the file does not have a valid header asynchronously.
    /// </summary>
    /// <returns>An asynchronous <see cref="Task" />.</returns>
    [Fact]
    public async Task ReaderRejectsInvalidSignatureAsync()
    {
        byte[] bytes = new byte[128];

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using MemoryStream stream = new(bytes);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        _ = await Assert.ThrowsAsync<TiFileFormatException>(() => TiFileReader.ReadAsync(stream));
    }

    /// <summary>
    /// Tests that unknown header bytes are preserved.
    /// </summary>
    [Fact]
    public void UnknownHeaderBytesArePreserved()
    {
        byte[] header = [.. Enumerable.Repeat((byte)0x20, 128)];

        header[0] = 7;
        TiFilesSignature.Signature.CopyTo(header.AsSpan(1));
        header[0x50] = 0xA5;

        ITiFile file = new TiFile(new TiFileHeader { FileName = "TEST" }, default, header);

        using MemoryStream stream = new();
        TiFileWriter.Write(stream, file);
        Assert.Equal(0xA5, stream.ToArray()[0x50]);
    }

    /// <summary>
    /// Tests that unknown header bytes are preserved asynchronously.
    /// </summary>
    /// <returns>An asynchronous <see cref="Task" />.</returns>
    [Fact]
    public async Task UnknownHeaderBytesArePreservedAsync()
    {
        byte[] header = [.. Enumerable.Repeat((byte)0x20, 128)];

        header[0] = 7;
        TiFilesSignature.Signature.CopyTo(header.AsSpan(1));
        header[0x50] = 0xA5;

        ITiFile file = new TiFile(new TiFileHeader { FileName = "TEST" }, default, header);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using MemoryStream stream = new();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        await TiFileWriter.WriteAsync(stream, file);
        Assert.Equal(0xA5, stream.ToArray()[0x50]);
    }

    /// <summary>
    /// Tests that the "Is TIFILES" asynchronous call returns true and restores the stream position.
    /// </summary>
    /// <returns>An asynchronous <see cref="Task" />.</returns>
    [Fact]
    public async Task IsTiFileAsyncReturnsTrueAndRestoresStreamPositionAsync()
    {
        ITiFile file = new TiFile(new TiFileHeader { FileName = "TEST", }, default);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using MemoryStream stream = new();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        await TiFileWriter.WriteAsync(stream, file);
        stream.Position = 0;

        bool result = await TiFileReader.IsTiFileAsync(stream);

        Assert.True(result);
        Assert.Equal(0, stream.Position);
    }

    /// <summary>
    /// Tests that the "Is TIFILES" asynchronous call returns false for an invalid signature.
    /// </summary>
    /// <returns>An asynchronous <see cref="Task" />.</returns>
    [Fact]
    public async Task IsTiFileAsyncReturnsFalseForInvalidSignatureAsync()
    {
        byte[] bytes = new byte[128];

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using MemoryStream stream = new(bytes);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

        bool result = await TiFileReader.IsTiFileAsync(stream);

        Assert.False(result);
        Assert.Equal(0, stream.Position);
    }

    /// <summary>
    /// Tests the asynchronous Write call honors a cancelled token.
    /// </summary>
    /// <returns>An asynchronous <see cref="Task" />.</returns>
    [Fact]
    public async Task WriteAsyncHonorsCancellationAsync()
    {
        ITiFile file = new TiFile(
            new TiFileHeader
            {
                FileName = "TEST",
            },
            (byte[])[1, 2, 3]);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using MemoryStream stream = new();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TiFileWriter.WriteAsync(
            stream,
            file,
            cancellationTokenSource.Token));
    }

    /// <summary>
    /// Tests the asynchronous Read call honors a cancelled token.
    /// </summary>
    /// <returns>An asynchronous <see cref="Task" />.</returns>
    [Fact]
    public async Task ReadAsyncHonorsCancellationAsync()
    {
        ITiFile file = new TiFile(
            new TiFileHeader
            {
                FileName = "TEST",
            },
            (byte[])[1, 2, 3]);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using MemoryStream stream = new();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        await TiFileWriter.WriteAsync(stream, file);
        stream.Position = 0;
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TiFileReader.ReadAsync(
            stream,
            cancellationTokenSource.Token));
    }
}