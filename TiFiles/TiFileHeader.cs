// <copyright file="TiFileHeader.cs" company="Always Elucidated Solution Pioneers, LLC">
// Copyright (c) Always Elucidated Solution Pioneers, LLC. All rights reserved.
// </copyright>

// Ignore Spelling: Mxt
namespace TiFiles;

/// <summary>
/// The TIFILES format consists of a header and a body. The header is 128 bytes long and contains the file format
/// information. The body contains the file contents as a sequence of the contents of those sectors that the file
/// occupies on the TI file system. The file length is a multiple of the sector length (256) plus the 128 bytes header.
/// </summary>
public sealed class TiFileHeader : ITiFileHeader
{
    /// <summary>
    /// The size of the TIFILES header in bytes.
    /// </summary>
    public const int HeaderLength = 128;

    /// <summary>
    /// The size of a sector in bytes.
    /// </summary>
    public const int SectorLength = 256;

    /// <summary>
    /// Offset from the beginning of the file where the identifier can be located.
    /// </summary>
    internal const int IdentifierOffset = 0;

    /// <summary>
    /// Single files should only have 0x07 as the first byte.
    /// </summary>
    internal const byte StandardIdentifier = 0x07;

    /// <summary>
    /// The first byte may be set to 0x08 for multi-file transfer using MXT YModem.
    /// In that case, the MXT field is set to 0 if this is the last file in the file sequence, and non-null if there
    /// are more files.
    /// </summary>
    internal const byte ExtendedIdentifier = 0x08;

    /// <summary>
    /// Offset from the beginning of the file where the signature can be located.
    /// </summary>
    internal const int SignatureOffset = 1;

    /// <summary>
    /// The length of the signature in bytes.
    /// </summary>
    internal const int SignatureLength = 7;

    /// <summary>
    /// Offset from the beginning of the file where the preamble can be located.
    /// </summary>
    internal const int PreambleLength = SignatureOffset + SignatureLength;

    /// <summary>
    /// Offset from the beginning of the file where the total sectors can be located.
    /// </summary>
    internal const int TotalSectorsOffset = 0x08;

    /// <summary>
    /// Offset from the beginning of the file where the flags can be located.
    /// </summary>
    internal const int FlagsOffset = 0x0A;

    /// <summary>
    /// Offset from the beginning of the file where the records per sector can be located.
    /// </summary>
    internal const int RecordsPerSectorOffset = 0x0B;

    /// <summary>
    /// Offset from the beginning of the file where the end of file offset can be located.
    /// </summary>
    internal const int EndOfFileOffsetPosition = 0x0C;

    /// <summary>
    /// Offset from the beginning of the file where the record length can be located.
    /// </summary>
    internal const int RecordLengthOffset = 0x0D;

    /// <summary>
    /// Offset from the beginning of the file where the Level-3 record count can be located.
    /// </summary>
    internal const int Level3RecordCountOffset = 0x0E;

    /// <summary>
    /// Offset from the beginning of the file where the file name can be located.
    /// </summary>
    internal const int FileNameOffset = 0x10;

    /// <summary>
    /// The length of the file name in bytes.
    /// </summary>
    internal const int FileNameLength = 10;

    /// <summary>
    /// The byte value which pads the filename if it is less than 10 characters long.
    /// </summary>
    internal const byte FileNamePaddingByte = 0x20;

    /// <summary>
    /// Offset from the beginning of the file where the MXT field can be located.
    /// </summary>
    internal const int MxtOffset = 0x1A;

    /// <summary>
    /// Offset from the beginning of the file where the reserved bytes can be located.
    /// </summary>
    internal const int Reserved1BOffset = 0x1B;

    /// <summary>
    /// Offset from the beginning of the file where the extended header can be located.
    /// </summary>
    internal const int ExtendedHeaderOffset = 0x1C;

    /// <summary>
    /// Offset from the beginning of the file where the created timestamp can be located.
    /// </summary>
    internal const int CreatedTimestampOffset = 0x1E;

    /// <summary>
    /// Offset from the beginning of the file where the modified timestamp can be located.
    /// </summary>
    internal const int UpdatedTimestampOffset = 0x22;

    /// <summary>
    /// The length of the timestamp in bytes.
    /// </summary>
    internal const int TimestampLength = 4;

    /// <summary>
    /// The length of a <see cref="ushort" /> field in bytes.
    /// </summary>
    internal const int UshortFieldLength = sizeof(ushort);

    /// <summary>
    /// The maximum code point value for an ASCII character.
    /// </summary>
    internal const int MaximumAsciiCodePoint = 0x7F;

    /// <summary>
    /// The default value for the extended header field.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    internal const ushort DefaultExtendedHeader = ushort.MaxValue;

    /// <summary>
    /// Gets the TIFILES identifier.
    /// Single files should only have 0x07 as the first byte.
    /// The first byte may be set to 0x08 for multi-file transfer using MXT YModem.  In that case, the MXT field is set
    /// to 0 if this is the last file in the file sequence, and non-null if there are more files.
    /// </summary>
    public byte Identifier { get; init; } = StandardIdentifier;

    /// <summary>
    /// Gets or sets the total number of sectors.
    /// </summary>
    public ushort TotalSectors { get; set; }

    /// <summary>
    /// Gets the flags.
    /// </summary>
    public TiFileFlags Flags { get; init; }

    /// <summary>
    /// Gets the number of records per sector.
    /// </summary>
    public byte RecordsPerSector { get; init; }

