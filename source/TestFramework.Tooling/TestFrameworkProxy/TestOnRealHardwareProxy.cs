// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for an attribute that implements <see cref="ITestOnRealHardware"/>
    /// </summary>
    public sealed class TestOnRealHardwareProxy : AttributeProxy
    {
        #region Fields
        private readonly Attribute _attribute;
        private readonly TestFrameworkImplementation _framework;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="framework">Information about the implementation of the test framework</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal TestOnRealHardwareProxy(Attribute attribute, TestFrameworkImplementation framework, Type interfaceType)
        {
            _attribute = attribute;
            _framework = framework;

            TestDeviceProxy.FoundTestFrameworkInterface(_framework, interfaceType);

            if (_framework._property_ITestOnRealHardware_Description is null)
            {
                _framework._property_ITestOnRealHardware_Description = interfaceType.GetProperty(nameof(ITestOnRealHardware.Description));
                if (_framework._property_ITestOnRealHardware_Description is null
                    || _framework._property_ITestOnRealHardware_Description.PropertyType != typeof(string))
                {
                    _framework._property_ITestOnRealHardware_Description = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(ITestOnRealHardware)}.${nameof(ITestOnRealHardware.Description)}");
                }
            }

            if (_framework._method_ITestOnRealHardware_ShouldTestOnDevice is null)
            {
                _framework._method_ITestOnRealHardware_ShouldTestOnDevice = interfaceType.GetMethod(nameof(ITestOnRealHardware.ShouldTestOnDevice));
                if (_framework._method_ITestOnRealHardware_ShouldTestOnDevice is null
                    || _framework._method_ITestOnRealHardware_ShouldTestOnDevice.GetParameters().Length != 1
                    || _framework._method_ITestOnRealHardware_ShouldTestOnDevice.GetParameters()[0].ParameterType.FullName != typeof(ITestDevice).FullName
                    || _framework._method_ITestOnRealHardware_ShouldTestOnDevice.ReturnType != typeof(bool))
                {
                    _framework._method_ITestOnRealHardware_ShouldTestOnDevice = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(ITestOnRealHardware)}.${nameof(ITestOnRealHardware.ShouldTestOnDevice)}");
                }
            }

            if (_framework._property_ITestOnRealHardware_TestOnAllDevices is null)
            {
                _framework._property_ITestOnRealHardware_TestOnAllDevices = interfaceType.GetProperty(nameof(ITestOnRealHardware.TestOnAllDevices));
                if (_framework._property_ITestOnRealHardware_TestOnAllDevices is null
                    || _framework._property_ITestOnRealHardware_TestOnAllDevices.PropertyType != typeof(bool))
                {
                    _framework._property_ITestOnRealHardware_TestOnAllDevices = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(ITestOnRealHardware)}.${nameof(ITestOnRealHardware.TestOnAllDevices)}");
                }
            }
        }
        #endregion

        #region Access to the ITestOnRealHardware interface
        /// <summary>
        /// Get a (short!) description of the devices that are suitable to execute the test on.
        /// This is added to the name of the test.
        /// </summary>
        public string Description
            => (string)_framework._property_ITestOnRealHardware_Description.GetValue(_attribute, null);

        /// <summary>
        /// Indicates whether the test should be executed on the device.
        /// </summary>
        /// <param name="testDevice">Device that is available to execute the test.</param>
        /// <returns>Returns <c>true</c> if the test should be run, <c>false</c> otherwise.</returns>
        public bool ShouldTestOnDevice(TestDeviceProxy testDevice)
             => (bool)_framework._method_ITestOnRealHardware_ShouldTestOnDevice.Invoke(_attribute, new object[]
                {
                    (testDevice ?? throw new ArgumentNullException (nameof (testDevice))).ITestDeviceProxy (_framework)
                });

        /// <summary>
        /// Indicates whether the test should be executed on every available devices for which
        /// <see cref="ITestOnRealHardware.ShouldTestOnDevice(ITestDevice)"/> of this attribute returns <c>true</c>. If the property
        /// is <c>false</c>, the test is executed only on the first of those devices.
        /// </summary>
        public bool TestOnAllDevices
            => (bool)_framework._property_ITestOnRealHardware_TestOnAllDevices.GetValue(_attribute, null);
        #endregion

        #region Helpers
        /// <summary>
        /// Collect all <see cref="TestOnRealHardwareProxy"/> in a dictionary ordered by <see cref="Description"/>.
        /// </summary>
        /// <param name="inherited">Inherited collection of attributes. Pass <c>null</c> if no attributes are inherited.</param>
        /// <param name="attributes">Attributes to add to the collection; may be <c>null</c> if no attributes have to be added.</param>
        /// <returns>The collection of attributes, or <c>null</c> if there are none.</returns>
        public static Dictionary<string, List<TestOnRealHardwareProxy>> Collect(Dictionary<string, List<TestOnRealHardwareProxy>> inherited, IEnumerable<TestOnRealHardwareProxy> attributes)
        {
            if (attributes is null || !attributes.Any())
            {
                return inherited;
            }

            Dictionary<string, List<TestOnRealHardwareProxy>> result = inherited is null ? null : new Dictionary<string, List<TestOnRealHardwareProxy>>(inherited);
            foreach (TestOnRealHardwareProxy testOnRealHardware in attributes)
            {
                result ??= new Dictionary<string, List<TestOnRealHardwareProxy>>();
                if (!result.TryGetValue(testOnRealHardware.Description, out List<TestOnRealHardwareProxy> list))
                {
                    result[testOnRealHardware.Description] = list = new List<TestOnRealHardwareProxy>();
                }
                list.Add(testOnRealHardware);
            }
            return result;
        }
        #endregion
    }
}
