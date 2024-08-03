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

[assembly: TestFramework.Tooling.Tests.TestFrameworkProxy.RunInParallelProxyTest.RunInParallelMock(true)]

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{

    [TestClass]
    [RunInParallelMock(false)]
    public sealed class RunInParallelProxyTest
    {
        [TestMethod]
        [TestCategory("nF test attributes")]
        public void RunInParallelProxyCreatedForAssembly()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType().Assembly, logger);

            CollectionAssert.AreEqual(
                new object[] { },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.IRunInParallel))
                 select msg.level).ToList()
            );
            Assert.IsNotNull(actual);

            RunInParallelProxy proxy = actual.OfType<RunInParallelProxy>()
                                             .FirstOrDefault();
            Assert.IsNotNull(proxy);
            Assert.AreEqual(true, proxy.CanRunInParallel);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        public void RunInParallelProxyCreatedForClass()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType(), null, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(RunInParallelProxy), actual[0].GetType());

            var proxy = actual[0] as RunInParallelProxy;
            Assert.AreEqual(false, proxy.CanRunInParallel);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        [TestCategory("Source code")]
        public void RunInParallelProxyCreatedForClassWithSource()
        {
            ProjectSourceInventory.ClassDeclaration source = TestProjectSourceAnalyzer.FindClassDeclaration(GetType());
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType(), source.Attributes, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(RunInParallelProxy), actual[0].GetType());

            var proxy = actual[0] as RunInParallelProxy;
            Assert.AreEqual(false, proxy.CanRunInParallel);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("RunInParallelMock", proxy.Source.Name);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        [RunInParallelMock(true)]
        public void RunInParallelProxyCreatedForMethod()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, null, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(RunInParallelProxy), actual[0].GetType());

            var proxy = actual[0] as RunInParallelProxy;
            Assert.AreEqual(true, proxy.CanRunInParallel);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        [TestCategory("Source code")]
        [RunInParallelMock(true)]
        public void RunInParallelProxyCreatedForMethodWithSource()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectSourceAnalyzer.FindMethodDeclaration(GetType(), thisMethod.Name);
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, source.Attributes, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(RunInParallelProxy), actual[0].GetType());

            var proxy = actual[0] as RunInParallelProxy;
            Assert.AreEqual(true, proxy.CanRunInParallel);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("RunInParallelMock", proxy.Source.Name);
        }

        #region RunInParallelMockAttribute
        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
        internal sealed class RunInParallelMockAttribute : Attribute, nfTest.IRunInParallel
        {
            public RunInParallelMockAttribute(bool canRunInParallel)
            {
                CanRunInParallel = canRunInParallel;
            }

            public bool CanRunInParallel
            {
                get;
            }
        }
        #endregion
    }
}
