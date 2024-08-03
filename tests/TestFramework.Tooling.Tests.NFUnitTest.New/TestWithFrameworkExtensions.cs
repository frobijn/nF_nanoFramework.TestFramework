// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;
using TestFramework.Tooling.Tests.NFUnitTest.TestFrameworkExtensions;

namespace TestFramework.Tooling.Tests.NFUnitTest
{
    [TestClass]
    public class TestWithFrameworkExtensions
    {
        [BrokenAfterRefactoring]
        public void TestThatIsNowInDisarray()
        {

        }

        [TestOnDoublePrecisionDevice]
        public void TestDoublePrecisionCalculation()
        {
        }
    }
}
