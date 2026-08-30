// <copyright file="TiFileFormatException.cs" company="Always Elucidated Solution Pioneers, LLC">
// Copyright (c) Always Elucidated Solution Pioneers, LLC. All rights reserved.
// </copyright>

// Ignore Spelling: hresult
namespace TiFiles;

/// <summary>
/// Exception that is thrown when a TIFILES file is not in the correct format.
/// </summary>
/// <seealso cref="IOException" />
public sealed class TiFileFormatException : IOException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TiFileFormatException" /> class.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public TiFileFormatException()
    {
        // Intentionally empty.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TiFileFormatException" /> class.
    /// </summary>
    /// <param name="message">A <see cref="string" /> that describes the error. The content of
    /// <paramref name="message" /> is intended to be understood by humans. The caller of this constructor is required
    /// to ensure that this string has been localized for the current system culture.</param>
    public TiFileFormatException(string message)
        : base(message)
    {
        // Intentionally empty.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TiFileFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception. If the
    /// <paramref name="innerException" /> parameter is not <see langword="null" />, the current exception is raised in
    /// a <see langword="catch" /> block that handles the inner exception.</param>
    // ReSharper disable once UnusedMember.Global
    public TiFileFormatException(string? message, Exception? innerException)
        : base(message, innerException)
    {
        // Intentionally empty.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TiFileFormatException" /> class.
    /// </summary>
    /// <param name="message">A <see cref="string" /> that describes the error. The content of
    /// <paramref name="message" /> is intended to be understood by humans. The caller of this constructor is required
    /// to ensure that this string has been localized for the current system culture.</param>
    /// <param name="hresult">An integer identifying the error that has occurred.</param>
    // ReSharper disable once UnusedMember.Global
    public TiFileFormatException(string? message, int hresult)
        : base(message, hresult)
    {
        // Intentionally empty.
    }
}