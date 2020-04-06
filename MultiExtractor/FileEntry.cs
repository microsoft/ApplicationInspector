// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.


using System;
using System.IO;

namespace MultiExtractor
{
    public class FileEntry
    {
        public FileEntry(string name, string parentPath, Stream content)
        {
            Name = name;
            if (string.IsNullOrEmpty(parentPath))
            {
                FullPath = Name;
            }
            else
            {
                FullPath = $"{parentPath}:{name}";
            }
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            Content = new MemoryStream();
            if (content.CanSeek)
            {
                content.Position = 0;
            }
            content.CopyTo(Content);
        }

        public string FullPath { get; set; }
        public string Name { get; set; }
        public MemoryStream Content { get; set; }



    }
}