    /// <summary>
    /// Gets or sets the end of file offset.
    /// The EOF offset is the location in the last sector where we find the EOF marker. Only variable length data files
    /// have an EOF marker (0xff). For program files and fixed length files this field points to the first byte after
    /// the file contents. If the EOF offset contains 0, the last sector is completely filled with data.
    /// The length of the complete TIFILE-encoded file is always a multiple of 128. To recreate the actual file length,
    /// - the total number of sectors must be multiplied by 256
    /// - the number of bytes past the EOF marker must be subtracted (if the offset is not 0).
    /// That is, if the number of sectors is 10 and the EOF offset is 36, the file length is
    /// (10*256) - (256-36) = 2340. If the EOF offset is zero, we get the full 2560 bytes.
    /// </summary>
    public byte EndOfFileOffset { get; set; }

    /// <summary>
    /// Gets the record length.
    /// </summary>
    public byte RecordLength { get; init; }

    /// <summary>
    /// Gets the number of Level-3 records.
    /// NOTE: The bytes in this field are in reverse order (little-endian).
    /// In the case of fixed length records, the field "Number of Level-3 records" contains the highest record actually
    /// written to. If the last sector is filled as far as possible, we have
    /// L3 = Records/Sector * Total number of sectors
    /// This number is required to determine the highest record number, especially if the last sector is not filled
    /// completely.
    /// In the case of variable length records, it contains the highest sector actually written to and should therefore
    /// be equal to the field "Total number of sectors".
    /// For program files, 0x0000 is usually found in this field.
    /// The high byte of the record count as used in the SCSI software specification is not included in the TIFILES
    /// header. This byte is required for large files with more than 65535 records; thus, the TIFILES format does not
    /// support files with such a very large number of records.
    /// </summary>
    public ushort Level3RecordCount { get; init; }

    /// <summary>
    /// Gets the file name.
    /// The file name is a 10-byte field that contains the name of the file. The file name is padded with spaces (0x20)
    /// if it is less than 10 bytes long. The file name is stored in ASCII format and is not null-terminated. It is
    /// sometimes empty, such as with file transfers from TELCO.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the MXT flag.
    /// The MXT flag is used for chains of files; 0 means last file.
    /// The first byte may be set to 0x08 for multi-file transfer using MXT YModem. Single files should only have 0x07
    /// as the first byte. In that case, the MXT field is set to 0 if this is the last file in the file sequence, and
    /// non-null if there are more files.
    /// </summary>
    public byte Mxt { get; init; }

    /// <summary>
    /// Gets the reserved byte at offset 0x1B.
    /// </summary>
    public byte Reserved1B { get; init; }

    /// <summary>
    /// Gets the extended header.
    /// The extended header field, set to 0xffff, indicates that there are additional fields in the header; currently,
    /// there are the creation and update time. Otherwise, this field contains 0x0000.
    /// </summary>
    public ushort ExtendedHeader { get; init; } = DefaultExtendedHeader;

    /// <summary>
    /// Gets the creation time.
    /// The creation time specification are two 16-bit words, the bits having the following meaning:
    /// ReSharper disable once CommentTypo
    /// hhhh.hmmm.mmms.ssss
    /// yyyy.yyyM.MMMd.dddd
    /// With only 5 bits for seconds, the timestamp has a resolution of 2 seconds. The years reach from 1970 (values
    /// 70..99) to 2069 (values 0..69).
    /// </summary>
    public DateTime? Created { get; init; }

    /// <summary>
    /// Gets the update time.
    /// The update time specification are two 16-bit words, the bits having the following meaning:
    /// ReSharper disable once CommentTypo
    /// hhhh.hmmm.mmms.ssss
    /// yyyy.yyyM.MMMd.dddd
    /// With only 5 bits for seconds, the timestamp has a resolution of 2 seconds. The years reach from 1970 (values
    /// 70..99) to 2069 (values 0..69).
    /// </summary>
    public DateTime? Updated { get; init; }

    /// <summary>
    /// Gets a value indicating whether the file organization is Variable.
    /// </summary>
    public bool IsVariable => Flags.HasFlag(TiFileFlags.Variable);

    /// <summary>
    /// Gets a value indicating whether the file organization is Program.
    /// </summary>
    public bool IsProgram => Flags.HasFlag(TiFileFlags.Program);

    /// <summary>
    /// Gets a value indicating whether the file type is Internal.
    /// </summary>
    public bool IsInternal => Flags.HasFlag(TiFileFlags.Internal);

    /// <summary>
    /// Gets a value indicating whether the file type is Display.
    /// </summary>
    public bool IsDisplay => !IsInternal;

    /// <summary>
    /// Gets a value indicating whether the file organization is Fixed.
    /// </summary>
    public bool IsFixed => !IsVariable;

    /// <summary>
    /// Gets a value indicating whether the file is protected.
    /// </summary>
    public bool IsProtected => Flags.HasFlag(TiFileFlags.Protected);

    /// <summary>
    /// Gets a value indicating whether the file has been modified.
    /// The Modified bit is set to one when a write operation occurs on this file (not with all DSRs). It may be reset
    /// by backup programs.
    /// </summary>
    public bool IsModified => Flags.HasFlag(TiFileFlags.Modified);

    /// <summary>
    /// Gets the file organization string value: Variable or Fixed.
    /// </summary>
    public string Organization => IsVariable ? "Variable" : "Fixed";

#pragma warning disable S3358 // Ternary operators should not be nested
    /// <summary>
    /// Gets the file type string value: Program, Internal, or Display.
    /// </summary>
    public string DataType => IsProgram ? "Program" : IsInternal ? "Internal" : "Display";
#pragma warning restore S3358 // Ternary operators should not be nested
}