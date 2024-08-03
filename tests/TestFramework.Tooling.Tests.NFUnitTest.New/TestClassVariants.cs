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
    }
}
