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
        private readonly object _attribute;
        private readonly TestFrameworkImplementation _framework;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="framework">Information about the implementation of the test framework</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal TestOnRealHardwareProxy(object attribute, TestFrameworkImplementation framework, Type interfaceType)
        {
            _attribute = attribute;
            _framework = framework;

            _framework.FoundTestFrameworkInterface(interfaceType);

            _framework.AddProperty<string>(typeof(ITestOnRealHardware).FullName, interfaceType, nameof(ITestOnRealHardware.Description));
            _framework.AddMethod<bool>(typeof(ITestOnRealHardware).FullName, interfaceType, nameof(ITestOnRealHardware.ShouldTestOnDevice), _framework.ITestDeviceType);
            _framework.AddMethod<bool>(typeof(ITestOnRealHardware).FullName, interfaceType, nameof(ITestOnRealHardware.AreDevicesEqual), _framework.ITestDeviceType, _framework.ITestDeviceType);
        }
        #endregion

        #region Access to the ITestOnRealHardware interface
        /// <summary>
        /// Get a (short!) description of the devices that are suitable to execute the test on.
        /// This is added to the name of the test.
        /// </summary>
        public string Description
            => _framework.GetPropertyValue<string>(typeof(ITestOnRealHardware).FullName, nameof(ITestOnRealHardware.Description), _attribute);

        /// <summary>
        /// Indicates whether the test should be executed on the device.
        /// </summary>
        /// <param name="testDevice">Device that is available to execute the test.</param>
        /// <returns>Returns <c>true</c> if the test should be run, <c>false</c> otherwise.</returns>
        public bool ShouldTestOnDevice(TestDeviceProxy testDevice)
             => _framework.CallMethod<bool>(
                    typeof(ITestOnRealHardware).FullName, nameof(ITestOnRealHardware.ShouldTestOnDevice),
                    _attribute,
                    (testDevice ?? throw new ArgumentNullException(nameof(testDevice))).ITestDeviceProxy(_framework)
                 );

        /// <summary>
        /// Indicates whether two test devices are considered similar enough that the
        /// test should be executed only on one of both devices.
        /// </summary>
        /// <param name="testDevice1">First device.</param>
        /// <param name="testDevice2">Second device.</param>
        /// <returns>Returns <c>true</c> if it is sufficient to execute the test
        /// on one of the devices. Returns <c>false</c> if the devices are sufficiently
        /// different that the test should be executed on both devices.</returns>
        /// <remarks>
        /// For each of the devices <see cref="ShouldTestOnDevice"/> has been called and
        /// has returned <c>true</c>.
        /// </remarks>
        public bool AreDevicesEqual(TestDeviceProxy testDevice1, TestDeviceProxy testDevice2)
            => (bool)_framework.CallMethod(
                    typeof(ITestOnRealHardware).FullName, nameof(ITestOnRealHardware.AreDevicesEqual),
                    _attribute,
                    (testDevice1 ?? throw new ArgumentNullException(nameof(testDevice1))).ITestDeviceProxy(_framework),
                    (testDevice2 ?? throw new ArgumentNullException(nameof(testDevice2))).ITestDeviceProxy(_framework)
                );
        #endregion

        #region Helpers
        /// <summary>
        /// Collect all <see cref="TestOnRealHardwareProxy"/> in a dictionary ordered by <see cref="Description"/>.
        /// </summary>
        /// <param name="inherited">Inherited collection of attributes. Pass <c>null</c> if no attributes are inherited.</param>
        /// <param name="attributes">Attributes to add to the collection; may be <c>null</c> if no attributes have to be added.</param>
        /// <returns>The collection of attributes, or <c>null</c> if there are none.</returns>
        internal static (HashSet<string> descriptions, List<TestOnRealHardwareProxy> attributes) Collect((HashSet<string> descriptions, List<TestOnRealHardwareProxy> attributes) inherited, IEnumerable<TestOnRealHardwareProxy> attributes)
        {
            if (attributes is null || !attributes.Any())
            {
                return inherited;
            }

            (HashSet<string>, List<TestOnRealHardwareProxy>) result = (
                inherited.descriptions is null ? null : new HashSet<string>(inherited.descriptions),
                inherited.attributes is null ? null : new List<TestOnRealHardwareProxy>(inherited.attributes)
            );

            foreach (TestOnRealHardwareProxy testOnRealHardware in attributes)
            {
                result = (
                    result.Item1 ??= new HashSet<string>(),
                    result.Item2 ??= new List<TestOnRealHardwareProxy>()
                );
                result.Item1.Add(testOnRealHardware.Description ?? "");
                result.Item2.Add(testOnRealHardware);
            }
            return result;
        }
        #endregion
    }
}
