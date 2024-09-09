// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;
using TestFramework.Tooling.Tests.Helpers;
using nfTest = nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{
    [TestCategoriesProxyTest.CategoriesMock("In assembly")]
    public abstract class CategoriesProxyTest_AssemblyAttributes : nfTest.IAssemblyAttributes
    {
    }

    [TestClass]
    [CategoriesMock("In test class")]
    [TestCategory("nF test attributes")]
    public sealed class TestCategoriesProxyTest
    {
        [TestMethod]
        [CategoriesMock("Some", "category")]
        public void CategoriesProxy_CreatedForMethod()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.AreEqual(1, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
            Assert.AreEqual(typeof(TestCategoriesProxy), actual[0].GetType());

            var proxy = actual[0] as TestCategoriesProxy;
            CollectionAssert.AreEqual(
                new string[] { "Some", "category" },
                proxy.Categories);
        }

        [TestMethod]
        [TestCategory("Source code")]
        [CategoriesMock("Some", "category")]
        [CategoriesMock("Other")]
        public void CategoriesProxy_CreatedForMethodWithSource()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectHelper.FindMethodDeclaration(GetType(), thisMethod.Name);
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), source.Attributes, logger);

            logger.AssertEqual("");
            Assert.AreEqual(2, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
            Assert.AreEqual(typeof(TestCategoriesProxy), actual[0].GetType());
            Assert.AreEqual(typeof(TestCategoriesProxy), actual[1].GetType());

            var proxy = actual[0] as TestCategoriesProxy;
            CollectionAssert.AreEqual(
                new string[] { "Some", "category" },
                proxy.Categories);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("CategoriesMock", proxy.Source.Name);

            proxy = actual[1] as TestCategoriesProxy;
            CollectionAssert.AreEqual(
                new string[] { "Other" },
                proxy.Categories);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("CategoriesMock", proxy.Source.Name);
            Assert.AreNotEqual(actual[0].Source.LineNumber, proxy.Source.LineNumber);
        }

        [TestMethod]
        public void CategoriesProxy_CreatedForAssembly()
        {
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetAssemblyAttributeProxies(GetType().Assembly, new TestFrameworkImplementation(), logger);

            CollectionAssert.AreEqual(
                new object[] { },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.ITestCategories))
                 select msg.level).ToList()
            );

            TestCategoriesProxy proxy = actual.OfType<TestCategoriesProxy>()
                              .FirstOrDefault();
            Assert.IsNotNull(proxy);
            CollectionAssert.AreEquivalent(
                new string[] { "In assembly" },
                proxy.Categories);
            Assert.AreEqual(0, custom?.OfType<TestCategoriesProxy>().Count());
        }

        [TestMethod]
        public void CategoriesProxy_CreatedForClass()
        {
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetClassAttributeProxies(GetType(), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.AreEqual(1, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
            Assert.AreEqual(typeof(TestCategoriesProxy), actual[0].GetType());

            var proxy = actual[0] as TestCategoriesProxy;
            CollectionAssert.AreEquivalent(
                new string[] { "In test class" },
                proxy.Categories);
        }

        [TestMethod]
        [TestCategory("Source code")]
        public void CategoriesProxy_CreatedForClassWithSource()
        {
            var logger = new LogMessengerMock();
            ProjectSourceInventory.ClassDeclaration source = TestProjectHelper.FindClassDeclaration(GetType());
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetClassAttributeProxies(GetType(), new TestFrameworkImplementation(), source.Attributes, logger);

            logger.AssertEqual("");
            Assert.AreEqual(1, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
            Assert.AreEqual(typeof(TestCategoriesProxy), actual[0].GetType());

            var proxy = actual[0] as TestCategoriesProxy;
            CollectionAssert.AreEquivalent(
                new string[] { "In test class" },
                proxy.Categories);
            Assert.AreEqual("CategoriesMock", proxy.Source.Name);
        }

        #region CategoriesMockAttribute
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
        internal sealed class CategoriesMockAttribute : Attribute, nfTest.ITestCategories
        {
            public CategoriesMockAttribute(params string[] categories)
            {
                Categories = categories;
            }

            public string[] Categories
            {
                get;
            }
        }
        #endregion
    }
}
