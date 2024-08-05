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

[assembly: TestFramework.Tooling.Tests.TestFrameworkProxy.DataRowProxyTest.DataRowMock(42)] // This is not correct!

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{

    [TestClass]
    [DataRowMock(42)] // This is not correct!
    public sealed class DataRowProxyTest
    {
        [TestMethod]
        [TestCategory("nF test attributes")]
        [DataRowMock(42)]
        public void DataRowProxyCreated()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(DataRowProxy), actual[0].GetType());

            var proxy = actual[0] as DataRowProxy;
            CollectionAssert.AreEqual(
                new object[] { 42 },
                proxy.MethodParameters
            );
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        [TestCategory("Source code")]
        [DataRowMock(42, 'A')]
        [DataRowMock(3.14d, "B")]
        public void DataRowProxyMultipleCreatedWithSource()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectHelper.FindMethodDeclaration(GetType(), thisMethod.Name);
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, new TestFrameworkImplementation(), source.Attributes, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual(typeof(DataRowProxy), actual[0].GetType());
            Assert.AreEqual(typeof(DataRowProxy), actual[1].GetType());

            var proxy = actual[0] as DataRowProxy;
            CollectionAssert.AreEqual(
                new object[] { 42, 'A' },
                proxy.MethodParameters
            );
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("DataRowMock", proxy.Source.Name);

            proxy = actual[1] as DataRowProxy;
            CollectionAssert.AreEqual(
                new object[] { 3.14d, "B" },
                proxy.MethodParameters
            );
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("DataRowMock", proxy.Source.Name);
            Assert.AreNotEqual(actual[0].Source.LineNumber, proxy.Source.LineNumber);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        public void DataRowProxyErrorForAssembly()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType().Assembly, new TestFrameworkImplementation(), logger);

            CollectionAssert.AreEqual(
                new object[] { LoggingLevel.Error },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.IDataRow))
                 select msg.level).ToList()
            );
            Assert.AreEqual(0, actual?.OfType<DataRowProxy>().Count() ?? -1);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        public void DataRowProxyErrorForClass()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType(), new TestFrameworkImplementation(), null, logger);

            CollectionAssert.AreEqual(
                new object[] { LoggingLevel.Error },
                (from msg in logger.Messages select msg.level).ToList()
            );
            Assert.AreEqual(0, actual?.Count ?? -1);
        }

        #region DataRowMockAttribute
        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
        internal sealed class DataRowMockAttribute : Attribute, nfTest.IDataRow
        {
            public DataRowMockAttribute(params object[] methodParameters)
            {
                MethodParameters = methodParameters;
            }

            public object[] MethodParameters
            {
                get;
            }
        }
        #endregion
    }
}
