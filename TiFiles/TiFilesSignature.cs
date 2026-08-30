// <copyright file="TiFilesSignature.cs" company="Always Elucidated Solution Pioneers, LLC">
// Copyright (c) Always Elucidated Solution Pioneers, LLC. All rights reserved.
// </copyright>

namespace TiFiles;

using System.Text;

/// <summary>
/// Contains the TIFILES signature used in the file format.
/// </summary>
public static class TiFilesSignature
{
#pragma warning disable IDE0230, IDE1006 // Naming Styles
    private static readonly byte[] _Signature = Encoding.ASCII.GetBytes("TIFILES");
#pragma warning restore IDE1006, IDE0230 // Naming Styles

    /// <summary>
    /// Gets the TIFILES signature used in the file format.
    /// </summary>
    /// <value>
    /// The TIFILES signature used in the file format.
    /// </value>
    public static ReadOnlySpan<byte> Signature => _Signature;
}