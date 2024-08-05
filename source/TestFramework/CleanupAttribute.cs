// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Mark a method that should be called to clean up after the tests.
    /// The <see cref="ITestClass"/>/<see cref="TestClassAttribute"/> determines whether
    /// a cleanup is done after each test method or once all test methods have been executed.
    /// A test class can have at most one cleanup method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CleanupAttribute : Attribute, ICleanup
    {
    }
}
