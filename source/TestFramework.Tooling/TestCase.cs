// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;

namespace nanoFramework.TestFramework.Tooling
{
    public sealed class TestCase
    {
        #region Fields
        private readonly HashSet<string> _traits;
        private readonly IEnumerable<TestOnRealHardwareProxy> _testOnRealHardware;
        #endregion

        #region Construction
        internal TestCase(string testCaseId,
            int dataRowIndex,
            string assemblyFilePath,
            TestCaseGroup group,
            MethodInfo method, string displayName,
            ProjectSourceInventory.ElementDeclaration location,
            bool shouldRunOnVirtualDevice,
            IEnumerable<TestOnRealHardwareProxy> testOnRealHardware,
            IReadOnlyList<(string key, Type valueType)> requiredConfigurationKeys,
            HashSet<string> traits)
        {
            AssemblyFilePath = assemblyFilePath;
            TestCaseId = testCaseId;
            DataRowIndex = dataRowIndex;
            DisplayName = displayName;
            FullyQualifiedName = $"{method.ReflectedType.FullName}.{method.Name}";
            TestMethodSourceCodeLocation = location;
            ShouldRunOnVirtualDevice = shouldRunOnVirtualDevice;
            _testOnRealHardware = testOnRealHardware;
            RequiredConfigurationKeys = requiredConfigurationKeys ?? new (string, Type)[] { };
            _traits = traits;
            Group = group;
            Group._testCases.Add(this);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the assembly the test case resides in
        /// </summary>
        public string AssemblyFilePath
        {
            get;
        }

        /// <summary>
        /// Name of the test method to view in, e.g., the Test Explorer
        /// </summary>
        public string DisplayName
        {
            get;
        }

        /// <summary>
        /// The fully qualified name of the test method within its assembly
        /// </summary>
        public string FullyQualifiedName
        {
            get;
        }

        /// <summary>
        /// Get the most appropriate location to point a user to, if the user wants to navigate to the test.
        /// Is <c>null</c> if the method cannot be found in the source code
        /// </summary>
        public ProjectSourceInventory.ElementDeclaration TestMethodSourceCodeLocation
        {
            get;
        }

        /// <summary>
        /// The traits that describe the test
        /// </summary>
        public IReadOnlyCollection<string> Traits
            => _traits;

        /// <summary>
        /// Indicates whether the test should be executed on the Virtual Device.
        /// </summary>
        /// <remarks>
        /// Either <see cref="ShouldRunOnVirtualDevice"/> or <see cref="ShouldRunOnRealHardware"/>
        /// is true, not both.
        /// </remarks>
        public bool ShouldRunOnVirtualDevice
        {
            get;
        }

        /// <summary>
        /// Indicates whether the test should be executed on real hardware. Call <see cref="SelectDevicesForExecution(IEnumerable{ITestDevice})"/>
        /// to determine whether real hardware is suited to execute the test.
        /// </summary>
        /// <remarks>
        /// Either <see cref="ShouldRunOnVirtualDevice"/> or <see cref="ShouldRunOnRealHardware"/>
        /// is true, not both.
        /// </remarks>
        public bool ShouldRunOnRealHardware
            => !(_testOnRealHardware is null);

        /// <summary>
        /// Get the attributes that determine whether the test case can be executed on a real hardware device.
        /// </summary>
        public IEnumerable<TestOnRealHardwareProxy> RealHardwareDeviceSelectors
            => _testOnRealHardware ?? new TestOnRealHardwareProxy[0];

        /// <summary>
        /// Get the keys that identify what part of the deployment configuration
        /// should be passed to the test method. Each key should have a corresponding
        /// argument of the setup method that is of type <c>byte[]</c> or <c>string</c>,
        /// as indicated for the key.
        /// </summary>
        /// <remarks>
        /// Additional deployment configuration information may be required to initialise
        /// the test case group; see <see cref="TestCaseGroup.RequiredConfigurationKeys"/>.
        /// </remarks>
        public IReadOnlyList<(string key, Type valueType)> RequiredConfigurationKeys
        {
            get;
            internal set;
        }

        /// <summary>
        /// Get the 0-based index of the <see cref="IDataRow"/> that is related to this test case,
        /// </summary>
        /// <remarks>
        /// In the current implementation, the index is the position of the attribute implementing
        /// <see cref="IDataRow"/> in the list of attributes that implement <see cref="IDataRow"/>.
        /// If the test case does not correspond to a <see cref="IDataRow"/>, the index is -1.
        /// </remarks>
        public int DataRowIndex
        {
            get;
        }

        /// <summary>
        /// Get the group of test cases this case is part of
        /// </summary>
        public TestCaseGroup Group
        {
            get;
        }

        /// <summary>
        /// Get a unique identification of this test case among all test cases for the assembly,
        /// if the device is ignored (the equivalent test on another device has the same identifier).
        /// </summary>
        public string TestCaseId
        {
            get;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Selects the (real hardware) devices among the available devices the test should be executed on.
        /// </summary>
        /// <param name="devices">Description of the devices that are available</param>
        /// <returns>A selection of the devices the test should be run on.</returns>
        /// <remarks>
        /// Regardless of the result from this method, the test should be run on the Virtual Device if
        /// <see cref="ShouldRunOnVirtualDevice"/> is <c>true</c>.
        /// </remarks>
        public IEnumerable<TestDeviceProxy> SelectDevicesForExecution(IEnumerable<TestDeviceProxy> devices)
        {
            if (!(_testOnRealHardware is null))
            {
                foreach (TestDeviceProxy device in devices)
                {
                    foreach (TestOnRealHardwareProxy attribute in _testOnRealHardware)
                    {
                        if (attribute.ShouldTestOnDevice(device))
                        {
                            yield return device;
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Remove the device or device type identification from the
        /// display name.
        /// </summary>
        /// <param name="displayName">Display name of a test case</param>
        public static string DisplayNameWithoutDevice(string displayName)
        {
            if (displayName?.EndsWith("]") ?? false)
            {
                int idx = displayName.LastIndexOf('[');
                return displayName.Substring(0, idx).Trim();
            }
            return displayName;
        }
        #endregion
    }
}
