// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework.Tools
{
    public partial class UnitTestLauncher
    {
        public enum Communication
        {
            /// <summary>Error: method not found</summary>
            MethodError,

            /// <summary>Start of a test or of the initialisation of a test class</summary>
            Start,
            /// <summary>About to instantiate an instance of the test class</summary>
            Instantiate,
            /// <summary>About to run the setup method</summary>
            Setup,
            /// <summary>Initialisation of the test class complete</summary>
            SetupComplete,
            /// <summary>Error occurred while initialising a test or the test class</summary>
            SetupFail,

            /// <summary>Test was skipped</summary>
            Skipped,
            /// <summary>Test was completed successfully</summary>
            Pass,
            /// <summary>Test (proper) was not completed successfully</summary>
            Fail,

            /// <summary>All tests of this class have been run</summary>
            TestsComplete,
            /// <summary>About to run the cleanup method</summary>
            Cleanup,
            /// <summary>About to dispose the instance of the test class</summary>
            Dispose,
            /// <summary>Error occurred in the cleanup phase of a test or group</summary>
            CleanupFail,

            /// <summary>Processing of the test class has been completed</summary>
            Done,
        }
    }
}
