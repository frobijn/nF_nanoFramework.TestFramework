// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework.Tools
{
    public partial class UnitTestLauncher
    {
        /// <summary>
        /// Summary of the various combinations of static/non-static test class,
        /// <see cref="ITestClass.CreateInstancePerTestMethod"/> and <see cref="ITestClass.SetupCleanupPerTestMethod"/>.
        /// The numerical values are passed to the unit test launcher.
        /// </summary>
        internal enum TestClassInitialisation
        {
            NoInstantiation = 0x00,
            InstantiateForAllMethods = 0x01,
            InstantiatePerTestMethod = 0x02,
            SetupCleanupPerClass = 0x00,
            SetupCleanupPerTestMethod = 0x10,
        }
    }
}
