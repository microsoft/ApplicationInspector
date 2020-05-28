// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DiscUtils;
using DiscUtils.Btrfs;
using DiscUtils.Ext;
using DiscUtils.HfsPlus;
using DiscUtils.Fat;
using DiscUtils.Iso9660;
using DiscUtils.Ntfs;
using DiscUtils.Setup;
using DiscUtils.Streams;
using DiscUtils.Xfs;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using SharpCompress.Archives.GZip;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Xz;
using System.Collections.Concurrent;

namespace MultiExtractor
{
    public class Extractor
    {
        /// <summary>
        /// Internal buffer size for extraction
        /// </summary>
        private const int BUFFER_SIZE = 32768;

        private const string DEBUG_STRING = "Failed parsing archive of type {0} {1}:{2} ({3})";

        /// <summary>
        /// The maximum number of items to take at once in the parallel extractors
        /// </summary>
        private const int MAX_BATCH_SIZE = 50;

        /// <summary>
        /// By default, stop extracting if the total number of bytes
        /// seen is greater than this multiple of the original archive
        /// size. Used to avoid denial of service (zip bombs and the like).
        /// </summary>
        private const double DEFAULT_MAX_EXTRACTED_BYTES_RATIO = 60.0;
        
        /// <summary>
        /// By default, stop processing after this time span. Used to avoid
        /// denial of service (zip bombs and the like).
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(300);

        /// <summary>
        /// The maximum number of bytes to extract from the archive and
        /// all embedded archives. Set to 0 to remove limit. Note that
        /// MaxExpansionRatio may also apply. Defaults to 0.
        /// </summary>
        public long MaxExtractedBytes { get; set; } = 0;

        /// <summary>
        /// Backing store for MaxExtractedBytesRatio.
        /// </summary>
        private double _MaxExtractedBytesRatio;

        /// <summary>
        /// The maximum number of bytes to extract from the archive and
        /// all embedded archives, relative to the size of the initial
        /// archive. The default value of 100 means if the archive is 5k
        /// in size, stop processing after 500k bytes are extracted. Set
        /// this to 0 to mean, 'no limit'. Not that MaxExtractedBytes
        /// may also apply.
        /// </summary>
        public double MaxExtractedBytesRatio {
            get
            {
                return _MaxExtractedBytesRatio;
            }

            set
            {
                _MaxExtractedBytesRatio = Math.Max(value, 0);
            }
        }

        /// <summary>
        /// Logger for interesting events.
        /// </summary>
        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly string IS_QUINE_STRING = "Detected Quine {0} in {1}. Aborting Extraction.";

        /// <summary>
        /// Times extraction operations to avoid denial of service.
        /// </summary>
        private Stopwatch GovernorStopwatch;

        public bool EnableTiming { get; }

        /// <summary>
        /// Stores the number of bytes left before we abort (denial of service).
        /// </summary>
        private long CurrentOperationProcessedBytesLeft = -1;

        public Extractor(bool enableTiming = false)
        {   
            MaxExtractedBytesRatio = DEFAULT_MAX_EXTRACTED_BYTES_RATIO;
            GovernorStopwatch = new Stopwatch();
            EnableTiming = enableTiming;
            SetupHelper.RegisterAssembly(typeof(BtrfsFileSystem).Assembly);
            SetupHelper.RegisterAssembly(typeof(ExtFileSystem).Assembly);
            SetupHelper.RegisterAssembly(typeof(FatFileSystem).Assembly);
            SetupHelper.RegisterAssembly(typeof(HfsPlusFileSystem).Assembly);
            SetupHelper.RegisterAssembly(typeof(NtfsFileSystem).Assembly);
            SetupHelper.RegisterAssembly(typeof(XfsFileSystem).Assembly);
        }

        private void ResetResourceGovernor()
        {
            Logger.Trace("ResetResourceGovernor()");
            GovernorStopwatch.Reset();
            CurrentOperationProcessedBytesLeft = -1;
        }

