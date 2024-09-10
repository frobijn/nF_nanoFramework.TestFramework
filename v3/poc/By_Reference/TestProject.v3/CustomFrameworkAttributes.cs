// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace NFUnitTest
{
    [TestFixture]
    [Trait("Attributes")]
    [Trait("Framework extensions")]
    public class CustomFrameworkAttributes
    {
        [Fact]
        public void TestMethod()
        {
        }

        [OutOfOrder]
        public void TestMethodIsIncorrectNeedsReworkToBeDoneLater()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TestFixtureAttribute : Attribute, ITestClass
    {
        bool ITestClass.CreateInstancePerTestMethod
            => false;

        bool ITestClass.SetupCleanupPerTestMethod
            => false;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FactAttribute : Attribute, ITestMethod
    {
        bool ITestMethod.CanBeRun
            => true;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class OutOfOrderAttribute : Attribute, ITestMethod, ITestCategories
    {
        bool ITestMethod.CanBeRun
            => false;

        string[] ITestCategories.Categories
            => new string[] { "Out of order, test needs rework after refactoring" };
    }



    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class TraitAttribute : Attribute, ITestCategories
    {
        public TraitAttribute(string trait)
        {
            _trait = trait;
        }

        string[] ITestCategories.Categories
            => new string[] { _trait };
        private readonly string _trait;
    }
}
