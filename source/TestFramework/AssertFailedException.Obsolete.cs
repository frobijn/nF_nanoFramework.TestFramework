// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace TestFrameworkShared
{
    /// <summary>
    /// <see cref="AssertFailedException"/> class as originally declared in the wrong namespace.
    /// Changing the namespace may break extensions to the test framework created by users of
    /// nanoFramework. Instead this class is made obsolete.
    /// </summary>
    [Obsolete("This exception is deprecated and will be removed in a future version. Use nanoFramework.TestFramework.AssertFailedException instead.", false)]
    public class AssertFailedException : TestFrameworkException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssertFailedException"/> class.
        /// </summary>
        /// <param name="msg">The message.</param>
        /// <param name="ex">The exception.</param>
        public AssertFailedException(string msg, Exception ex)
               : base(msg, ex)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertFailedException"/> class.
        /// </summary>
        /// <param name="msg">The message.</param>
        public AssertFailedException(string msg)
            : base(msg)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AssertFailedException"/> class.
        /// </summary>
        public AssertFailedException()
        {
        }
    }
}
