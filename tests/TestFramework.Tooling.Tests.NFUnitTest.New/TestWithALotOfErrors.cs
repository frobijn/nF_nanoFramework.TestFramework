// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest
{
    [TestClass(true, false)]
    [TestClass2]
    public class TestWithALotOfErrors
    {
        [Setup]
        public void Setup1()
        {

        }

        [Setup]
        public void Setup2()
        {

        }

        [Cleanup]
        public void Cleanup1()
        {

        }

        [Cleanup]
        public void Cleanup2()
        {

        }

        [TestMethod]
        [Cleanup]
        [Setup]
        [Trait("Ignored")]
        public void All()
        {

        }


        [AttributeUsage(AttributeTargets.Class)]
        private class TestClass2Attribute : Attribute, ITestClass
        {
            bool ITestClass.InstantiatePerMethod => true;

            bool ITestClass.RunClassMethodsInParallel => false;
        }
    }
}
