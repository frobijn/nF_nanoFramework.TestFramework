// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest
{
    /// <summary>
    /// This class uses only the current TestFramework attributes but is
    /// analysed as a new-style test as this project uses new test attributes.
    /// </summary>
    [TestClass]
    public class TestWithMethods
    {
        [TestMethod]
        public void Test()
        {
        }

        [TestMethod, DeploymentConfiguration("Make and model")]
        public void Test2(byte[] makeAndModel)
        {
            Assert.IsNotNull(makeAndModel);
        }
    }
}
