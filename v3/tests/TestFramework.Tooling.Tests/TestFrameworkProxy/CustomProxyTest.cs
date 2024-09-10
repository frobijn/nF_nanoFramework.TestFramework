// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;

using nfTest = nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{
    [CustomProxyTest.Custom]
    public sealed class CustomProxyTest_AssemblyAttributes : nfTest.IAssemblyAttributes
    {
    }

    [TestClass]
    [Custom]
    public sealed class CustomProxyTest
    {
        [TestMethod]
        [TestCategory("nF test attributes")]
        [Custom]
        public void CustomProxy_Test()
        {
            AttributeProxy.Register(typeof(CustomAttribute).FullName, false, (a, f, t) => new CustomAttributeProxy(), true, true, true);
            AttributeProxy.Register(typeof(ICustomInterface).FullName, true, (a, f, t) => new CustomInterfaceProxy(), true, true, true);

            // For assembly
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetAssemblyAttributeProxies(GetType().Assembly, new TestFrameworkImplementation(), null);

            Assert.AreEqual(0, actual.OfType<CustomAttributeProxy>().Count());
            Assert.AreEqual(1, custom.OfType<CustomAttributeProxy>().Count());
            Assert.AreEqual(0, actual.OfType<CustomInterfaceProxy>().Count());
            Assert.AreEqual(1, custom.OfType<CustomInterfaceProxy>().Count());

            // For class
            (actual, custom) = AttributeProxy.GetClassAttributeProxies(GetType(), new TestFrameworkImplementation(), null, null);

            Assert.AreEqual(0, actual.OfType<CustomAttributeProxy>().Count());
            Assert.AreEqual(1, custom.OfType<CustomAttributeProxy>().Count());
            Assert.AreEqual(0, actual.OfType<CustomInterfaceProxy>().Count());
            Assert.AreEqual(1, custom.OfType<CustomInterfaceProxy>().Count());

            // For method
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            (actual, custom) = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, null);

            Assert.AreEqual(0, actual.OfType<CustomAttributeProxy>().Count());
            Assert.AreEqual(1, custom.OfType<CustomAttributeProxy>().Count());
            Assert.AreEqual(0, actual.OfType<CustomInterfaceProxy>().Count());
            Assert.AreEqual(1, custom.OfType<CustomInterfaceProxy>().Count());
        }

        public sealed class CustomAttributeProxy : AttributeProxy
        {
        }

        public sealed class CustomInterfaceProxy : AttributeProxy
        {
        }

        public interface ICustomInterface
        {
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        public sealed class CustomAttribute : Attribute, ICustomInterface
        {
        }
    }
}
