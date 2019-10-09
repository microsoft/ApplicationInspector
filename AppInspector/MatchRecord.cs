// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using Microsoft.DevSkim;
using System.Collections;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Linq;

namespace Microsoft.AppInspector.CLI.Writers
{
    public class MatchRecord
    {
        public string Language { get; set; }
        public string Filename { get; set; }
        public int Filesize { get; set; }
        public string TextSample { get; set; }
        public Issue Issue { get; set; }
        public string Excerpt { get; set; }
    }

}