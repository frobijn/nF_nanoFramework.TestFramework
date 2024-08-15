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

    [DeploymentConfigurationProxyTest.DeploymentConfigurationMock("assembly")] // This is not correct!
    public sealed class DeploymentConfigurationProxyTest_AssemblyAttributes : nfTest.IAssemblyAttributes
    {
    }

    [TestClass]
    [DeploymentConfigurationMock("class")] // This is not correct!
    [TestCategory("nF test attributes")]
    public sealed class DeploymentConfigurationProxyTest
    {
        [TestMethod]
        [DeploymentConfigurationMock("method")]
        public void DeploymentConfigurationProxyCreated()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.AreEqual(2, actual.Count);
            CollectionAssert.AreEquivalent(
                new object[] { typeof(DeploymentConfigurationProxy), typeof(SetupProxy) },
                (from a in actual select a.GetType()).ToArray()
            );

            var proxy = (from a in actual
                         where a.GetType() == typeof(DeploymentConfigurationProxy)
                         select a).First() as DeploymentConfigurationProxy;
            CollectionAssert.AreEqual(
                new object[] { "method" },
                proxy.ConfigurationKeys
            );
        }

        [TestMethod]
        [TestCategory("Source code")]
        [DeploymentConfigurationMock("some", "key")]
        public void DeploymentConfigurationProxyMultipleCreatedWithSource()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectHelper.FindMethodDeclaration(GetType(), thisMethod.Name);
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), source.Attributes, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.AreEqual(2, actual.Count);
            CollectionAssert.AreEquivalent(
                new object[] { typeof(DeploymentConfigurationProxy), typeof(SetupProxy) },
                (from a in actual select a.GetType()).ToArray()
            );

            foreach (AttributeProxy p in actual)
            {
                Assert.IsNotNull(p.Source);
                Assert.AreEqual("DeploymentConfigurationMock", p.Source.Name);
            }

            var proxy = (from a in actual
                         where a.GetType() == typeof(DeploymentConfigurationProxy)
                         select a).First() as DeploymentConfigurationProxy;
            CollectionAssert.AreEqual(
                new object[] { "some", "key" },
                proxy.ConfigurationKeys
            );
        }

        [TestMethod]
        public void DeploymentConfigurationProxyErrorForAssembly()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAssemblyAttributeProxies(GetType().Assembly, new TestFrameworkImplementation(), logger);

            CollectionAssert.AreEqual(
                new object[] { LoggingLevel.Error },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.IDeploymentConfiguration))
                 select msg.level).ToList()
            );
            Assert.AreEqual(0, actual?.OfType<DeploymentConfigurationProxy>().Count() ?? -1);
        }

        [TestMethod]
        public void DeploymentConfigurationProxyErrorForClass()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetClassAttributeProxies(GetType(), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual(
@"Error: TestFramework.Tooling.Tests:TestFramework.Tooling.Tests.TestFrameworkProxy.DeploymentConfigurationProxyTest: Error: Attribute implementing 'IDeploymentConfiguration' can only be applied to a method. Attribute is ignored.
Error: TestFramework.Tooling.Tests:TestFramework.Tooling.Tests.TestFrameworkProxy.DeploymentConfigurationProxyTest: Error: Attribute implementing 'ISetup' can only be applied to a method. Attribute is ignored.");
            Assert.AreEqual(0, actual?.Count ?? -1);
        }

        #region DeploymentConfigurationMockAttribute
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
        internal sealed class DeploymentConfigurationMockAttribute : Attribute, nfTest.IDeploymentConfiguration
        {
            public DeploymentConfigurationMockAttribute(params string[] configurationKeys)
            {
                ConfigurationKeys = configurationKeys;
            }

            public string[] ConfigurationKeys
            {
                get;
            }
        }
        #endregion
    }
}
