// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// A selection of test cases to run on a single (type of) device.
    /// </summary>
    public sealed class TestCaseSelection
    {
        #region Fields
        internal List<(int selectionIndex, TestCase testCase)> _testCases = new List<(int selectionIndex, TestCase testCase)>();
        #endregion

        #region Construction
        /// <summary>
        /// Create the selection of test cases
        /// </summary>
        /// <param name="assemblyFilePath">Path to the assembly file</param>
        internal TestCaseSelection(string assemblyFilePath)
        {
            AssemblyFilePath = assemblyFilePath;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the path to the assembly file that has to 
        /// </summary>
        public string AssemblyFilePath
        {
            get;
        }

        /// <summary>
        /// The selected test cases. For each test case the index of the matching
        /// entry in the <c>testCaseSelection</c> is also if the selection
        /// was created via <see cref="TestCaseCollection.TestCaseCollection(IEnumerable{ValueTuple{string, string, string}}, Func{string, string}, bool, LogMessenger)()"/>.
        /// If the test case selection was not created based on a selection, the list of
        /// contains all test cases (for the device) and the <c>selectionIndex</c> is -1.
        /// The test cases are in the same order they are discovered in the test assembly.
        /// </summary>
        public IReadOnlyList<(int selectionIndex, TestCase testCase)> TestCases
            => _testCases;
        #endregion
    }
}
