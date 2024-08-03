// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for an attribute that implements <see cref="ITestOnRealHardware"/>
    /// </summary>
    public sealed class TestOnRealHardwareProxy : AttributeProxy
    {
        #region Fields
        private readonly Attribute _attribute;
        private static PropertyInfo s_description;
        private static MethodInfo s_shouldTestOnDevice;
        private static PropertyInfo s_testOnAllDevices;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal TestOnRealHardwareProxy(Attribute attribute, Type interfaceType)
        {
            TestDeviceProxy.FoundITestDevice(interfaceType);
            _attribute = attribute;

            if (s_description is null)
            {
                s_description = interfaceType.GetProperty(nameof(ITestOnRealHardware.Description));
                if (s_description is null
                    || s_description.PropertyType != typeof(string))
                {
                    s_description = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(ITestOnRealHardware)}.${nameof(ITestOnRealHardware.Description)}");
                }
            }

            if (s_shouldTestOnDevice is null)
            {
                s_shouldTestOnDevice = interfaceType.GetMethod(nameof(ITestOnRealHardware.ShouldTestOnDevice));
                if (s_shouldTestOnDevice is null
                    || s_shouldTestOnDevice.GetParameters().Length != 1
                    || s_shouldTestOnDevice.GetParameters()[0].ParameterType.FullName != typeof(ITestDevice).FullName
                    || s_shouldTestOnDevice.ReturnType != typeof(bool))
                {
                    s_shouldTestOnDevice = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(ITestOnRealHardware)}.${nameof(ITestOnRealHardware.ShouldTestOnDevice)}");
                }
            }

            if (s_testOnAllDevices is null)
            {
                s_testOnAllDevices = interfaceType.GetProperty(nameof(ITestOnRealHardware.TestOnAllDevices));
                if (s_testOnAllDevices is null
                    || s_testOnAllDevices.PropertyType != typeof(bool))
                {
                    s_testOnAllDevices = null;
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
            => (string)s_description.GetValue(_attribute, null);

        /// <summary>
        /// Indicates whether the test should be executed on the device.
        /// </summary>
        /// <param name="testDevice">Device that is available to execute the test.</param>
        /// <returns>Returns <c>true</c> if the test should be run, <c>false</c> otherwise.</returns>
        public bool ShouldTestOnDevice(TestDeviceProxy testDevice)
             => (bool)s_shouldTestOnDevice.Invoke(_attribute, new object[]
                {
                    (testDevice ?? throw new ArgumentNullException (nameof (testDevice))).ITestDeviceProxy
                });

        /// <summary>
        /// Indicates whether the test should be executed on every available devices for which
        /// <see cref="ITestOnRealHardware.ShouldTestOnDevice(ITestDevice)"/> of this attribute returns <c>true</c>. If the property
        /// is <c>false</c>, the test is executed only on the first of those devices.
        /// </summary>
        public bool TestOnAllDevices
            => (bool)s_testOnAllDevices.GetValue(_attribute, null);
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
