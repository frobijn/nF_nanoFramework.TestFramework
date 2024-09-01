// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace nanoFramework.TestFramework.Tooling.Tools
{
    /// <summary>
    /// Constants for the communication between test adapter and
    /// the test host that does the heavy lifting.
    /// </summary>
    public static class TestAdapterMessages
    {
        /// <summary>
        /// The messages used in the communication between test adapter and
        /// the test host that does the heavy lifting.
        /// </summary>
        public static readonly Type[] Types = new Type[]
        {
            typeof(TestDiscoverer_Parameters), typeof(TestDiscoverer_DiscoveredTests),
            typeof(TestExecutor_TestCases_Parameters), typeof(TestExecutor_TestResults)
        };
    }

    #region TestDiscoverer
    /// <summary>
    /// Parameters to start the discovery of tests with.
    /// </summary>
    public sealed class TestDiscoverer_Parameters : ChildProcess_Parameters, InterProcessCommunicator.IMessage
    {
        /// <summary>
        /// The path of the assemblies to examine to discover unit tests
        /// </summary>
        [JsonProperty("S")]
        public List<string> Sources
        {
            get; set;
        }
    }

    /// <summary>
    /// (Partial) result of the test discovery
    /// </summary>
    public sealed class TestDiscoverer_DiscoveredTests : InterProcessCommunicator.IMessage
    {
        public sealed class TestCase
        {
            /// <summary>
            /// Gets or sets the display name of the test case.
            /// </summary>
            [JsonProperty("D")]
            public string DisplayName
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the fully qualified name of the test case.
            /// </summary>
            [JsonProperty("N")]
            public string FullyQualifiedName
            {
                get; set;
            }

            /// <summary>
            /// The source code file path of the test.
            /// </summary>
            [JsonProperty("S")]
            public string CodeFilePath
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the line number of the test.
            /// </summary>
            [JsonProperty("L")]
            public int? LineNumber
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the collection of traits
            /// </summary>
            [JsonProperty("T")]
            public List<string> Traits
            {
                get; set;
            }
        }

        /// <summary>
        /// Gets the test container source from which the test is discovered.
        /// </summary>
        [JsonProperty("S")]
        public string Source
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the collection of test cases discovered
        /// </summary>
        [JsonProperty("T")]
        public List<TestCase> TestCases
        {
            get; set;
        }
    }
    #endregion

    #region TestExecutor
    /// <summary>
    /// Parameters to start the execution of a selection of previously discovered tests with
    /// </summary>
    public sealed class TestExecutor_TestCases_Parameters : ChildProcess_Parameters, InterProcessCommunicator.IMessage
    {
        /// <summary>
        /// Description of the test case in a test assembly.
        /// </summary>
        public sealed class TestCase
        {
            /// <summary>
            /// Gets or sets the path to the assembly that contains the test case.
            /// </summary>
            [JsonProperty("S")]
            public string AssemblyFilePath
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the display name of the test case.
            /// </summary>
            [JsonProperty("D")]
            public string DisplayName
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the fully qualified name of the test case.
            /// </summary>
            [JsonProperty("N")]
            public string FullyQualifiedName
            {
                get; set;
            }
        }

        /// <summary>
        /// Get the test cases to execute.
        /// </summary>
        [JsonProperty("T")]
        public List<TestCase> TestCases
        {
            get; set;
        }
    }

    /// <summary>
    /// (Partial) result of the execution of the tests
    /// </summary>
    public sealed class TestExecutor_TestResults : InterProcessCommunicator.IMessage
    {
        public sealed class TestResult
        {
            /// <summary>
            /// Gets or sets the index of the test case in the selection to be run,
            /// or 
            /// </summary>
            [JsonProperty("I")]
            public int Index
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the outcome of a test case.
            /// </summary>
            [JsonProperty("O")]
            public int Outcome
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the exception message.
            /// </summary>
            [JsonProperty("E")]
            public string ErrorMessage
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the TestResult Display name.
            /// </summary>
            [JsonProperty("D")]
            public string DisplayName
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the test result Duration.
            /// </summary>
            [JsonProperty("T")]
            public TimeSpan Duration
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets whether the test was executed on real hardware
            /// </summary>
            [JsonProperty("H")]
            public bool? ForRealHardware
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the test messages.
            /// </summary>
            [JsonProperty("M")]
            public List<string> Messages
            {
                get; set;
            }
        }

        /// <summary>
        /// Gets or sets the ComputerName the tests have been executed on
        /// </summary>
        [JsonProperty("C")]
        public string ComputerName { get; set; }

        /// <summary>
        /// Get the test results.
        /// </summary>
        [JsonProperty("R")]
        public List<TestResult> TestResults
        {
            get; set;
        }
    }
    #endregion
}
