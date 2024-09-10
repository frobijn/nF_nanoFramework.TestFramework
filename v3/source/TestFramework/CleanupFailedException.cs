// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Raise this exception through the <c>Assert.</c><see cref="Assert.CleanupFailed(string, Exception)"/>("some message")
    /// if the test has passed but the cleanup after the test resulted in an error.
    /// </summary>
    public class CleanupFailedException : TestFrameworkException
    {
        /// <summary>
        /// Initializes a new instance of the CleanupFailedException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception. 
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException"></param>
        public CleanupFailedException(string message = null, Exception innerException = null) : base(message, innerException)
        { }
    }
}
