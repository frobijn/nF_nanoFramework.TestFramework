// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Raise this exception through the <c>Assert.</c><see cref="Assert.Inconclusive(string)"/>("some message").
    /// A test should be marked as inconclusive if the conditions to consider running the test are met,
    /// but initialising the setup fails before the test proper could be started.
    /// </summary>
    public class SetupFailedException : TestFrameworkException
    {
        /// <summary>
        /// Initializes a new instance of the InconclusiveException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception. 
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException"></param>
        public SetupFailedException(string message = null, Exception innerException = null) : base(message, innerException)
        { }
    }
}
