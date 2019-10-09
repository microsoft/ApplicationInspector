// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.AppInspector.CLI.Writers
{
    public class DummyWriter : Writer
    {

        public override void WriteApp(AppProfile appCharacteriation)
        {

        }

      
        public override void FlushAndClose()
        {
            // This is intentionaly empty
        }

    }
}
