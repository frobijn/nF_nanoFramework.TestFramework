// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Execution.Tests
{
    [TestClass()]
    public static class StaticTestClass
    {
        [TestMethod]
        [Trait("TestClass demonstration")]
        public static void Method()
        {
        }

        [Setup]
        public static void Setup()
        {
        }

        [Cleanup]
        public static void Cleanup()
        {
        }
    }

    [TestClass]
    public class NonStaticTestClass
    {
        [TestMethod]
        [Trait("TestClass demonstration")]
        public void Method1()
        {
            Assert.IsNotNull(this);
        }

        [TestMethod]
        [Trait("TestClass demonstration")]
        public void Method2()
        {
            Assert.IsNotNull(this);
        }

        [Setup]
        public void Setup()
        {
            Assert.IsNotNull(this);
        }

        [Cleanup]
        public void Cleanup()
        {
            Assert.IsNotNull(this);
        }
    }
}
