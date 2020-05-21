// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Microsoft.CST.OpenSource.MultiExtractor
{
    public class FileEntry
    {
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

            // We want to be able to seek, so ensure any passthrough stream is Seekable
            if (passthroughStream && inputStream.CanSeek)
            {
                Content = inputStream;
                Content.Position = 0;
            }
            else
            {
                // Back with a temporary filestream, this is optimized to be cached in memory when possible automatically by .NET
                Content = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
                long? initialPosition = null;

                if (inputStream.CanSeek)
                {
                    initialPosition = inputStream.Position;
                    inputStream.Position = 0;
                }
                inputStream.CopyTo(Content);
                if (inputStream.CanSeek)
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