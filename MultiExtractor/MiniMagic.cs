// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MultiExtractor
{
    /// <summary>
    /// ArchiveTypes are the kinds of archive files that this module can process.
    /// </summary>
    public enum ArchiveFileType
    {
        UNKNOWN,
        ZIP,
        TAR,
        XZ,
        GZIP,
        BZIP2,
        RAR,
        P7ZIP,
        DEB,
        AR,
        ISO_9660,
        VHDX,
        VHD,
        WIM,
        VMDK
    }

    /// <summary>
    /// MiniMagic is a tiny implementation of a file type identifier based on binary signatures.
    /// </summary>
    public static class MiniMagic
    {
        /// <summary>
        /// Fallback using file extensions in case the binary signature doesn't match.
        /// </summary>
        private static readonly Dictionary<string, ArchiveFileType> FileExtensionMap = new Dictionary<string, ArchiveFileType>()
        {
            {"ZIP", ArchiveFileType.ZIP },
            {"APK", ArchiveFileType.ZIP },
            {"IPA", ArchiveFileType.ZIP },
            {"JAR", ArchiveFileType.ZIP },
            {"EAR", ArchiveFileType.ZIP },
            {"WAR", ArchiveFileType.ZIP },

            {"GZ", ArchiveFileType.GZIP },
            {"TGZ", ArchiveFileType.GZIP },

            {"TAR", ArchiveFileType.TAR },
            {"GEM", ArchiveFileType.TAR },

            {"XZ", ArchiveFileType.XZ },

            {"BZ2", ArchiveFileType.BZIP2 },

            {"RAR", ArchiveFileType.RAR },
            {"RAR4", ArchiveFileType.RAR },

            {"7Z", ArchiveFileType.P7ZIP },

            {"DEB", ArchiveFileType.DEB },

            {"AR", ArchiveFileType.AR },

            {"ISO", ArchiveFileType.ISO_9660 },

            {"VHDX", ArchiveFileType.VHDX },

            {"VHD", ArchiveFileType.VHD },

            {"WIM", ArchiveFileType.WIM },

            {"VMDK", ArchiveFileType.VMDK }
        };

        public static ArchiveFileType DetectFileType(string filename)
        {
            #pragma warning disable SEC0116 // Path Tampering Unvalidated File Path
            using var fs = new FileStream(filename,FileMode.Open);
            #pragma warning restore SEC0116 // Path Tampering Unvalidated File Path

            // If you don't pass passthroughStream: true here it will read the entire file into the stream in FileEntry
            // This way it will only read the bytes minimagic uses
            var fileEntry = new FileEntry(filename, fs, null, passthroughStream: true);
            return DetectFileType(fileEntry);
        }

        /// <summary>
        /// Detects the type of a file.
        /// </summary>
        /// <param name="fileEntry">FileEntry containing the file data.</param>
        /// <returns></returns>
        public static ArchiveFileType DetectFileType(FileEntry fileEntry)
        {
            if (fileEntry == null)
            {
                return ArchiveFileType.UNKNOWN;
            }
            var initialPosition = fileEntry.Content.Position;
            Span<byte> buffer = stackalloc byte[9];
            if (fileEntry.Content.Length >= 9)
            {
                fileEntry.Content.Position = 0;
                fileEntry.Content.Read(buffer);
                fileEntry.Content.Position = initialPosition;

                if (buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04)
                {
                    return ArchiveFileType.ZIP;
                }

                if (buffer[0] == 0x1F && buffer[1] == 0x8B)
                {
                    return ArchiveFileType.GZIP;
                }

                if (buffer[0] == 0xFD && buffer[1] == 0x37 && buffer[2] == 0x7A && buffer[3] == 0x58 && buffer[4] == 0x5A && buffer[5] == 0x00)
                {
                    return ArchiveFileType.XZ;
                }
                if (buffer[0] == 0x42 && buffer[1] == 0x5A && buffer[2] == 0x68)
                {
                    return ArchiveFileType.BZIP2;
                }
                if ((buffer[0] == 0x52 && buffer[1] == 0x61 && buffer[2] == 0x72 && buffer[3] == 0x21 && buffer[4] == 0x1A && buffer[5] == 0x07 && buffer[6] == 0x00) ||
                    (buffer[0] == 0x52 && buffer[1] == 0x61 && buffer[2] == 0x72 && buffer[3] == 0x21 && buffer[4] == 0x1A && buffer[5] == 0x07 && buffer[6] == 0x01 && buffer[7] == 0x00))
                {
                    return ArchiveFileType.RAR;
                }
                if (buffer[0] == 0x37 && buffer[1] == 0x7A && buffer[2] == 0xBC && buffer[3] == 0xAF && buffer[4] == 0x27 && buffer[5] == 0x1C)
                {
                    return ArchiveFileType.P7ZIP;
                }
                if (Encoding.ASCII.GetString(buffer.Slice(0,8)) == "MSWIM\0\0\0" || Encoding.ASCII.GetString(buffer.Slice(0, 8)) == "WLPWM\0\0\0")
                {
                    return ArchiveFileType.WIM;
                }
                if (Encoding.ASCII.GetString(buffer.Slice(0,4)) == "KDMV")
                {
                    fileEntry.Content.Position = 512;
                    Span<byte> secondToken = stackalloc byte[21];
                    fileEntry.Content.Read(secondToken);
                    fileEntry.Content.Position = initialPosition;

                    if (Encoding.ASCII.GetString(secondToken) == "# Disk DescriptorFile")
                    {
                        return ArchiveFileType.VMDK;
                    }
                }
                // some kind of unix Archive https://en.wikipedia.org/wiki/Ar_(Unix)
                if (buffer[0] == 0x21 && buffer[1] == 0x3c && buffer[2] == 0x61 && buffer[3] == 0x72 && buffer[4] == 0x63 && buffer[5] == 0x68 && buffer[6] == 0x3e)
                {   
                    // .deb https://manpages.debian.org/unstable/dpkg-dev/deb.5.en.html
                    fileEntry.Content.Position = 68;
                    fileEntry.Content.Read(buffer.Slice(0, 4));
                    fileEntry.Content.Position = initialPosition;
                    
                    var encoding = new ASCIIEncoding();
                    if (encoding.GetString(buffer.Slice(0,4)) == "2.0\n")
                    {
                        return ArchiveFileType.DEB;
                    }
                    else
                    {
                        Span<byte> headerBuffer = stackalloc byte[60];

                        // Created by GNU ar https://en.wikipedia.org/wiki/Ar_(Unix)#System_V_(or_GNU)_variant
                        fileEntry.Content.Position = 8;
                        fileEntry.Content.Read(headerBuffer);
                        fileEntry.Content.Position = initialPosition;

                        var size = int.Parse(Encoding.ASCII.GetString(headerBuffer.Slice(48, 10))); // header size in bytes

                        if (size > 0)
                        {
                            // Defined ending characters for a header
                            if (headerBuffer[58]=='`' && headerBuffer[59] == '\n')
                            {
                                return ArchiveFileType.AR;
                            }
                        }
                    }
                }
                // https://winprotocoldoc.blob.core.windows.net/productionwindowsarchives/MS-VHDX/%5bMS-VHDX%5d.pdf
                if (Encoding.UTF8.GetString(buffer.Slice(0,8)).Equals("vhdxfile"))
                {
                    return ArchiveFileType.VHDX;
                }
            }

            if (fileEntry.Content.Length >= 262)
            {
                fileEntry.Content.Position = 257;
                fileEntry.Content.Read(buffer.Slice(0,5));
                fileEntry.Content.Position = initialPosition;

                if (buffer[0] == 0x75 && buffer[1] == 0x73 && buffer[2] == 0x74 && buffer[3] == 0x61 && buffer[4] == 0x72)
                {
                    return ArchiveFileType.TAR;
                }
            }

            // ISO Format https://en.wikipedia.org/wiki/ISO_9660#Overall_structure
            // Reserved space + 1 header
            if (fileEntry.Content.Length > 32768 + 2048)
            {
                fileEntry.Content.Position = 32769;
                fileEntry.Content.Read(buffer.Slice(0, 5));
                fileEntry.Content.Position = initialPosition;

                if (buffer[0] == 'C' && buffer[1] == 'D' && buffer[2] == '0' && buffer[3] == '0' && buffer[4] == '1')
                {
                    return ArchiveFileType.ISO_9660;
                }
            }

            //https://www.microsoft.com/en-us/download/details.aspx?id=23850 - 'Hard Disk Footer Format'
            // Unlike other formats the magic string is stored in the footer, which is either the last 511 or 512 bytes
            // The magic string is Magic string "conectix" (63 6F 6E 65 63 74 69 78)
            if (fileEntry.Content.Length > 512)
            {
                Span<byte> vhdFooterCookie = stackalloc byte[] { 0x63, 0x6F, 0x6E, 0x65, 0x63, 0x74, 0x69, 0x78 };

                fileEntry.Content.Position = fileEntry.Content.Length - 0x200; // Footer position
                fileEntry.Content.Read(buffer);
                fileEntry.Content.Position = initialPosition;

                if (vhdFooterCookie.SequenceEqual(buffer.Slice(0, 8))
                       || vhdFooterCookie.SequenceEqual(buffer.Slice(1)))//If created on legacy platform
                {
                    return ArchiveFileType.VHD;
                }
            }

            // Fall back to file extensions
            string fileExtension = Path.GetExtension(fileEntry.Name.ToUpperInvariant());

            if (fileExtension.StartsWith('.'))
            {
                fileExtension = fileExtension.Substring(1);
            }
            if (!FileExtensionMap.TryGetValue(fileExtension, out ArchiveFileType fileType))
            {
                fileType = ArchiveFileType.UNKNOWN;
            }
            return fileType;
        }
    }
}
