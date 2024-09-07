// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework.Tooling.Tools;

#if DEBUG
#if LAUNCHDEBUGGER
using System.Diagnostics;
#endif
#endif

namespace nanoFramework.TestFramework.TestHost
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
#endif
            if (args.Length > 2)
            {
                TestAdapter.Run(args[0], args[1], args[2]);
            }
        }
    }
}
