// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;
using TestFramework.Tooling.Tests.Helpers;
using nfTest = nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{
    [TestClass]
    public class SetupProxyTest
    {
        [TestMethod]
        [TestCategory("nF test attributes")]
        [SetupMock]
        public void SetupProxyCreated()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(SetupProxy), actual[0].GetType());
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        [TestCategory("Source code")]
        [SetupMock]
        public void SetupProxyCreatedWithSource()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectHelper.FindMethodDeclaration(GetType(), thisMethod.Name);
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, new TestFrameworkImplementation(), source.Attributes, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(SetupProxy), actual[0].GetType());

            Assert.IsNotNull(actual[0].Source);
            Assert.AreEqual("SetupMock", actual[0].Source.Name);
        }

        private sealed class SetupMockAttribute : Attribute, nfTest.ISetup
        {
        }
    }
}
