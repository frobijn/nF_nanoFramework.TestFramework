// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Mark a method that should be called to setup the test context
    /// for the tests of this test class. The <see cref="ITestClass"/>/<see cref="TestClassAttribute"/> determines whether a
    /// setup method is called before each test method or once before all test methods.
    /// A test class can have at most one setup method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SetupAttribute : Attribute, ISetup
    {
    }
}
