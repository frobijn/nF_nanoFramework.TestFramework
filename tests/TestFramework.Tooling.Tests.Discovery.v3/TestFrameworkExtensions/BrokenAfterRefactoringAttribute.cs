// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest.TestFrameworkExtensions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class BrokenAfterRefactoringAttribute : Attribute, ITestMethod
    {
        bool ITestMethod.CanBeRun
            => false;
    }
}
