// <copyright file="TiFileFlags.cs" company="Always Elucidated Solution Pioneers, LLC">
// Copyright (c) Always Elucidated Solution Pioneers, LLC. All rights reserved.
// </copyright>

namespace TiFiles;

/// <summary>
///   MSB                                                                      LSB
/// 0 FIXED     Reserved normal       Unmodified Unprotected Reserved DISPLAY  Data
/// 1 VARIABLE  Reserved Emulate File Modified   Protected   Reserved INTERNAL Program.
/// </summary>
[Flags]
#pragma warning disable CA1028 // Enum Storage should be Int32
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
#pragma warning disable S2344 // Enumeration type names should not have "Flags" or "Enum" suffixes
public enum TiFileFlags : byte
#pragma warning restore S2344 // Enumeration type names should not have "Flags" or "Enum" suffixes
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
#pragma warning restore CA1028 // Enum Storage should be Int32
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// File is a program. Data if not set.
    /// </summary>
    Program = 0x01,

    /// <summary>
    /// File has internal (binary) layout. Display (text) if not set.
    /// </summary>
    Internal = 0x02,

    /// <summary>
    /// Reserved bit.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
#pragma warning disable CA1700
    Reserved04 = 0x04,
#pragma warning restore CA1700

    /// <summary>
    /// File is protected. Unprotected if not set.
    /// </summary>
    Protected = 0x08,

    /// <summary>
    /// File has been modified. Unmodified if not set.
    /// </summary>
    Modified = 0x10,

    /// <summary>
    /// Reserved bit.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
#pragma warning disable CA1700
    Reserved20 = 0x20,
#pragma warning restore CA1700

    /// <summary>
    /// File is an emulated disk. Normal file if not set.
    /// </summary>
    Emulate = 0x40,

    /// <summary>
    /// File has variable length records. Fixed length records if not set.
    /// </summary>
    Variable = 0x80,
}