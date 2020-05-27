// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.ApplicationInspector.MultiExtractor
{
    /**
     * Gnu Ar file parser.  Supports SystemV style lookup tables in both 32 and 64 bit mode as well as BSD and GNU formatted .ars.
     */
    public static class ArFile
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        // Simple method which returns a the file entries. We can't make this a continuation because
        // we're using spans.
        public static IEnumerable<FileEntry> GetFileEntries(FileEntry fileEntry)
        {
            if (fileEntry == null)
            {
                yield break;
            }
            // First, cut out the file signature (8 bytes)
            fileEntry.Content.Position = 8;
            var filenameLookup = new Dictionary<int, string>();
            byte[] headerBuffer = new byte[60];
            while (true)
            {
                if (fileEntry.Content.Length - fileEntry.Content.Position < 60)  // The header for each file is 60 bytes
                {
                    break;
                }

                fileEntry.Content.Read(headerBuffer, 0, 60);

                if (long.TryParse(Encoding.ASCII.GetString(headerBuffer[48..58]), out long size))// header size in bytes
                {
                    var filename = Encoding.ASCII.GetString(headerBuffer[0..16]).Trim();

                    // Header with list of file names
                    if (filename.StartsWith("//"))
                    {
                        // This should just be a list of names, size should be safe to load in memory and cast to int
                        var fileNamesBytes = new byte[size];
                        fileEntry.Content.Read(fileNamesBytes, 0, (int)size);

                        var name = new StringBuilder();
                        var index = 0;
                        for (int i = 0; i < fileNamesBytes.Length; i++)
                        {
                            if (fileNamesBytes[i] == '/')
                            {
                                filenameLookup.Add(index, name.ToString());
                                name.Clear();
                            }
                            else if (fileNamesBytes[i] == '\n')
                            {
                                // The next filename would start on the next line
                                index = i + 1;
                            }
                            else
                            {
                                name.Append((char)fileNamesBytes[i]);
                            }
                        }
                    }
                    else if (filename.StartsWith("#1/"))
                    {
                        // We should be positioned right after the header
                        if (int.TryParse(filename.Substring(3), out int nameLength))
                        {
                            Span<byte> nameSpan = stackalloc byte[nameLength];
                            // This should move us right to the file
                            fileEntry.Content.Read(nameSpan);

                            var entryStream = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
                            
                            // The name length is included in the total size reported in the header
                            CopyStreamBytes(fileEntry.Content, entryStream, size - nameLength);

                            yield return new FileEntry(Encoding.ASCII.GetString(nameSpan), entryStream, fileEntry, true);
                        }
                    }
                    else if (filename.Equals('/'))
                    {
                        // System V symbol lookup table
                        // N = 32 bit big endian integers (entries in table)
                        // then N 32 bit big endian integers representing prositions in archive
                        // then N \0 terminated strings "symbol name" (possibly filename)

                        var tableContents = new Span<byte>(new byte[size]);
                        fileEntry.Content.Read(tableContents);

                        var numEntries = IntFromBigEndianBytes(tableContents.Slice(0, 4).ToArray());
                        var filePositions = new int[numEntries];
                        for (int i = 0; i < numEntries; i++)
                        {
                            filePositions[i] = IntFromBigEndianBytes(tableContents.Slice((i + 1) * 4, 4).ToArray());
                        }

                        var index = 0;
                        var sb = new StringBuilder();
                        var fileEntries = new List<(int, string)>();

                        for (int i = 0; i< tableContents.Length; i++)
                        {
                            if (tableContents.Slice(i, 1)[0] == '\0')
                            {
                                fileEntries.Add((filePositions[index++], sb.ToString()));
                                sb.Clear();
                            }
                            else
                            {
                                sb.Append(tableContents.Slice(i, 1)[0]);
                            }
                        }

                        foreach (var entry in fileEntries)
                        {
                            fileEntry.Content.Position = entry.Item1;
                            fileEntry.Content.Read(headerBuffer, 0, 60);

                            if (long.TryParse(Encoding.ASCII.GetString(headerBuffer[48..58]), out long innerSize))// header size in bytes
                            {
                                if (filename.StartsWith('/'))
                                {
                                    if (int.TryParse(filename[1..], out int innerIndex))
                                    {
                                        try
                                        {
                                            filename = filenameLookup[innerIndex];
                                        }
                                        catch (Exception)
                                        {
                                            Logger.Debug("Expected to find a filename at index {0}", innerIndex);
                                        }
                                    }
                                }
                                else
                                {
                                    filename = entry.Item2;
                                }

                                var entryStream = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
                                CopyStreamBytes(fileEntry.Content, entryStream, innerSize);
                                yield return new FileEntry(filename, entryStream, fileEntry);
                            }
                        }
                        fileEntry.Content.Position = fileEntry.Content.Length - 1;
                    }
                    else if (filename.Equals("/SYM64/"))
                    {
                        // https://en.wikipedia.org/wiki/Ar_(Unix)#System_V_(or_GNU)_variant
                        // GNU lookup table (archives larger than 4GB)
                        // N = 64 bit big endian integers (entries in table)
                        // then N 64 bit big endian integers representing positions in archive
                        // then N \0 terminated strings "symbol name" (possibly filename)

                        var buffer = new byte[8];
                        fileEntry.Content.Read(buffer, 0, 8);

                        var numEntries = Int64FromBigEndianBytes(buffer);
                        var filePositions = new long[numEntries];

                        for (int i = 0; i < numEntries; i++)
                        {
                            fileEntry.Content.Read(buffer, 0, 8);
                            filePositions[i] = Int64FromBigEndianBytes(buffer);
                        }

                        var index = 0;
                        var sb = new StringBuilder();
                        var fileEntries = new List<(long, string)>();

                        while (fileEntry.Content.Position < size)
                        {
                            fileEntry.Content.Read(buffer, 0, 1);
                            if (buffer[0] == '\0')
                            {
                                fileEntries.Add((filePositions[index++], sb.ToString()));
                                sb.Clear();
                            }
                            else
                            {
                                sb.Append(buffer[0]);
                            }
                        }

                        foreach (var innerEntry in fileEntries)
                        {
                            fileEntry.Content.Position = innerEntry.Item1;

                            fileEntry.Content.Read(headerBuffer, 0, 60);

                            if (long.TryParse(Encoding.ASCII.GetString(headerBuffer[48..58]), out long innerSize))// header size in bytes
                            {
                                if (filename.StartsWith('/'))
                                {
                                    if (int.TryParse(filename[1..], out int innerIndex))
                                    {
                                        try
                                        {
                                            filename = filenameLookup[innerIndex];
                                        }
                                        catch (Exception)
                                        {
                                            Logger.Debug("Expected to find a filename at index {0}", innerIndex);
                                        }
                                    }
                                }
                                else
                                {
                                    filename = innerEntry.Item2;
                                }
                                var entryStream = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
                                CopyStreamBytes(fileEntry.Content, entryStream, innerSize);
                                yield return new FileEntry(filename, entryStream, fileEntry);
                            }
                        }
                        fileEntry.Content.Position = fileEntry.Content.Length - 1;
                    }
                    else if (filename.StartsWith('/'))
                    {
                        if (int.TryParse(filename[1..], out int index))
                        {
                            try
                            {
                                filename = filenameLookup[index];
                            }
                            catch (Exception)
                            {
                                Logger.Debug("Expected to find a filename at index {0}", index);
                            }
                        }

                        var entryStream = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
                        CopyStreamBytes(fileEntry.Content, entryStream, size);

                        yield return new FileEntry(filename, entryStream, fileEntry, true);
                    }
                    else
                    {
                        var entryStream = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
                        CopyStreamBytes(fileEntry.Content, entryStream, size);

                        yield return new FileEntry(filename, entryStream, fileEntry, true);
                    }
                }
                else
                {
                    // Not a valid header, we couldn't parse the file size.
                    yield break;
                }

                // Entries are padded on even byte boundaries
                // https://docs.oracle.com/cd/E36784_01/html/E36873/ar.h-3head.html  
                fileEntry.Content.Position = fileEntry.Content.Position % 2 == 1 ? fileEntry.Content.Position + 1 : fileEntry.Content.Position;
            }
        }

        internal static void CopyStreamBytes(Stream input, Stream output, long bytes)
        {
            byte[] buffer = new byte[32768];
            long read;
            while (bytes > 0 &&
                   (read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, bytes))) > 0)
            {
                output.Write(buffer, 0, (int)read);
                bytes -= read;
            }
        }

        public static int IntFromBigEndianBytes(byte[] value)
        {
            if (value.Length == 4)
            {
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value);
                }
                return BitConverter.ToInt32(value);
            }
            return -1;
        }

        public static long Int64FromBigEndianBytes(byte[] value)
        {
            if (value.Length == 8)
            {
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(value);
                }
                return BitConverter.ToInt64(value);
            }
            return -1;
        }
    }
}