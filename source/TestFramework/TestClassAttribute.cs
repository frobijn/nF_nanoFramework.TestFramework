// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// The attribute indicates that a class contains test methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TestClassAttribute : Attribute, ITestClass
    {
        #region Construction
        /// <summary>
        /// Indicate that the class is a test class.
        /// </summary>
        public TestClassAttribute()
        {
        }
        #endregion
    }
}
