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

[assembly: TestFramework.Tooling.Tests.TestFrameworkProxy.TraitsProxyTest.TraitsMock("This is not correct!")]

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{

    [TestClass]
    [TraitsMock("This is not correct!")]
    public sealed class TraitsProxyTest
    {
        [TestMethod]
        [TestCategory("nF test attributes")]
        [TraitsMock("Some", "trait")]
        public void TraitsProxyCreatedForMethod()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TraitsProxy), actual[0].GetType());

            var proxy = actual[0] as TraitsProxy;
            CollectionAssert.AreEqual(
                new string[] { "Some", "trait" },
                proxy.Traits);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        [TestCategory("Source code")]
        [TraitsMock("Some", "trait")]
        [TraitsMock("Other")]
        public void TraitsProxyCreatedForMethodWithSource()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectHelper.FindMethodDeclaration(GetType(), thisMethod.Name);
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, new TestFrameworkImplementation(), source.Attributes, logger);

            Assert.AreEqual(0, logger.Messages.Count);
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
        [TestCategory("nF test attributes")]
        public void TraitsProxyErrorForAssembly()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType().Assembly, new TestFrameworkImplementation(), logger);

            CollectionAssert.AreEqual(
                new object[] { LoggingLevel.Error },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.ITraits))
                 select msg.level).ToList()
            );
            Assert.AreEqual(0, actual?.OfType<TraitsProxy>().Count() ?? -1);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        public void TraitsProxyErrorForClass()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType(), new TestFrameworkImplementation(), null, logger);

            CollectionAssert.AreEqual(
                new object[] { LoggingLevel.Error },
                (from msg in logger.Messages select msg.level).ToList()
            );
            Assert.AreEqual(0, actual?.Count ?? -1);
        }

        #region TraitsMockAttribute
        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
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
