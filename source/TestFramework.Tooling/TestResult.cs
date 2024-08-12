﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace nanoFramework.TestFramework.Tooling
{
    public sealed class TestResult
    {
        #region Fields
        internal List<string> _messages = new List<string>();
        #endregion

        #region Construction
        /// <summary>
        /// Create the result for a test case
        /// </summary>
        /// <param name="testCase">Test case for which this is a result</param>
        /// <param name="index">Index of the test case in the selection; pass -1 for a new test case.</param>
        /// <param name="comPort">In case the test is run on real hardware: the COM-port
        /// of the device the test is executed on.</param>
        internal TestResult(TestCase testCase, int index, string comPort)
        {
            TestCase = testCase;
            Index = index;
            COMPort = comPort;
            Outcome = TestOutcome.None;
            ErrorMessage = "Test has not been run";
        }
        #endregion

        #region Properties
        /// <summary>
        /// The test case for which this is one of the results.
        /// </summary>
        public TestCase TestCase
        {
            get;
        }

        /// <summary>
        /// Get the index of the test case in the specification of
        /// the selection of test cases (as passed to <see cref="TestCaseCollection.TestCaseCollection(IEnumerable{ValueTuple{string, string, string}}, Func{string, string}, bool, LogMessenger)"/>).
        /// </summary>
        public int Index
        {
            get;
        }

        /// <summary>
        /// In case the test is run on real hardware: the COM-port
        /// of the device the test is executed on.
        /// </summary>
        public string COMPort
        {
            get;
            internal set;
        }

        public enum TestOutcome
        {
            /// <summary>
            /// Test Case Does Not Have an outcome.
            /// </summary>
            None = 0,

            /// <summary>
            /// Test Case Passed
            /// </summary>
            Passed = 1,

            /// <summary>
            /// Test Case Failed
            /// </summary>
            Failed = 2,

            /// <summary>
            /// Test Case Skipped
            /// </summary>
            Skipped = 3,

            /// <summary>
            /// Test Case Not found
            /// </summary>
            NotFound = 4,
        }
        /// <summary>
        /// Get the outcome of the test
        /// </summary>
        public TestOutcome Outcome
        {
            get;
            internal set;
        }

        /// <summary>
        /// Get the display name of the test.
        /// </summary>
        public string DisplayName
            => COMPort is null
                ? $"{TestCase.DisplayName} - {(string.IsNullOrEmpty(ErrorMessage) ? Outcome.ToString() : ErrorMessage)}"
                : $"{TestCase.DisplayNameWithoutDevice(TestCase.DisplayName)} [{COMPort}] - {(string.IsNullOrEmpty(ErrorMessage) ? Outcome.ToString() : ErrorMessage)}";

        /// <summary>
        /// Get a short description why the test did not pass
        /// </summary>
        public string ErrorMessage
        {
            get;
            internal set;
        }

        public TimeSpan Duration
        {
            get;
            internal set;
        }

        /// <summary>
        /// Get the textual output of the unit tests
        /// </summary>
        public IReadOnlyList<string> Messages
            => _messages;
        #endregion
    }
}
