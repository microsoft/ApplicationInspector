using System;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System.IO;
using System.Collections.Generic;
using SharpCompress.Archives.Rar;
using SharpCompress.Compressors.Xz;
using SharpCompress.Compressors.BZip2;
using ICSharpCode.SharpZipLib;
using System.Dynamic;

namespace MultiExtractor
{
    public static class Extractor
    {
        private const int BUFFER_SIZE = 4096;
        private static int _count;
        //private static List<FileEntry> _files;

        public static int Count { get { return _count; } }
        
        public static bool IsSupportedFormat(string filename)
        {
            ArchiveFileType archiveFileType = MiniMagic.DetectFileType(filename);
            return (archiveFileType != ArchiveFileType.UNKNOWN);
        }

        public static IEnumerable<FileEntry> ExtractFile(string filename)
        {
            _count = 0;
            //_files = new List<FileEntry>();
            using var memoryStream = new MemoryStream(File.ReadAllBytes(filename));
            //using var fileEntry = new FileEntry(filename, "", memoryStream);
            return ExtractFile(new FileEntry(filename, "", memoryStream));
            //return _files;
        }
        public static IEnumerable<FileEntry> ExtractFile(string filename, ArchiveFileType archiveFileType)
        {
            _count = 0;
            //_files = new List<FileEntry>();
            using var memoryStream = new MemoryStream(File.ReadAllBytes(filename));
            //using var fileEntry = new FileEntry(filename, "", memoryStream);
            return ExtractFile(new FileEntry(filename, "", memoryStream), archiveFileType);
            //return _files;
        }

        public static IEnumerable<FileEntry> ExtractFile(string filename, byte[] archiveBytes)
        {
            _count = 0;
            //_files = new List<FileEntry>();
            using var memoryStream = new MemoryStream(archiveBytes);
            //using var fileEntry = new FileEntry(filename, "", memoryStream);
            return ExtractFile(new FileEntry(filename, "", memoryStream));
            //return _files;
        }

        #region internal

        private static IEnumerable<FileEntry> ExtractFile(FileEntry fileEntry)
        {
            return ExtractFile(fileEntry, MiniMagic.DetectFileType(fileEntry));
            //return _files;
        }


        private static IEnumerable<FileEntry> ExtractFile(FileEntry fileEntry, ArchiveFileType archiveFileType)
        {
            IEnumerable<FileEntry> results = new List<FileEntry>();

            switch (archiveFileType)
            {
                case ArchiveFileType.ZIP:
                    results = ExtractZipFile(fileEntry);
                    break;

                case ArchiveFileType.GZIP:
                    results = ExtractGZipFile(fileEntry);
                    break;

                case ArchiveFileType.TAR:
                    results = ExtractTarFile(fileEntry);
                    break;

                case ArchiveFileType.XZ:
                    results = ExtractXZFile(fileEntry);
                    break;

                case ArchiveFileType.BZIP2:
                    results = ExtractBZip2File(fileEntry);
                    break;

                case ArchiveFileType.RAR:
                    results = ExtractRarFile(fileEntry);
                    break;

                case ArchiveFileType.P7ZIP:
                    results = Extract7ZipFile(fileEntry);
                    break;

                case ArchiveFileType.UNKNOWN:
                default:
                    results = new[] { fileEntry };
                    break;
            }

            return results;
        }


        private static IEnumerable<FileEntry> ExtractZipFile(FileEntry fileEntry)
        {
            List<FileEntry> files = new List<FileEntry>();
            //Console.WriteLine("Extracting from Zip");
            //Console.WriteLine("Content Size => {0}", fileEntry.Content.Length);
            using var zipFile = new ZipFile(fileEntry.Content);
            foreach (ZipEntry zipEntry in zipFile)
            {
                //Console.WriteLine("Found {0}", zipEntry.Name);
                if (zipEntry.IsDirectory ||
                    zipEntry.IsCrypted ||
                    !zipEntry.CanDecompress)
                {
                    continue;
                }

                using var memoryStream = new MemoryStream();
                byte[] buffer = new byte[BUFFER_SIZE];
                var zipStream = zipFile.GetInputStream(zipEntry);
                StreamUtils.Copy(zipStream, memoryStream, buffer);

                var newFileEntry = new FileEntry(zipEntry.Name, fileEntry.FullPath, memoryStream);
                _count++;
                foreach (var extractedFile in ExtractFile(newFileEntry))
                {
                    files.Add(extractedFile);
                }
            }

            return files;

        }

