// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// To skip a test, raise this exception through the <c>Assert.</c><see cref="Assert.SkipTest(string)"/>("some message").
    /// A test should be skipped if the conditions to consider running the test are not met,
    /// e.g., because the device the test is executed on does not support a feature required
    /// for the test.
    /// </summary>
    public class SkipTestException : TestFrameworkException
    {
        /// <summary>
        /// Initializes a new instance of the SkipTestException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception. 
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException"></param>
        public SkipTestException(string message = null, Exception innerException = null) : base(message, innerException)
        { }
    }
}
