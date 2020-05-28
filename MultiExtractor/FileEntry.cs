// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using NLog.Fluent;
using System;
using System.IO;

namespace MultiExtractor
{
    public class FileEntry
    {
        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Constructs a FileEntry object from a Stream.  
        /// If passthroughStream is set to true, and the stream is seekable, it will directly use inputStream.
        /// If passthroughStream is false or it is not seekable, it will copy the full contents of inputStream 
        ///   to a new internal FileStream and attempt to reset the position of inputstream.
        /// The finalizer for this class Disposes the contained Stream.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parentPath"></param>
        /// <param name="inputStream"></param>
        /// <param name="parent"></param>
        /// <param name="passthroughStream"></param>
        public FileEntry(string name, Stream inputStream, FileEntry? parent = null, bool passthroughStream = false)
        {
            Parent = parent;
            Name = name;
            Passthrough = passthroughStream;

            if (parent == null)
            {
                ParentPath = null;
                FullPath = Name;
            }
            else
            {
                ParentPath = parent.FullPath;
                FullPath = $"{ParentPath}:{Name}";
            }

            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            if (!inputStream.CanRead)
            {
                Content = new MemoryStream();
            }

            // We want to be able to seek, so ensure any passthrough stream is Seekable
            if (passthroughStream && inputStream.CanSeek)
            {
                Content = inputStream;
                if (Content.Position != 0)
                {
                    Content.Position = 0;
                }
            }
            else
            {
                // Back with a temporary filestream, this is optimized to be cached in memory when possible automatically by .NET
                Content = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
                long? initialPosition = null;

                if (inputStream.CanSeek)
                {
                    initialPosition = inputStream.Position;
                    if (inputStream.Position != 0)
                    {
                        inputStream.Position = 0;
                    }
                }

                try
                {
                    inputStream.CopyTo(Content);
                }
                catch(Exception e)
                {
                    Logger.Debug("Failed to copy stream from {0} ({1}:{2})", FullPath, e.GetType(), e.Message);
                }

                if (inputStream.CanSeek && inputStream.Position != 0)
                {
                    inputStream.Position = initialPosition ?? 0;
                }

                Content.Position = 0;
            }
        }

        public string? ParentPath { get; set; }
        public string FullPath { get; set; }
        public FileEntry? Parent { get; set; }
        public string Name { get; set; }
        public Stream Content { get; set; }
        private bool Passthrough { get; }
        ~FileEntry()
        {
            if (!Passthrough)
            {
                Content?.Dispose();
            }
        }
    }
}