        private static IEnumerable<FileEntry> ExtractGZipFile(FileEntry fileEntry)
        {
            List<FileEntry> files = new List<FileEntry>();
            using var gzipStream = new GZipInputStream(fileEntry.Content);
            using var memoryStream = new MemoryStream();
            gzipStream.CopyTo(memoryStream);

            var newFilename = Path.GetFileNameWithoutExtension(fileEntry.Name);
            if (fileEntry.Name.EndsWith(".tgz", System.StringComparison.CurrentCultureIgnoreCase))
            {
                newFilename = newFilename[0..^4] + ".tar";
            }

            var newFileEntry = new FileEntry(newFilename, fileEntry.FullPath, memoryStream);
            _count++;

            foreach (var extractedFile in ExtractFile(newFileEntry))
            {
                files.Add(extractedFile);
            }

            return files;

        }

        private static IEnumerable<FileEntry> ExtractTarFile(FileEntry fileEntry)
        {
            List<FileEntry> files = new List<FileEntry>();
            TarEntry tarEntry;
            using var tarStream = new TarInputStream(fileEntry.Content);
            while ((tarEntry = tarStream.GetNextEntry()) != null)
            {
                if (tarEntry.IsDirectory)
                {
                    continue;
                }
                using var memoryStream = new MemoryStream();
                tarStream.CopyEntryContents(memoryStream);

                var newFileEntry = new FileEntry(tarEntry.Name, fileEntry.FullPath, memoryStream);
                _count++;
                foreach (var extractedFile in ExtractFile(newFileEntry))
                {
                    files.Add(extractedFile);
                }
            }

            return files;

        }

        private static IEnumerable<FileEntry> ExtractXZFile(FileEntry fileEntry)
        {
            List<FileEntry> files = new List<FileEntry>();
            using var xzStream = new XZStream(fileEntry.Content);
            using var memoryStream = new MemoryStream();
            xzStream.CopyTo(memoryStream);

            var newFilename = Path.GetFileNameWithoutExtension(fileEntry.Name);
            var newFileEntry = new FileEntry(newFilename, fileEntry.FullPath, memoryStream);
            _count++;
            foreach (var extractedFile in ExtractFile(newFileEntry))
            {
                files.Add(extractedFile);
            }

            return files;

        }

        private static IEnumerable<FileEntry> ExtractBZip2File(FileEntry fileEntry)
        {
            List<FileEntry> files = new List<FileEntry>();
            using var bzip2Stream = new BZip2Stream(fileEntry.Content, SharpCompress.Compressors.CompressionMode.Decompress, false);
            using var memoryStream = new MemoryStream();
            bzip2Stream.CopyTo(memoryStream);

            var newFilename = Path.GetFileNameWithoutExtension(fileEntry.Name);
            var newFileEntry = new FileEntry(newFilename, fileEntry.FullPath, memoryStream);
            _count++;
            foreach (var extractedFile in ExtractFile(newFileEntry))
            {
                files.Add(extractedFile);
            }

            return files;
        }

        private static IEnumerable<FileEntry> ExtractRarFile(FileEntry fileEntry)
        {
            List<FileEntry> files = new List<FileEntry>();
            using var rarArchive = RarArchive.Open(fileEntry.Content);
            using var memoryStream = new MemoryStream();
            foreach (var entry in rarArchive.Entries)
            {
                if (entry.IsDirectory)
                {
                    continue;
                }
                var newFileEntry = new FileEntry(entry.Key, fileEntry.FullPath, entry.OpenEntryStream());
                _count++;
                foreach (var extractedFile in ExtractFile(newFileEntry))
                {
                    files.Add(extractedFile);
                }
            }

            return files;
        }

        private static IEnumerable<FileEntry> Extract7ZipFile(FileEntry fileEntry)
        {
            List<FileEntry> files = new List<FileEntry>();
            using var rarArchive = RarArchive.Open(fileEntry.Content);
            using var memoryStream = new MemoryStream();
            foreach (var entry in rarArchive.Entries)
            {
                if (entry.IsDirectory)
                {
                    continue;
                }
                var newFileEntry = new FileEntry(entry.Key, fileEntry.FullPath, entry.OpenEntryStream());
                _count++;
                foreach (var extractedFile in ExtractFile(newFileEntry))
                {
                    files.Add(extractedFile);
                }
            }

            return files;
        }
    }

    #endregion

}
