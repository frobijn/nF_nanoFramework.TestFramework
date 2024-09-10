// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//=============================================================================
//
// To select the tests to debug, change the SelectUnitTests.json file
// and rebuild the project.
//
// If the SelectUnitTests.json is missing, also rebuild the project.
//
// There is no need to change the code in this file.
//
//=============================================================================

namespace DebugTestProject
{
    public class Program
    {
        public static void Main()
        {
            // Do not remove the next line of code! It runs the selected tests.
            nanoFramework.TestFramework.Tools.UnitTestLauncher.Run("***");
        }
    }
}