        private void ResetResourceGovernor(Stream stream)
        {
            Logger.Trace("ResetResourceGovernor()");

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream), "Stream must not be null.");
            }

            GovernorStopwatch = Stopwatch.StartNew();

            // Default value is we take MaxExtractedBytes (meaning, ratio is not defined)
            CurrentOperationProcessedBytesLeft = MaxExtractedBytes;
            if (MaxExtractedBytesRatio > 0)
            {
                long streamLength;
                try
                {
                    streamLength = stream.Length;
                }
                catch (Exception)
                {
                    throw new ArgumentException("Unable to get length of stream.");
                }

                // Ratio *is* defined, so the max value would be based on the stream length
                var maxViaRatio = (long)(MaxExtractedBytesRatio * streamLength);
                // Assign the samller of the two, accounting for MaxExtractedBytes == 0 means, 'no limit'.
                CurrentOperationProcessedBytesLeft = Math.Min(maxViaRatio, MaxExtractedBytes > 0 ? MaxExtractedBytes : long.MaxValue);
            }
        }

        /// <summary>
        /// Checks to ensure we haven't extracted too many bytes, or taken too long.
        /// This exists primarily to mitigate the risks of quines (archives that 
        /// contain themselves) and zip bombs (specially constructed to expand to huge
        /// sizes).
        /// Ref: https://alf.nu/ZipQuine
        /// </summary>
        /// <param name="additionalBytes"></param>
        private void CheckResourceGovernor(long additionalBytes = 0)
        {
            Logger.ConditionalTrace("CheckResourceGovernor(duration={0}, bytes={1})", GovernorStopwatch.Elapsed.TotalMilliseconds, CurrentOperationProcessedBytesLeft);

            if (EnableTiming && GovernorStopwatch.Elapsed > Timeout)
            {
                throw new TimeoutException(string.Format($"Processing timeout exceeded: {GovernorStopwatch.Elapsed.TotalMilliseconds} ms."));
            }

            if (CurrentOperationProcessedBytesLeft - additionalBytes <= 0)
            {
                throw new OverflowException("Too many bytes extracted, exceeding limit.");
            }
        }


        /// <summary>
        /// Extracts files from the file 'filename'.
        /// </summary>
        /// <param name="fileEntry">FileEntry to extract</param>
        /// <returns>Extracted files</returns>
        public IEnumerable<FileEntry> ExtractFile(string filename, bool parallel = false)
        {
            if (!File.Exists(filename))
            {
                Logger.Warn("ExtractFile called, but {0} does not exist.", filename);
                yield break;
            }
            FileEntry? fileEntry = null;
            try
            {
                using var fs = new FileStream(filename,FileMode.Open);
                // We give it a parent so we can give it a shortname. This is useful for Quine detection later.
                fileEntry = new FileEntry(Path.GetFileName(filename), fs, new FileEntry(filename, new MemoryStream()));
                ResetResourceGovernor(fs);
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to extract file {0}", filename);
            }

            if (fileEntry != null)
            {
                foreach (var result in ExtractFile(fileEntry, parallel))
                {
                    GovernorStopwatch.Stop();
                    yield return result;
                    GovernorStopwatch.Start();
                }
            }
            GovernorStopwatch.Stop();
        }

        /// <summary>
        /// Extracts files from the file, identified by 'filename', but with 
        /// contents passed through 'archiveBytes'. Note that 'filename' does not
        /// have to exist; it will only be used to identify files extracted.
        /// </summary>
        /// <param name="fileEntry">FileEntry to extract</param>
        /// <returns>Extracted files</returns>
        public IEnumerable<FileEntry> ExtractFile(string filename, byte[] archiveBytes, bool parallel = false)
        {
            using var ms = new MemoryStream(archiveBytes);
            ResetResourceGovernor(ms);
            var result = ExtractFile(new FileEntry(filename, ms),parallel);
            return result;
        }

        /// <summary>
        /// Extracts files from the given FileEntry, using the appropriate
        /// extractors, recursively.
        /// </summary>
        /// <param name="fileEntry">FileEntry to extract</param>
        /// <returns>Extracted files</returns>
        private IEnumerable<FileEntry> ExtractFile(FileEntry fileEntry, bool parallel = false)
        {
            Logger.Trace("ExtractFile({0})", fileEntry.FullPath);
            CurrentOperationProcessedBytesLeft -= fileEntry.Content.Length;
            CheckResourceGovernor();
            IEnumerable<FileEntry> result;
            bool useRaw = false;

            try
            {
                var fileEntryType = MiniMagic.DetectFileType(fileEntry);
                switch (fileEntryType)
                {
                    case ArchiveFileType.ZIP:
                        result = ExtractZipFile(fileEntry, parallel);
                        break;
                    case ArchiveFileType.RAR:
                        result = ExtractRarFile(fileEntry, parallel);
                        break;
                    case ArchiveFileType.P7ZIP:
                        result = Extract7ZipFile(fileEntry, parallel);
                        break;
                    case ArchiveFileType.DEB:
                        result = ExtractDebFile(fileEntry, parallel);
                        break;
                    case ArchiveFileType.GZIP:
                        result = ExtractGZipFile(fileEntry, parallel);
                        break;
                    case ArchiveFileType.TAR:
                        result = ExtractTarFile(fileEntry, parallel);
                        break;
                    case ArchiveFileType.XZ:
                        result = ExtractXZFile(fileEntry, parallel);
                        break;
                    case ArchiveFileType.BZIP2:
                        result = ExtractBZip2File(fileEntry, parallel);
                        break;
                    case ArchiveFileType.AR:
                        result = ExtractGnuArFile(fileEntry, parallel);
                        break;
                    case ArchiveFileType.ISO_9660:
                        result = ExtractIsoFile(fileEntry, parallel);
                        break;
                    case ArchiveFileType.VHDX:
                        result = ExtractVHDXFile(fileEntry, parallel);
                        break;
                    case ArchiveFileType.VHD:
                        result = ExtractVHDFile(fileEntry, parallel);
                        break;
                    case ArchiveFileType.WIM:
                        result = ExtractWimFile(fileEntry, parallel);
                        break;
                    case ArchiveFileType.VMDK:
                        result = ExtractVMDKFile(fileEntry, parallel);
                        break;
                    default:
                        useRaw = true;
                        result = new[] { fileEntry };
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Error extracting {0}: {1}", fileEntry.FullPath, ex.Message);
                useRaw = true;
                result = new[] { fileEntry };   // Default is to not try to extract.
            }

            // After we are done with an archive subtract its bytes. Contents have been counted now separately
            if (!useRaw)
            {
                CurrentOperationProcessedBytesLeft += fileEntry.Content.Length;
            }

            return result;
        }

        /// <summary>
        /// Extracts an a Wim file
        /// </summary>
        /// <param name="fileEntry"></param>
        /// <returns></returns>
        private IEnumerable<FileEntry> ExtractWimFile(FileEntry fileEntry, bool parallel)
        {
            DiscUtils.Wim.WimFile? baseFile = null;
            try
            {
                baseFile = new DiscUtils.Wim.WimFile(fileEntry.Content);
            }
            catch(Exception e)
            {
                Logger.Debug(e, "Failed to init WIM image.");
            }
            if (baseFile != null)
            {
                if (parallel)
                {
                    ConcurrentStack<FileEntry> files = new ConcurrentStack<FileEntry>();

                    for (int i = 0; i < baseFile.ImageCount; i++)
                    {
                        var image = baseFile.GetImage(i);
                        var fileList = image.GetFiles(image.Root.FullName, "*.*", SearchOption.AllDirectories).ToList();
                        while (fileList.Any())
                        {
                            int batchSize = Math.Min(MAX_BATCH_SIZE, fileList.Count);
                            var range = fileList.Take(batchSize);
                            var streamsAndNames = new List<(DiscFileInfo, Stream)>();
                            foreach (var file in range)
                            {
                                try
                                {
                                    var info = image.GetFileInfo(file);
                                    streamsAndNames.Add((info, info.OpenRead()));
                                }
                                catch (Exception e)
                                {
                                    Logger.Debug("Error reading {0} from WIM {1} ({2}:{3})", file, image.FriendlyName, e.GetType(), e.Message);
                                }
                            }
                            CheckResourceGovernor(streamsAndNames.Sum(x => x.Item1.Length));
                            streamsAndNames.AsParallel().ForAll(file =>
                            {
                                var newFileEntry = new FileEntry($"{image.FriendlyName}\\{file.Item1.FullName}", file.Item2, fileEntry);
                                var entries = ExtractFile(newFileEntry, true);
                                files.PushRange(entries.ToArray());
                            });
                            fileList.RemoveRange(0, batchSize);

                            while (files.TryPop(out FileEntry? result))
                            {
                                if (result != null)
                                    yield return result;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < baseFile.ImageCount; i++)
                    {
                        var image = baseFile.GetImage(i);
                        var files = image.GetFiles(image.Root.FullName, "*.*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            Stream? stream = null;
                            try
                            {
                                var info = image.GetFileInfo(file);
                                CheckResourceGovernor(info.Length);
                                stream = info.OpenRead();
                            }
                            catch (Exception e)
                            {
                                Logger.Debug("Error reading {0} from WIM {1} ({2}:{3})", file, image.FriendlyName, e.GetType(), e.Message);
                            }
                            if (stream != null)
                            {
                                var newFileEntry = new FileEntry($"{image.FriendlyName}\\{file}", stream, fileEntry);
                                var entries = ExtractFile(newFileEntry, parallel);
                                foreach (var entry in entries)
                                {
                                    yield return entry;
                                }
                                stream.Dispose();
                            }
                        }
                    }
                }
            }
            else
            {
                yield return fileEntry;
            }
        }

        /// <summary>
        /// Extracts an a VMDK file
        /// </summary>
        /// <param name="fileEntry"></param>
        /// <returns></returns>
        private IEnumerable<FileEntry> ExtractVMDKFile(FileEntry fileEntry, bool parallel)
        {
            using var disk = new DiscUtils.Vmdk.Disk(fileEntry.Content, Ownership.None);
            LogicalVolumeInfo[]? logicalVolumes = null;

            try
            {
                var manager = new VolumeManager(disk);
                logicalVolumes = manager.GetLogicalVolumes();
            }
            catch (Exception e)
            {
                Logger.Debug("Error reading {0} disk at {1} ({2}:{3})", disk.GetType(), fileEntry.FullPath, e.GetType(), e.Message);
            }

            if (logicalVolumes != null)
            {
                foreach (var volume in logicalVolumes)
                {
                    foreach (var entry in DumpLogicalVolume(volume, fileEntry.FullPath, parallel, fileEntry))
                    {
                        yield return entry;
                    }
                }
            }
            else
            {
                yield return fileEntry;
            }
        }

        /// <summary>
        /// Extracts an a VHDX file
        /// </summary>
        /// <param name="fileEntry"></param>
        /// <returns></returns>
        private IEnumerable<FileEntry> ExtractVHDXFile(FileEntry fileEntry, bool parallel)
        {
            using var disk = new DiscUtils.Vhdx.Disk(fileEntry.Content, Ownership.None);
            LogicalVolumeInfo[]? logicalVolumes = null;

            try
            {
                var manager = new VolumeManager(disk);
                logicalVolumes = manager.GetLogicalVolumes();
            }
            catch (Exception e)
            {
                Logger.Debug("Error reading {0} disk at {1} ({2}:{3})", disk.GetType(), fileEntry.FullPath, e.GetType(), e.Message);
            }

            if (logicalVolumes != null)
            {
                foreach (var volume in logicalVolumes)
                {
                    var fsInfos = FileSystemManager.DetectFileSystems(volume);

                    foreach (var entry in DumpLogicalVolume(volume, fileEntry.FullPath, parallel, fileEntry))
                    {
                        yield return entry;
                    }
                }
            }
            else
            {
                yield return fileEntry;
            }
        }

        /// <summary>
        /// Extracts an a VHD file
        /// </summary>
        /// <param name="fileEntry"></param>
        /// <returns></returns>
        private IEnumerable<FileEntry> ExtractVHDFile(FileEntry fileEntry, bool parallel)
        {
            using var disk = new DiscUtils.Vhd.Disk(fileEntry.Content, Ownership.None);
            LogicalVolumeInfo[]? logicalVolumes = null;

            try
            {
                var manager = new VolumeManager(disk);
                logicalVolumes = manager.GetLogicalVolumes();
            }
            catch (Exception e)
            {
                Logger.Debug("Error reading {0} disk at {1} ({2}:{3})", disk.GetType(), fileEntry.FullPath, e.GetType(), e.Message);
            }

            if (logicalVolumes != null)
            {
                foreach (var volume in logicalVolumes)
                {
                    foreach (var entry in DumpLogicalVolume(volume, fileEntry.FullPath, parallel, fileEntry))
                    {
                        yield return entry;
                    }
                }
            }
            else
            {
                yield return fileEntry;
            }
        }

        /// <summary>
        /// Extracts an an ISO file
        /// </summary>
        /// <param name="fileEntry"></param>
        /// <returns></returns>
        private IEnumerable<FileEntry> ExtractIsoFile(FileEntry fileEntry, bool parallel)
        {
            using CDReader cd = new CDReader(fileEntry.Content, true);
            var entries = cd.GetFiles(cd.Root.FullName, "*.*", SearchOption.AllDirectories);
            if (entries != null)
            {
                if (parallel)
                {
                    ConcurrentStack<FileEntry> files = new ConcurrentStack<FileEntry>();

                    int batchSize = Math.Min(MAX_BATCH_SIZE, entries.Length);
                    var selectedFileNames = entries[0..batchSize];
                    var fileInfoTuples = new List<(DiscFileInfo, Stream)>();

                    foreach (var selectedFileName in selectedFileNames)
                    {
                        try
                        {
                            var fileInfo = cd.GetFileInfo(selectedFileName);
                            var stream = fileInfo.OpenRead();
                            fileInfoTuples.Add((fileInfo, stream));
                        }
                        catch (Exception e)
                        {
                            Logger.Debug("Failed to get FileInfo or OpenStream from {0} in ISO {1} ({2}:{3})", selectedFileName, fileEntry.FullPath, e.GetType(), e.Message);
                        }
                    }
                    CheckResourceGovernor(fileInfoTuples.Sum(x => x.Item1.Length));

                    fileInfoTuples.AsParallel().ForAll(cdFile =>
                    {
                        var newFileEntry = new FileEntry(cdFile.Item1.Name, cdFile.Item2, fileEntry);
                        var entries = ExtractFile(newFileEntry, true);
                        files.PushRange(entries.ToArray());
                    });

                    entries = entries[batchSize..];

                    while (files.TryPop(out FileEntry? result))
                    {
                        if (result != null)
                            yield return result;
                    }
                }
                else
                {
                    foreach (var file in entries)
                    {
                        var fileInfo = cd.GetFileInfo(file);
                        CheckResourceGovernor(fileInfo.Length);
                        Stream? stream = null;
                        try
                        {
                            stream = fileInfo.OpenRead();
                        }
                        catch (Exception e)
                        {
                            Logger.Debug("Failed to extract {0} from ISO {1}. ({2}:{3})", fileInfo.Name, fileEntry.FullPath, e.GetType(), e.Message);
                        }
                        if (stream != null)
                        {
                            var newFileEntry = new FileEntry(fileInfo.Name, stream, fileEntry);
                            var innerEntries = ExtractFile(newFileEntry, parallel);
                            foreach (var entry in innerEntries)
                            {
                                yield return entry;
                            }
                        }
                    }
                }
            }
            else
            {
                yield return fileEntry;
            }
        }

        /// <summary>
        /// Extracts an archive file created with GNU ar
        /// </summary>
        /// <param name="fileEntry"></param>
        /// <returns></returns>
        private IEnumerable<FileEntry> ExtractGnuArFile(FileEntry fileEntry, bool parallel)
        {
            IEnumerable<FileEntry>? fileEntries = null;
            try
            {
                fileEntries = ArFile.GetFileEntries(fileEntry);
            }
            catch (Exception e)
            {
                Logger.Debug(DEBUG_STRING, ArchiveFileType.AR, fileEntry.FullPath, string.Empty, e.GetType());
            }
            if (fileEntries != null)
            {
                if (parallel)
                {
                    var tempStore = new ConcurrentStack<FileEntry>();
                    var selectedEntries = fileEntries.Take(MAX_BATCH_SIZE);
                    CheckResourceGovernor(selectedEntries.Sum(x => x.Content.Length));
                    selectedEntries.AsParallel().ForAll(arEntry =>
                    {
                        tempStore.PushRange(ExtractFile(arEntry, parallel).ToArray());
                    });

                    fileEntries = fileEntries.Skip(selectedEntries.Count());

                    while (tempStore.TryPop(out FileEntry? result))
                    {
                        if (result != null)
                            yield return result;
                    }
                }
                else
                {
                    foreach (var entry in fileEntries)
                    {
                        CheckResourceGovernor(entry.Content.Length);
                        foreach (var extractedFile in ExtractFile(entry, parallel))
                        {
                            yield return extractedFile;
                        }
                    }
                }
            }
            else
            {
                yield return fileEntry;
            }
        }

        /// <summary>
        /// Extracts an zip file contained in fileEntry.
        /// </summary>
        /// <param name="fileEntry">FileEntry to extract</param>
        /// <returns>Extracted files</returns>
        private IEnumerable<FileEntry> ExtractZipFile(FileEntry fileEntry, bool parallel)
        {
            ZipFile? zipFile = null;
            try
            {
                zipFile = new ZipFile(fileEntry.Content);
            }
            catch(Exception e)
            {
                Logger.Debug(DEBUG_STRING, ArchiveFileType.ZIP, fileEntry.FullPath, string.Empty, e.GetType());
            }
            if (zipFile != null)
            {
                if (parallel)
                {
                    ConcurrentStack<FileEntry> files = new ConcurrentStack<FileEntry>();

                    var zipEntries = new List<ZipEntry>();
                    foreach (ZipEntry? zipEntry in zipFile)
                    {
                        if (zipEntry is null ||
                            zipEntry.IsDirectory ||
                            zipEntry.IsCrypted ||
                            !zipEntry.CanDecompress)
                        {
                            continue;
                        }
                        zipEntries.Add(zipEntry);
                    }

                    while (zipEntries.Count > 0)
                    {
                        int batchSize = Math.Min(MAX_BATCH_SIZE, zipEntries.Count);
                        var selectedEntries = zipEntries.GetRange(0, batchSize);
                        CheckResourceGovernor(selectedEntries.Sum(x => x.Size));
                        try
                        {
                            selectedEntries.AsParallel().ForAll(zipEntry =>
                            {
                                try
                                {
                                    var zipStream = zipFile.GetInputStream(zipEntry);
                                    var newFileEntry = new FileEntry(zipEntry.Name, zipStream, fileEntry);
                                    if (IsQuine(newFileEntry))
                                    {
                                        Logger.Info(IS_QUINE_STRING, fileEntry.Name, fileEntry.FullPath);
                                        CurrentOperationProcessedBytesLeft = -1;
                                    }
                                    else
                                    {
                                        files.PushRange(ExtractFile(newFileEntry, true).ToArray());
                                    }
                                }
                                catch (Exception e) when (e is OverflowException)
                                {
                                    Logger.Debug(DEBUG_STRING, ArchiveFileType.ZIP, fileEntry.FullPath, zipEntry.Name, e.GetType());
                                    throw;
                                }
                                catch (Exception e)
                                {
                                    Logger.Debug(DEBUG_STRING, ArchiveFileType.ZIP, fileEntry.FullPath, zipEntry.Name, e.GetType());
                                }
                            });
                        }
                        catch (Exception e) when (e is AggregateException)
                        {
                            if (e.InnerException?.GetType() == typeof(OverflowException))
                            {
                                throw e.InnerException;
                            }
                            throw;
                        }

                        CheckResourceGovernor(0);
                        zipEntries.RemoveRange(0, batchSize);

                        while (files.TryPop(out FileEntry? result))
                        {
                            if (result != null)
                                yield return result;
                        }
                    }
                }
                else
                {
                    foreach (ZipEntry? zipEntry in zipFile)
                    {
                        if (zipEntry is null ||
                            zipEntry.IsDirectory ||
                            zipEntry.IsCrypted ||
                            !zipEntry.CanDecompress)
                        {
                            continue;
                        }

                        CheckResourceGovernor(zipEntry.Size);

                        using var fs = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
                        try
                        {
                            byte[] buffer = new byte[BUFFER_SIZE];
                            var zipStream = zipFile.GetInputStream(zipEntry);
                            StreamUtils.Copy(zipStream, fs, buffer);
                        }
                        catch (Exception e)
                        {
                            Logger.Debug(DEBUG_STRING, ArchiveFileType.ZIP, fileEntry.FullPath, zipEntry.Name, e.GetType());
                        }

                        var newFileEntry = new FileEntry(zipEntry.Name, fs, fileEntry);

                        if (IsQuine(newFileEntry))
                        {
                            Logger.Info(IS_QUINE_STRING, fileEntry.Name, fileEntry.FullPath);
                            throw new OverflowException();
                        }

                        foreach (var extractedFile in ExtractFile(newFileEntry, parallel))
                        {
                            yield return extractedFile;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extracts an Gzip file contained in fileEntry.
        /// Since this function is recursive, even though Gzip only supports a single
        /// compressed file, that inner file could itself contain multiple others.
        /// </summary>
        /// <param name="fileEntry">FileEntry to extract</param>
        /// <returns>Extracted files</returns>
        private IEnumerable<FileEntry> ExtractGZipFile(FileEntry fileEntry, bool parallel)
        {
            GZipArchive? gzipArchive = null;
            try
            {
                gzipArchive = GZipArchive.Open(fileEntry.Content);
            }
            catch (Exception e)
            {
                Logger.Debug(DEBUG_STRING, ArchiveFileType.GZIP, fileEntry.FullPath, string.Empty, e.GetType());
            }
            if (gzipArchive != null)
            {
                foreach (var entry in gzipArchive.Entries)
                {
                    if (entry.IsDirectory)
                    {
                        continue;
                    }
                    CheckResourceGovernor(entry.Size);

                    var newFilename = Path.GetFileNameWithoutExtension(fileEntry.Name);
                    if (fileEntry.Name.EndsWith(".tgz", StringComparison.InvariantCultureIgnoreCase))
                    {
                        newFilename = newFilename[0..^4] + ".tar";
                    }

                    FileEntry? newFileEntry = null;
                    try
                    {
                        using var stream = entry.OpenEntryStream();
                        newFileEntry = new FileEntry(newFilename, stream, fileEntry);
                    }
                    catch (Exception e)
                    {
                        Logger.Debug(DEBUG_STRING, ArchiveFileType.GZIP, fileEntry.FullPath, newFilename, e.GetType());
                    }
                    if (newFileEntry != null)
                    {
                        foreach (var extractedFile in ExtractFile(newFileEntry, parallel))
                        {
                            yield return extractedFile;
                        }
                    }
                }
            }
            gzipArchive?.Dispose();
        }

        /// <summary>
        /// Extracts a tar file contained in fileEntry.
        /// </summary>
        /// <param name="fileEntry">FileEntry to extract</param>
        /// <returns>Extracted files</returns>
        private IEnumerable<FileEntry> ExtractTarFile(FileEntry fileEntry, bool parallel)
        {
            TarEntry tarEntry;
            TarInputStream? tarStream = null;
            try
            {
                tarStream = new TarInputStream(fileEntry.Content);
            }
            catch(Exception e)
            {
                Logger.Debug(DEBUG_STRING, ArchiveFileType.TAR, fileEntry.FullPath, string.Empty, e.GetType());
            }
            if (tarStream != null)
            {
                while ((tarEntry = tarStream.GetNextEntry()) != null)
                {
                    if (tarEntry.IsDirectory)
                    {
                        continue;
                    }
                    var fs = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
                    CheckResourceGovernor(tarStream.Length);
                    try
                    {
                        tarStream.CopyEntryContents(fs);
                    }
                    catch (Exception e)
                    {
                        Logger.Debug(DEBUG_STRING, ArchiveFileType.TAR, fileEntry.FullPath, tarEntry.Name, e.GetType());
                    }

                    var newFileEntry = new FileEntry(tarEntry.Name, fs, fileEntry, true);
                    
                    if (IsQuine(newFileEntry))
                    {
                        Logger.Info(IS_QUINE_STRING, fileEntry.Name, fileEntry.FullPath);
                        throw new OverflowException();
                    }

                    foreach (var extractedFile in ExtractFile(newFileEntry, parallel))
                    {
                        yield return extractedFile;
                    }
                }
                tarStream.Dispose();
            }
            else
            {
                // If we couldn't parse it just return it
                yield return fileEntry;
            }
        }

        /// <summary>
        /// Extracts an .XZ file contained in fileEntry.
        /// </summary>
        /// <param name="fileEntry">FileEntry to extract</param>
        /// <returns>Extracted files</returns>
        private IEnumerable<FileEntry> ExtractXZFile(FileEntry fileEntry, bool parallel)
        {
            XZStream? xzStream = null;
            try
            {
                xzStream = new XZStream(fileEntry.Content);
            }
            catch(Exception e)
            {
                Logger.Debug(DEBUG_STRING, ArchiveFileType.XZ, fileEntry.FullPath, string.Empty, e.GetType());
            }
            if (xzStream != null)
            {
                var newFilename = Path.GetFileNameWithoutExtension(fileEntry.Name);
                var newFileEntry = new FileEntry(newFilename, xzStream, fileEntry);

                // SharpCompress does not expose metadata without a full read,
                // so we need to decompress first, and then abort if the bytes
                // exceeded the governor's capacity.

                var streamLength = xzStream.Index.Records?.Select(r => r.UncompressedSize)
                                          .Aggregate((ulong?)0, (a, b) => a + b);

                // BUG: Technically, we're casting a ulong to a long, but we don't expect
                // 9 exabyte steams, so low risk.
                if (streamLength.HasValue)
                {
                    CheckResourceGovernor((long)streamLength.Value);
                }

                if (IsQuine(newFileEntry))
                {
                    Logger.Info(IS_QUINE_STRING, fileEntry.Name, fileEntry.FullPath);
                    throw new OverflowException();
                }

                foreach (var extractedFile in ExtractFile(newFileEntry, parallel))
                {
                    yield return extractedFile;
                }
            }
            else
            {
                yield return fileEntry;
            }
            xzStream?.Dispose();
        }

        /// <summary>
        /// Extracts an BZip2 file contained in fileEntry.
        /// </summary>
        /// <param name="fileEntry">FileEntry to extract</param>
        /// <returns>Extracted files</returns>
        private IEnumerable<FileEntry> ExtractBZip2File(FileEntry fileEntry, bool parallel)
        {
            BZip2Stream? bzip2Stream = null;
            try
            {
                bzip2Stream = new BZip2Stream(fileEntry.Content, SharpCompress.Compressors.CompressionMode.Decompress, false);
                CheckResourceGovernor(bzip2Stream.Length);
            }
            catch (Exception e)
            {
                Logger.Debug(DEBUG_STRING, ArchiveFileType.BZIP2, fileEntry.FullPath, string.Empty, e.GetType());
            }
            if (bzip2Stream != null)
            {
                var newFilename = Path.GetFileNameWithoutExtension(fileEntry.Name);
                var newFileEntry = new FileEntry(newFilename, bzip2Stream, fileEntry);

                if (IsQuine(newFileEntry))
                {
                    Logger.Info(IS_QUINE_STRING, fileEntry.Name, fileEntry.FullPath);
                    bzip2Stream.Dispose();
                    throw new OverflowException();
                }

                foreach (var extractedFile in ExtractFile(newFileEntry, parallel))
                {
                    yield return extractedFile;
                }
                bzip2Stream.Dispose();
            }
            else
            {
                yield return fileEntry;
            }
        }

        /// <summary>
        /// Extracts a RAR file contained in fileEntry.
        /// </summary>
        /// <param name="fileEntry">FileEntry to extract</param>
        /// <returns>Extracted files</returns>
        private IEnumerable<FileEntry> ExtractRarFile(FileEntry fileEntry, bool parallel)
        {
            RarArchive? rarArchive = null;
            try
            {
                rarArchive = RarArchive.Open(fileEntry.Content);
            }
            catch (Exception e)
            {
                Logger.Debug(DEBUG_STRING, ArchiveFileType.RAR, fileEntry.FullPath, string.Empty, e.GetType());
            }

            if (rarArchive != null)
            {
                var entries = rarArchive.Entries.Where(x => x.IsComplete && !x.IsDirectory && !x.IsEncrypted);
                if (parallel)
                {
                    ConcurrentStack<FileEntry> files = new ConcurrentStack<FileEntry>();

                    while (entries.Any())
                    {
                        int batchSize = Math.Min(MAX_BATCH_SIZE, entries.Count());

                        var streams = entries.Take(batchSize).Select(entry => (entry, entry.OpenEntryStream())).ToList();

                        CheckResourceGovernor(streams.Sum(x => x.Item2.Length));

                        streams.AsParallel().ForAll(streampair =>
                        {
                            try
                            {
                                var newFileEntry = new FileEntry(streampair.entry.Key, streampair.Item2, fileEntry);
                                if (IsQuine(newFileEntry))
                                {
                                    Logger.Info(IS_QUINE_STRING, fileEntry.Name, fileEntry.FullPath);
                                    CurrentOperationProcessedBytesLeft = -1;
                                }
                                else
                                {
                                    files.PushRange(ExtractFile(newFileEntry, true).ToArray());
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Debug(DEBUG_STRING, ArchiveFileType.RAR, fileEntry.FullPath, streampair.entry.Key, e.GetType());
                            }
                        });
                        CheckResourceGovernor(0);

                        entries = entries.Skip(streams.Count);

                        while (files.TryPop(out FileEntry? result))
                        {
                            if (result != null)
                                yield return result;
                        }
                    }
                }
                else
                {
                    foreach (var entry in entries)
                    {
                        CheckResourceGovernor(entry.Size);
                        FileEntry? newFileEntry = null;
                        try
                        {
                            newFileEntry = new FileEntry(entry.Key, entry.OpenEntryStream(), fileEntry);
                        }
                        catch (Exception e)
                        {
                            Logger.Debug(DEBUG_STRING, ArchiveFileType.RAR, fileEntry.FullPath, entry.Key, e.GetType());
                        }
                        if (newFileEntry != null)
                        {
                            if (IsQuine(newFileEntry))
                            {
                                Logger.Info(IS_QUINE_STRING, fileEntry.Name, fileEntry.FullPath);
                                throw new OverflowException();
                            }
                            foreach (var extractedFile in ExtractFile(newFileEntry, parallel))
                            {
                                yield return extractedFile;
                            }
                        }
                    }
                }
            }
            else
            {
                yield return fileEntry;
            }
        }

        /// <summary>
        /// Extracts a 7-Zip file contained in fileEntry.
        /// </summary>
        /// <param name="fileEntry">FileEntry to extract</param>
        /// <returns>Extracted files</returns>
        private IEnumerable<FileEntry> Extract7ZipFile(FileEntry fileEntry, bool parallel)
        {
            SevenZipArchive? sevenZipArchive = null;
            try
            {
                sevenZipArchive = SevenZipArchive.Open(fileEntry.Content);
            }
            catch (Exception e)
            {
                Logger.Debug(DEBUG_STRING, ArchiveFileType.P7ZIP, fileEntry.FullPath, string.Empty, e.GetType());
            }
            if (sevenZipArchive != null)
            {
                var entries = sevenZipArchive.Entries.Where(x => !x.IsDirectory && !x.IsEncrypted && x.IsComplete).ToList();

                if (parallel)
                {
                    ConcurrentStack<FileEntry> files = new ConcurrentStack<FileEntry>();

                    while (entries.Count() > 0)
                    {
                        int batchSize = Math.Min(MAX_BATCH_SIZE, entries.Count());
                        var selectedEntries = entries.GetRange(0, batchSize).Select(entry => (entry, entry.OpenEntryStream()));
                        CheckResourceGovernor(selectedEntries.Sum(x => x.entry.Size));

                        try
                        {
                            selectedEntries.AsParallel().ForAll(entry =>
                            {
                                try
                                {
                                    var newFileEntry = new FileEntry(entry.entry.Key, entry.Item2, fileEntry);
                                    if (IsQuine(newFileEntry))
                                    {
                                        Logger.Info(IS_QUINE_STRING, fileEntry.Name, fileEntry.FullPath);
                                        CurrentOperationProcessedBytesLeft = -1;
                                    }
                                    else
                                    {
                                        files.PushRange(ExtractFile(newFileEntry, true).ToArray());
                                    }
                                }
                                catch (Exception e) when (e is OverflowException)
                                {
                                    Logger.Debug(DEBUG_STRING, ArchiveFileType.P7ZIP, fileEntry.FullPath, entry.entry.Key, e.GetType());
                                    throw;
                                }
                                catch (Exception e)
                                {
                                    Logger.Debug(DEBUG_STRING, ArchiveFileType.P7ZIP, fileEntry.FullPath, entry.entry.Key, e.GetType());
                                }
                            });
                        }
                        catch (Exception e) when (e is AggregateException)
                        {
                            if (e.InnerException?.GetType() == typeof(OverflowException))
                            {
                                throw e.InnerException;
                            }
                            throw;
                        }

                        CheckResourceGovernor(0);
                        entries.RemoveRange(0, batchSize);

                        while (files.TryPop(out FileEntry? result))
                        {
                            if (result != null)
                                yield return result;
                        }
                    }
                }
                else
                {
                    foreach (var entry in entries)
                    {
                        CheckResourceGovernor(entry.Size);
                        FileEntry newFileEntry = new FileEntry(entry.Key, entry.OpenEntryStream(), fileEntry);
                        
                        if (IsQuine(newFileEntry))
                        {
                            Logger.Info(IS_QUINE_STRING, fileEntry.Name, fileEntry.FullPath);
                            throw new OverflowException();
                        }
                        foreach (var extractedFile in ExtractFile(newFileEntry, parallel))
                        {
                            yield return extractedFile;
                        }
                    }
                }
            }
            else
            {
                yield return fileEntry;
            }
        }

        /// <summary>
        /// Extracts a .deb file contained in fileEntry.
        /// </summary>
        /// <param name="fileEntry">FileEntry to extract</param>
        /// <returns>Extracted files</returns>
        private IEnumerable<FileEntry> ExtractDebFile(FileEntry fileEntry, bool parallel)
        {
            IEnumerable<FileEntry>? entries = null;
            try
            {
                entries = DebArchiveFile.GetFileEntries(fileEntry).Where(x => x.Name != "control.tar.xz");
            }
            catch (Exception e)
            {
                Logger.Debug(DEBUG_STRING, ArchiveFileType.DEB, fileEntry.FullPath, string.Empty, e.GetType());
            }
            if (entries != null)
            {
                if (parallel)
                {
                    ConcurrentStack<FileEntry> files = new ConcurrentStack<FileEntry>();

                    while (entries.Any())
                    {
                        int batchSize = Math.Min(MAX_BATCH_SIZE, entries.Count());
                        var selectedEntries = entries.Take(batchSize);

                        CheckResourceGovernor(selectedEntries.Sum(x => x.Content.Length));

                        selectedEntries.AsParallel().ForAll(entry =>
                        {
                            files.PushRange(ExtractFile(entry, parallel).ToArray());
                        });

                        entries = entries.Skip(batchSize);

                        while (files.TryPop(out FileEntry? result))
                        {
                            if (result != null)
                                yield return result;
                        }
                    }
                }
                else
                {
                    foreach (var entry in entries)
                    {
                        CheckResourceGovernor(entry.Content.Length);
                        foreach (var extractedFile in ExtractFile(entry, parallel))
                        {
                            yield return extractedFile;
                        }
                    }
                }
            }
            else
            {
                yield return fileEntry;
            }
        }

        /// </summary>
        /// <param name="volume"></param>
        /// <param name="parentPath"></param>
        /// <param name="parallel"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private IEnumerable<FileEntry> DumpLogicalVolume(LogicalVolumeInfo volume, string parentPath, bool parallel, FileEntry? parent = null)
        {
            DiscUtils.FileSystemInfo[]? fsInfos = null;
            try
            {
                fsInfos = FileSystemManager.DetectFileSystems(volume);
            }
            catch(Exception e)
            {
                Logger.Debug("Failed to get file systems from logical volume {0} Image {1} ({2}:{3})", volume.Identity, parentPath, e.GetType(), e.Message);
            }

            foreach (var fsInfo in fsInfos ?? Array.Empty<DiscUtils.FileSystemInfo>())
            {
                using var fs = fsInfo.Open(volume);
                var diskFiles = fs.GetFiles(fs.Root.FullName, "*.*", SearchOption.AllDirectories).ToList();
                if (parallel)
                {
                    ConcurrentStack<FileEntry> files = new ConcurrentStack<FileEntry>();

                    while (diskFiles.Any())
                    {
                        int batchSize = Math.Min(MAX_BATCH_SIZE, diskFiles.Count);
                        var range = diskFiles.GetRange(0, batchSize);
                        var fileinfos = new List<(DiscFileInfo, Stream)>();
                        long totalLength = 0;
                        foreach (var r in range)
                        {
                            try
                            {
                                var fi = fs.GetFileInfo(r);
                                totalLength += fi.Length;
                                fileinfos.Add((fi, fi.OpenRead()));
                            }
                            catch (Exception e)
                            {
                                Logger.Debug("Failed to get FileInfo from {0} in Volume {1} @ {2} ({3}:{4})", r, volume.Identity, parentPath, e.GetType(), e.Message);
                            }
                        }

                        CheckResourceGovernor(totalLength);

                        fileinfos.AsParallel().ForAll(file =>
                        {
                            if (file.Item2 != null)
                            {
                                var newFileEntry = new FileEntry($"{volume.Identity}\\{file.Item1.FullName}", file.Item2, parent);
                                var entries = ExtractFile(newFileEntry, true);
                                files.PushRange(entries.ToArray());
                            }
                        });
                        diskFiles.RemoveRange(0, batchSize);

                        while (files.TryPop(out FileEntry? result))
                        {
                            if (result != null)
                                yield return result;
                        }
                    }
                }
                else
                {
                    foreach (var file in diskFiles)
                    {
                        Stream? fileStream = null;
                        try
                        {
                            var fi = fs.GetFileInfo(file);
                            CheckResourceGovernor(fi.Length);
                            fileStream = fi.OpenRead();
                        }
                        catch (Exception e)
                        {
                            Logger.Debug(e, "Failed to open {0} in volume {1}", file, volume.Identity);
                        }
                        if (fileStream != null)
                        {
                            var newFileEntry = new FileEntry($"{volume.Identity}\\{file}", fileStream, parent);
                            var entries = ExtractFile(newFileEntry, parallel);
                            foreach (var entry in entries)
                            {
                                yield return entry;
                            }
                        }
                    }
                }
            }
            
        }

        /// <summary>
        /// Check if the fileEntry is a quine
        /// </summary>
        /// <param name="fileEntry"></param>
        /// <returns></returns>
        public static bool IsQuine(FileEntry fileEntry)
        {
            var next = fileEntry.Parent;
            var current = fileEntry;

            while(next != null)
            {
                if (AreIdentical(current, next))
                {
                    return true;
                }
                current = next;
                next = next.Parent;
            }
            
            return false;
        }

        /// <summary>
        /// Check if the two files are identical (i.e. Extraction is a quine)
        /// </summary>
        /// <param name="fileEntry1"></param>
        /// <param name="fileEntry2"></param>
        /// <returns></returns>
        public static bool AreIdentical(FileEntry fileEntry1, FileEntry fileEntry2)
        {
            var stream1 = fileEntry1.Content;
            var stream2 = fileEntry2.Content;
            lock (stream1)
            {
                lock (stream2)
                {
                    if (stream1.CanRead && stream2.CanRead && stream1.Length == stream2.Length && fileEntry1.Name == fileEntry2.Name)
                    {
                        Span<byte> buffer1 = stackalloc byte[1024];
                        Span<byte> buffer2 = stackalloc byte[1024];
                
                        var position1 = fileEntry1.Content.Position;
                        var position2 = fileEntry2.Content.Position;
                        stream1.Position = 0;
                        stream2.Position = 0;
                        var bytesRemaining = stream2.Length;
                        while (bytesRemaining > 0)
                        {
                            stream1.Read(buffer1);
                            stream2.Read(buffer2);
                            if (!buffer1.SequenceEqual(buffer2))
                            {
                                stream1.Position = position1;
                                stream2.Position = position2;
                                return false;
                            }
                            bytesRemaining = stream2.Length - stream2.Position;
                        }
                        stream1.Position = position1;
                        stream2.Position = position2;
                        return true;
                    }
                
                    return false;
                }
            }
        }
    }
}
