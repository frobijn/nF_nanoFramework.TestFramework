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
    [CleanupProxyTest.CleanupMock] // This is not correct!
    public sealed class CleanupProxyTest_AssemblyAttributes : nfTest.IAssemblyAttributes
    {
    }

    [TestClass]
    [TestCategory("nF test attributes")]
    [CleanupMock] // This is not correct!
    public class CleanupProxyTest
    {
        [TestMethod]
        [CleanupMock]
        public void CleanupProxy_Created()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(CleanupProxy), actual[0].GetType());
        }

        [TestMethod]
        [TestCategory("Source code")]
        [CleanupMock]
        public void CleanupProxy_CreatedWithSource()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectHelper.FindMethodDeclaration(GetType(), thisMethod.Name);
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), source.Attributes, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(CleanupProxy), actual[0].GetType());

            Assert.IsNotNull(actual[0].Source);
            Assert.AreEqual("CleanupMock", actual[0].Source.Name);
        }

        [TestMethod]
        public void CleanupProxy_ErrorForAssembly()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAssemblyAttributeProxies(GetType().Assembly, new TestFrameworkImplementation(), logger);

            CollectionAssert.AreEqual(
                new object[] { LoggingLevel.Error },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.ICleanup))
                 select msg.level).ToList()
            );
            Assert.AreEqual(0, actual?.OfType<CleanupProxy>().Count() ?? -1);
        }

        [TestMethod]
        public void CleanupProxy_ErrorForClass()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetClassAttributeProxies(GetType(), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual(@"Error: TestFramework.Tooling.Tests:TestFramework.Tooling.Tests.TestFrameworkProxy.CleanupProxyTest: Error: Attribute implementing 'ICleanup' can only be applied to a method. Attribute is ignored.");
            Assert.AreEqual(0, actual?.Count ?? -1);
        }

        internal sealed class CleanupMockAttribute : Attribute, nfTest.ICleanup
        {
        }
    }
}
