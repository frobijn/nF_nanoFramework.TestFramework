// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// A proxy to the <see cref="nanoFramework.TestFramework.TestDevice"/>
    /// class that can be passed to attributes of the nanoCLR-platform
    /// that take an <see cref="ITestDevice"/> interface as argument.
    /// </summary>
    public class TestDeviceProxy
    {
        #region Fields
        private readonly ITestDevice _device;
        private readonly Dictionary<TestFrameworkImplementation, object> _proxies = new Dictionary<TestFrameworkImplementation, object>();
        #endregion

        #region Construction / initialisation
        /// <summary>
        /// This method has to be called by code that discovers an interface from the test framework.
        /// It is used to find the <see cref="nanoFramework.TestFramework.Tools.TestDeviceProxy"/> type
        /// in the assembly that implements the test framework.
        /// </summary>
        /// <param name="framework">Information about the implementation of the test framework</param>
        /// <param name="interfaceType">One of the interface types defined in the test framework</param>
        internal static void FoundTestFrameworkInterface(TestFrameworkImplementation framework, Type interfaceType)
        {
            framework._type_TestDeviceProxy ??= (from type in interfaceType.Assembly.GetTypes()
                                                 where type.FullName == typeof(nanoFramework.TestFramework.Tools.TestDeviceProxy).FullName
                                                 select type).FirstOrDefault();
        }

        /// <summary>
        /// Create a proxy on the nanoCLR-platform that obtains its data from a device defined for the .NET platform,
        /// </summary>
        /// <param name="testDevice">Instance of a class that implements the <see cref="ITestDevice"/> interface</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testDevice"/> is <c>null</c> or if it does not
        /// implement the <see cref="ITestDevice"/> interface.</exception>
        /// <exception cref="FrameworkMismatchException">Thrown if there is a mismatch detected in the test framework. Make sure
        /// to use matching versions of this application and the nanoFramework.TestFramework.</exception>
        public TestDeviceProxy(ITestDevice testDevice)
        {
            if (testDevice is null)
            {
                throw new ArgumentNullException(nameof(testDevice));
            }
            _device = testDevice;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Platform that describes the family the device belongs to.
        /// </summary>
        public string Platform
            => _device.Platform();

        /// <summary>
        /// Target name that denotes the firmware installed on the device.
        /// </summary>
        public string TargetName
            => _device.TargetName();
        #endregion

        #region Methods
        /// <summary>
        /// Get the instance that implements the <see cref="ITestDevice"/> interface on the nanoCLR
        /// platform and that passes method calls to the <see cref="ITestDevice"/> implementation on the .NET platform.
        /// </summary>
        /// <param name="framework">Information about the implementation of the test framework</param>
        internal object ITestDeviceProxy(TestFrameworkImplementation framework)
        {
            if (!_proxies.TryGetValue(framework, out object proxy))
            {
                if (framework._type_TestDeviceProxy is null)
                {
                    throw new FrameworkMismatchException($"{nameof(TestDeviceProxy)} is not found in the nanoFramework assembly");
                }
                try
                {
                    proxy = Activator.CreateInstance(framework._type_TestDeviceProxy, _device, typeof(ITestDevice));
                }
                catch (Exception ex)
                {
                    if (ex.GetType().FullName == typeof(ArgumentException).FullName)
                    {
                        // The proxy cannot find the ITestDevice properties/methods
                        throw new FrameworkMismatchException($"The definition of {nameof(ITestDevice)} in this application does not match the one in the nanoFramework assembly.");
                    }
                    else
                    {
                        throw;
                    }
                }
                _proxies[framework] = proxy;
            }
            return proxy;
        }
        #endregion
    }
}
