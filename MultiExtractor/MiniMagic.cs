// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.


using System;
using System.Collections.Generic;
using System.IO;
using MimeTypes;


namespace MultiExtractor
{
    public enum ArchiveFileType
    {
        UNKNOWN,
        ZIP,
        TAR,
        XZ,
        GZIP,
        BZIP2,
        RAR,
        P7ZIP
    }

    public static class MiniMagic
    {
        
        private static readonly Dictionary<string, ArchiveFileType> FileExtensionMap = new Dictionary<string, ArchiveFileType>()
        {
            {"zip", ArchiveFileType.ZIP },
            {"apk", ArchiveFileType.ZIP },
            {"ipa", ArchiveFileType.ZIP },
            {"jar", ArchiveFileType.ZIP },
            {"ear", ArchiveFileType.ZIP },
            {"war", ArchiveFileType.ZIP },

            {"gz", ArchiveFileType.GZIP },
            {"tgz", ArchiveFileType.GZIP },

            {"tar", ArchiveFileType.TAR },
            {"gem", ArchiveFileType.TAR },

            {"xz", ArchiveFileType.XZ },

            {"bz2", ArchiveFileType.BZIP2 },

            {"rar", ArchiveFileType.RAR },

            {"7z", ArchiveFileType.P7ZIP }

        };


        public static ArchiveFileType DetectFileType(string filename)
        {
            using var memoryStream = new MemoryStream(File.ReadAllBytes(filename));
            return MiniMagic.DetectFileType(new FileEntry(filename, "", memoryStream));
        }


        public static ArchiveFileType DetectFileType(FileEntry fileEntry)
        {
            if (fileEntry == null)
            {
                return ArchiveFileType.UNKNOWN;
            }

            var buffer = new byte[8];
            if (fileEntry.Content.Length >= 8)
            {
                fileEntry.Content.Position = 0;
                fileEntry.Content.Read(buffer, 0, 8);
                fileEntry.Content.Position = 0;
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
            }

            if (fileEntry.Content.Length >= 262)
            {
                fileEntry.Content.Position = 257;
                fileEntry.Content.Read(buffer, 0, 5);
                fileEntry.Content.Position = 0;
                if (buffer[0] == 0x75 && buffer[1] == 0x73 && buffer[2] == 0x74 && buffer[3] == 0x61 && buffer[4] == 0x72)
                {
                    return ArchiveFileType.TAR;
                }
            }

            // Fall back to file extensions
#pragma warning disable CA1308 // Normalize strings to uppercase
            string fileExtension = Path.GetExtension(fileEntry.Name.ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase
            if (fileExtension.StartsWith('.'))
            {
                fileExtension = fileExtension.Substring(1);
            }
            if (!MiniMagic.FileExtensionMap.TryGetValue(fileExtension, out ArchiveFileType fileType))
            {
                fileType = ArchiveFileType.UNKNOWN;
            }
            return fileType;
        }
    }
}

