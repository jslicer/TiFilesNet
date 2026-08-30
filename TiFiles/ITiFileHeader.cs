// <copyright file="ITiFileHeader.cs" company="Always Elucidated Solution Pioneers, LLC">
// Copyright (c) Always Elucidated Solution Pioneers, LLC. All rights reserved.
// </copyright>

namespace TiFiles;

/// <summary>
/// Represents a TIFILES file header.
/// </summary>
public interface ITiFileHeader
{
    /// <summary>
    /// Gets the TIFILES identifier.
    /// Single files should only have 0x07 as the first byte.
    /// The first byte may be set to 0x08 for multi-file transfer using MXT YModem.  In that case, the MXT field is set
    /// to 0 if this is the last file in the file sequence, and non-null if there are more files.
    /// </summary>
    byte Identifier { get; }

    /// <summary>
    /// Gets or sets the total number of sectors.
    /// </summary>
    ushort TotalSectors { get; set; }

    /// <summary>
    /// Gets the flags.
    /// </summary>
    TiFileFlags Flags { get; }

    /// <summary>
    /// Gets the number of records per sector.
    /// </summary>
    byte RecordsPerSector { get; }

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
    byte EndOfFileOffset { get; set; }

    /// <summary>
    /// Gets the record length.
    /// </summary>
    byte RecordLength { get; }

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
    ushort Level3RecordCount { get; }

    /// <summary>
    /// Gets the file name.
    /// The file name is a 10-byte field that contains the name of the file. The file name is padded with spaces (0x20)
    /// if it is less than 10 bytes long. The file name is stored in ASCII format and is not null-terminated. It is
    /// sometimes empty, such as with file transfers from TELCO.
    /// </summary>
    string FileName { get; }

    /// <summary>
    /// Gets the MXT flag.
    /// The MXT flag is used for chains of files; 0 means last file.
    /// The first byte may be set to 0x08 for multi-file transfer using MXT YModem. Single files should only have 0x07
    /// as the first byte. In that case, the MXT field is set to 0 if this is the last file in the file sequence, and
    /// non-null if there are more files.
    /// </summary>
    byte Mxt { get; }

    /// <summary>
    /// Gets the reserved byte at offset 0x1B.
    /// </summary>
    byte Reserved1B { get; }

    /// <summary>
    /// Gets the extended header.
    /// The extended header field, set to 0xffff, indicates that there are additional fields in the header; currently,
    /// there are the creation and update time. Otherwise, this field contains 0x0000.
    /// </summary>
    ushort ExtendedHeader { get; }

    /// <summary>
    /// Gets the creation time.
    /// The creation time specification are two 16-bit words, the bits having the following meaning:
    /// hhhh.hmmm.mmms.ssss
    /// yyyy.yyyM.MMMd.dddd
    /// With only 5 bits for seconds, the timestamp has a resolution of 2 seconds. The years reach from 1970 (values
    /// 70..99) to 2069 (values 0..69).
    /// </summary>
    DateTime? Created { get; }

    /// <summary>
    /// Gets the update time.
    /// The update time specification are two 16-bit words, the bits having the following meaning:
    /// hhhh.hmmm.mmms.ssss
    /// yyyy.yyyM.MMMd.dddd
    /// With only 5 bits for seconds, the timestamp has a resolution of 2 seconds. The years reach from 1970 (values
    /// 70..99) to 2069 (values 0..69).
    /// </summary>
    DateTime? Updated { get; }

    /// <summary>
    /// Gets a value indicating whether the file organization is Variable.
    /// </summary>
    bool IsVariable { get; }

    /// <summary>
    /// Gets a value indicating whether the file organization is Program.
    /// </summary>
    bool IsProgram { get; }

    /// <summary>
    /// Gets a value indicating whether the file type is Internal.
    /// </summary>
    bool IsInternal { get; }

    /// <summary>
    /// Gets a value indicating whether the file type is Display.
    /// </summary>
#pragma warning disable IDE0051
    bool IsDisplay { get; }
#pragma warning restore IDE0051

    /// <summary>
    /// Gets a value indicating whether the file organization is Fixed.
    /// </summary>
#pragma warning disable IDE0051
    bool IsFixed { get; }
#pragma warning restore IDE0051

    /// <summary>
    /// Gets a value indicating whether the file is protected.
    /// </summary>
    bool IsProtected { get; }

    /// <summary>
    /// Gets a value indicating whether the file has been modified.
    /// The Modified bit is set to one when a write operation occurs on this file (not with all DSRs). It may be reset
    /// by backup programs.
    /// </summary>
    bool IsModified { get; }

    /// <summary>
    /// Gets the file organization string value: Variable or Fixed.
    /// </summary>
    string Organization { get; }

    /// <summary>
    /// Gets the file type string value: Program, Internal, or Display.
    /// </summary>
    string DataType { get; }
}