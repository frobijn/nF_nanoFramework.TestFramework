// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Base class for all exceptions that are thrown by the test framework.
    /// Test code that catches exceptions should check whether an exception is
    /// thrown by the framework, as those exceptions should in general be passed on.
    /// </summary>
    public abstract class TestFrameworkException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the TestFrameworkException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception. 
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException"></param>
        protected TestFrameworkException(string message = null, Exception innerException = null)
            : base(string.IsNullOrEmpty(message) ? message : Assert.ReplaceNulls(message), innerException)
        {
        }
    }
}
