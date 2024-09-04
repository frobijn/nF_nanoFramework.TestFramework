// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public void DeploymentConfigurationProxy_Created()
        {
            var thisMethod = MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.AreEqual(1, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
            Assert.AreEqual(typeof(DeploymentConfigurationProxy), actual[0].GetType());

            var proxy = actual[0] as DeploymentConfigurationProxy;
            CollectionAssert.AreEqual(
                new object[] { "method" },
                proxy.ConfigurationKeys
            );
        }

        [TestMethod]
        [TestCategory("Source code")]
        [DeploymentConfigurationMock("method")]
        public void DeploymentConfigurationProxy_WithSource()
        {
            var thisMethod = MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectHelper.FindMethodDeclaration(GetType(), thisMethod.Name);
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), source.Attributes, logger);

            logger.AssertEqual("");
            Assert.AreEqual(1, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
            Assert.AreEqual(typeof(DeploymentConfigurationProxy), actual[0].GetType());

            Assert.IsNotNull(actual[0].Source);
            Assert.AreEqual("DeploymentConfigurationMock", actual[0].Source.Name);

            var proxy = actual[0] as DeploymentConfigurationProxy;
            CollectionAssert.AreEqual(
                new object[] { "method" },
                proxy.ConfigurationKeys
            );
        }

        [TestMethod]
        public void DeploymentConfigurationProxy_ErrorForAssembly()
        {
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetAssemblyAttributeProxies(GetType().Assembly, new TestFrameworkImplementation(), logger);

            CollectionAssert.AreEqual(
                new object[] { LoggingLevel.Error },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.IDeploymentConfiguration))
                 select msg.level).ToList()
            );
            Assert.AreEqual(0, actual?.OfType<DeploymentConfigurationProxy>().Count());
            Assert.AreEqual(0, custom?.Count);
        }

        [TestMethod]
        public void DeploymentConfigurationProxy_ErrorForClass()
        {
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetClassAttributeProxies(GetType(), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual(
@"Error: TestFramework.Tooling.Tests:TestFramework.Tooling.Tests.TestFrameworkProxy.DeploymentConfigurationProxyTest: Error: Attribute implementing 'nanoFramework.TestFramework.IDeploymentConfiguration' cannot be applied to a test class. Attribute is ignored.");
            Assert.AreEqual(0, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
        }


        [TestMethod]
        public void DeploymentConfigurationProxy_GetDeploymentConfigurationArguments()
        {
            Type classType = typeof(TestDeploymentConfigurationArguments);

            #region OK
            MethodInfo method = classType.GetMethod(nameof(TestDeploymentConfigurationArguments.OK));
            var deploymentProxy = AttributeProxy.GetMethodAttributeProxies(method, new TestFrameworkImplementation(), null, null).framework[0] as DeploymentConfigurationProxy;
            var logger = new LogMessengerMock();

            IReadOnlyList<(string key, Type valueType)> actual = deploymentProxy.GetDeploymentConfigurationArguments(method, false, logger);
            logger.AssertEqual("");
            Assert.AreEqual(
                @"String 'String key', Byte[] 'Binary key'" + '\n',
                string.Join(", ", from a in actual
                                  select $"{a.valueType.Name} '{a.key}'") + '\n'
                );
            #endregion

            #region MoreArguments - test method
            method = classType.GetMethod(nameof(TestDeploymentConfigurationArguments.MoreArguments));
            deploymentProxy = AttributeProxy.GetMethodAttributeProxies(method, new TestFrameworkImplementation(), null, null).framework[0] as DeploymentConfigurationProxy;
            logger = new LogMessengerMock();

            actual = deploymentProxy.GetDeploymentConfigurationArguments(method, false, logger);
            logger.AssertEqual(
@"Error: TestFramework.Tooling.Tests.TestFrameworkProxy.DeploymentConfigurationProxyTest+TestDeploymentConfigurationArguments.MoreArguments: Error: The number of arguments of the method does not match the number of configuration keys specified by the attribute that implements 'IDeploymentConfiguration'.");
            Assert.AreEqual(
                @"" + '\n',
                string.Join(", ", from a in actual
                                  select $"{a.valueType.Name} '{a.key}'") + '\n'
                );
            #endregion

            #region MoreArguments - data row method
            method = classType.GetMethod(nameof(TestDeploymentConfigurationArguments.MoreArguments));
            deploymentProxy = AttributeProxy.GetMethodAttributeProxies(method, new TestFrameworkImplementation(), null, null).framework[0] as DeploymentConfigurationProxy;
            logger = new LogMessengerMock();

            actual = deploymentProxy.GetDeploymentConfigurationArguments(method, true, logger);
            logger.AssertEqual(@"");
            Assert.AreEqual(
                @"Int32 'Integer key', Int64 'Long key'" + '\n',
                string.Join(", ", from a in actual
                                  select $"{a.valueType.Name} '{a.key}'") + '\n'
                );
            #endregion

            #region TooManyKeys
            method = classType.GetMethod(nameof(TestDeploymentConfigurationArguments.TooManyKeys));
            deploymentProxy = AttributeProxy.GetMethodAttributeProxies(method, new TestFrameworkImplementation(), null, null).framework[0] as DeploymentConfigurationProxy;
            logger = new LogMessengerMock();

            actual = deploymentProxy.GetDeploymentConfigurationArguments(method, false, logger);
            logger.AssertEqual(
@"Error: TestFramework.Tooling.Tests.TestFrameworkProxy.DeploymentConfigurationProxyTest+TestDeploymentConfigurationArguments.TooManyKeys: Error: The number of arguments of the method does not match the number of configuration keys specified by the attribute that implements 'IDeploymentConfiguration'.");
            Assert.AreEqual(
                @"" + '\n',
                string.Join(", ", from a in actual
                                  select $"{a.valueType.Name} '{a.key}'") + '\n'
                );
            #endregion

            #region IncorrectType
            method = classType.GetMethod(nameof(TestDeploymentConfigurationArguments.IncorrectType));
            deploymentProxy = AttributeProxy.GetMethodAttributeProxies(method, new TestFrameworkImplementation(), null, null).framework[0] as DeploymentConfigurationProxy;
            logger = new LogMessengerMock();

            actual = deploymentProxy.GetDeploymentConfigurationArguments(method, false, logger);
            logger.AssertEqual(
@"Error: TestFramework.Tooling.Tests.TestFrameworkProxy.DeploymentConfigurationProxyTest+TestDeploymentConfigurationArguments.IncorrectType: Error: An argument of the method must be of type 'byte[]', 'int', 'long' or 'string'.");
            Assert.AreEqual(
                @"" + '\n',
                string.Join(", ", from a in actual
                                  select $"{a.valueType.Name} '{a.key}'") + '\n'
                );
            #endregion
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private sealed class TestDeploymentConfigurationArguments
        {
            [DeploymentConfigurationMock("String key", "Binary key")]
            public void OK(string text, byte[] binary)
            {
            }

            [DeploymentConfigurationMock("Integer key", "Long key")]
            public void MoreArguments(int ioPort, long address, bool dataRowArgument)
            {
            }

            [DeploymentConfigurationMock("key too many")]
            public void TooManyKeys()
            {
            }

            [DeploymentConfigurationMock("key")]
            public void IncorrectType(double gain)
            {
            }
        }
#pragma warning restore IDE0060 // Remove unused parameter


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
