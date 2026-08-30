// <copyright file="TiFile.cs" company="Always Elucidated Solution Pioneers, LLC">
// Copyright (c) Always Elucidated Solution Pioneers, LLC. All rights reserved.
// </copyright>

namespace TiFiles;

/// <summary>
/// The TIFILES format is used to encode files from a TI file system for transfer and storage on another system.
/// </summary>
public sealed class TiFile : ITiFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TiFile" /> class.
    /// </summary>
    /// <param name="header">The header.</param>
    /// <param name="data">The data.</param>
    /// <param name="originalHeader">The original header.</param>
    /// <exception cref="ArgumentNullException">header.</exception>
    /// <exception cref="ArgumentException"><paramref name="originalHeader" /> must be exactly 128 bytes.</exception>
    public TiFile(ITiFileHeader? header, ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> originalHeader = default)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Data = data;
        OriginalHeader = originalHeader.IsEmpty
            ? CreateDefaultHeaderImage()
            : originalHeader;

        if (OriginalHeader.Length != TiFileHeader.HeaderLength)
        {
            throw new ArgumentException("The original header must be exactly 128 bytes.", nameof(originalHeader));
        }
    }

    /// <summary>
    /// Gets the TIFILES header.
    /// </summary>
    /// <value>
    /// The TIFILES header.
    /// </value>
    public ITiFileHeader Header { get; }

    /// <summary>
    /// Gets the data.
    /// </summary>
    /// <value>
    /// The data.
    /// </value>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Gets the original header.
    /// </summary>
    /// <value>
    /// The original header.
    /// </value>
    public ReadOnlyMemory<byte> OriginalHeader { get; }

    private static byte[] CreateDefaultHeaderImage()
    {
        byte[] bytes = new byte[TiFileHeader.HeaderLength];

        Array.Fill(bytes, TiFileHeader.FileNamePaddingByte);
        return bytes;
    }
}