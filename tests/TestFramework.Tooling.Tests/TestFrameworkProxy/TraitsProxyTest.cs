﻿// Licensed to the .NET Foundation under one or more agreements.
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
    [TraitsProxyTest.TraitsMock("In assembly")]
    public abstract class TraitsProxyTest_AssemblyAttributes : nfTest.IAssemblyAttributes
    {
    }

    [TestClass]
    [TraitsMock("In test class")]
    [TestCategory("nF test attributes")]
    public sealed class TraitsProxyTest
    {
        [TestMethod]
        [TraitsMock("Some", "trait")]
        public void TraitsProxy_CreatedForMethod()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TraitsProxy), actual[0].GetType());

            var proxy = actual[0] as TraitsProxy;
            CollectionAssert.AreEqual(
                new string[] { "Some", "trait" },
                proxy.Traits);
        }

        [TestMethod]
        [TestCategory("Source code")]
        [TraitsMock("Some", "trait")]
        [TraitsMock("Other")]
        public void TraitsProxy_CreatedForMethodWithSource()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectHelper.FindMethodDeclaration(GetType(), thisMethod.Name);
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), source.Attributes, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual(typeof(TraitsProxy), actual[0].GetType());
            Assert.AreEqual(typeof(TraitsProxy), actual[1].GetType());

            var proxy = actual[0] as TraitsProxy;
            CollectionAssert.AreEqual(
                new string[] { "Some", "trait" },
                proxy.Traits);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("TraitsMock", proxy.Source.Name);

            proxy = actual[1] as TraitsProxy;
            CollectionAssert.AreEqual(
                new string[] { "Other" },
                proxy.Traits);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("TraitsMock", proxy.Source.Name);
            Assert.AreNotEqual(actual[0].Source.LineNumber, proxy.Source.LineNumber);
        }

        [TestMethod]
        public void TraitsProxy_CreatedForAssembly()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAssemblyAttributeProxies(GetType().Assembly, new TestFrameworkImplementation(), logger);

            CollectionAssert.AreEqual(
                new object[] { },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.ITraits))
                 select msg.level).ToList()
            );

            TraitsProxy proxy = actual.OfType<TraitsProxy>()
                              .FirstOrDefault();
            Assert.IsNotNull(proxy);
            CollectionAssert.AreEquivalent(
                new string[] { "In assembly" },
                proxy.Traits);
        }

        [TestMethod]
        public void TraitsProxy_CreatedForClass()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetClassAttributeProxies(GetType(), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TraitsProxy), actual[0].GetType());

            var proxy = actual[0] as TraitsProxy;
            CollectionAssert.AreEquivalent(
                new string[] { "In test class" },
                proxy.Traits);
        }

        [TestMethod]
        [TestCategory("Source code")]
        public void TraitsProxy_CreatedForClassWithSource()
        {
            var logger = new LogMessengerMock();
            ProjectSourceInventory.ClassDeclaration source = TestProjectHelper.FindClassDeclaration(GetType());
            List<AttributeProxy> actual = AttributeProxy.GetClassAttributeProxies(GetType(), new TestFrameworkImplementation(), source.Attributes, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TraitsProxy), actual[0].GetType());

            var proxy = actual[0] as TraitsProxy;
            CollectionAssert.AreEquivalent(
                new string[] { "In test class" },
                proxy.Traits);
            Assert.AreEqual("TraitsMock", proxy.Source.Name);
        }

        #region TraitsMockAttribute
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
        internal sealed class TraitsMockAttribute : Attribute, nfTest.ITraits
        {
            public TraitsMockAttribute(params string[] traits)
            {
                Traits = traits;
            }

            public string[] Traits
            {
                get;
            }
        }
        #endregion
    }
}
