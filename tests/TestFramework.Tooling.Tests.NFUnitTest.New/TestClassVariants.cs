// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest
{
    [TestClass(false, true /*this value is ignored as the class is static*/)]
    public static class StaticTestClassRunOneByOne
    {
        [TestMethod]
        [Trait("TestClass demonstration")]
        public static void Method()
        {

        }
    }

    [TestClass(true, true /*this value is ignored as the class is static*/)]
    [RunInParallel]
    public static class StaticTestClassRunInParallel
    {
        [TestMethod]
        [Trait("TestClass demonstration")]
        public static void Method()
        {

        }
    }

    [TestClass(false, false)]
    public static class TestClassInstantiateOnceForAllMethodsRunOneByOne
    {
        [Cleanup]
        [Setup]
        public static void SetupAndCleanup()
        {
        }


        [TestMethod]
        [Trait("TestClass demonstration")]
        public static void Method1()
        {

        }

        [TestMethod]
        [Trait("TestClass demonstration")]
        public static void Method2()
        {

        }
    }

    [TestClass(false, true)]
    public static class TestClassInstantiatePerMethodRunOneByOne
    {
        [Setup]
        public static void Setup()
        {
        }

        [TestMethod]
        [Trait("TestClass demonstration")]
        public static void Method1()
        {

        }

        [TestMethod]
        [Trait("TestClass demonstration")]
        public static void Method2()
        {

        }
    }

    [TestClass(true, true)]
    [RunInParallel]
    public static class TestClassInstantiatePerMethodRunInParallel
    {
        [TestMethod]
        [Trait("TestClass demonstration")]
        public static void Method1()
        {

        }

        [TestMethod]
        [Trait("TestClass demonstration")]
        public static void Method2()
        {

        }

        [Cleanup]
        public static void Cleanup()
        {
        }
    }
}
