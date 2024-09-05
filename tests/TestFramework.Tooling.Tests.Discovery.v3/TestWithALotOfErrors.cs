// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest
{
    [TestClass]
    [TestClass2]
    public class TestWithALotOfErrors
    {
        [Setup, DeploymentConfiguration("some", "key", "invalid_type")]
        public void Setup1(byte[] some, string key, double invalid)
        {

        }

        [Setup]
        public void Setup2()
        {

        }

        [Cleanup, DeploymentConfiguration("Some key")]
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
        [TestCategory("Ignored")]
        public void All()
        {

        }


        [AttributeUsage(AttributeTargets.Class)]
        public sealed class TestClass2Attribute : Attribute, ITestClass
        {
            bool ITestClass.CreateInstancePerTestMethod => true;

            bool ITestClass.SetupCleanupPerTestMethod => true;
        }

        [DeploymentConfiguration("too_many"), Setup]
        public void Setup3()
        {

        }
    }
}
