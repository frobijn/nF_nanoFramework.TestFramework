// Copyright (c) .NET Foundation and Contributors.
// See LICENSE file in the project root for full license information.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// The attribute marks a method as being a test method. The attribute can be omitted
    /// if any of the other test-related attributes is present.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class TestMethodAttribute : Attribute, ITestMethod
    {
        #region ITestMethod implementation
        bool ITestMethod.CanBeRun
            => true;
        #endregion
    }
}
