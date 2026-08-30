// <copyright file="ITiFile.cs" company="Always Elucidated Solution Pioneers, LLC">
// Copyright (c) Always Elucidated Solution Pioneers, LLC. All rights reserved.
// </copyright>

namespace TiFiles;

/// <summary>
/// Represents a TIFILES file.
/// </summary>
public interface ITiFile
{
    /// <summary>
    /// Gets the TIFILES header.
    /// </summary>
    /// <value>
    /// The TIFILES header.
    /// </value>
    ITiFileHeader Header { get; }

    /// <summary>
    /// Gets the data.
    /// </summary>
    /// <value>
    /// The data.
    /// </value>
    ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Gets the original header.
    /// </summary>
    /// <value>
    /// The original header.
    /// </value>
    ReadOnlyMemory<byte> OriginalHeader { get; }
}