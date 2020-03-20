// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Writes in json format
    /// Users can select arguments to filter output to 1. only simple tags 2. only matchlist without rollup metadata etc. 3. everything
    /// Lists of tagreportgroups are written as well as match list details so users have chose to present the same
    /// UI as shown in the HTML report to the level of detail desired...
    /// </summary>
    public class JsonWriter : Writer
    {
        public override void WriteApp(AppProfile app)
        {

            if (app.SimpleTagsOnly)
            {
                List<string> keys = new List<string>(app.MetaData.UniqueTags);
                keys.Sort();
                TagsFile tags = new TagsFile();
                tags.Tags = keys.ToArray();
                TextWriter.Write(JsonConvert.SerializeObject(tags, Formatting.Indented));
            }
            else
            {
                JsonSerializer jsonSerializer = new JsonSerializer();
                jsonSerializer.Formatting = Formatting.Indented;
                jsonSerializer.Serialize(TextWriter, app);
            }
        }


        public override void FlushAndClose()
        {
            TextWriter.Flush();
            TextWriter.Close();
        }


    }
}
