// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;

namespace nanoFramework.TestFramework.Tooling
{
    public sealed class TestCase
    {
        #region Fields
        private readonly HashSet<string> _traits;
        private readonly List<TestOnRealHardwareProxy> _testOnRealHardware;
        #endregion

        #region Construction
        internal TestCase(int testIndex,
            string assemblyFilePath,
            TestCaseGroup group,
            MethodInfo method, string displayName,
            ProjectSourceInventory.ElementDeclaration location,
            bool shouldRunOnVirtualDevice,
            List<TestOnRealHardwareProxy> testOnRealHardware,
            HashSet<string> traits, params string[] extraTraits)
        {
            AssemblyFilePath = assemblyFilePath;
            TestIndex = testIndex;
            DisplayName = displayName;
            FullyQualifiedName = $"{method.ReflectedType.FullName}.{method.Name}";
            TestMethodSourceCodeLocation = location;
            ShouldRunOnVirtualDevice = shouldRunOnVirtualDevice;
            _testOnRealHardware = testOnRealHardware;
            if (extraTraits.Length > 0)
            {
                _traits = new HashSet<string>(traits);
                _traits.UnionWith(extraTraits);
            }
            else
            {
                _traits = traits;
            }
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
        public bool ShouldRunOnVirtualDevice
        {
            get;
        }

        /// <summary>
        /// Indicates whether the test should be executed on real hardware. Call <see cref="SelectDevicesForExecution(IEnumerable{ITestDevice})"/>
        /// to determine whether real hardware is suited to execute the test.
        /// </summary>
        public bool ShouldRunOnRealHardware
            => !(_testOnRealHardware is null);

        /// <summary>
        /// Get the 1-based index of the test case (in the set of all test cases in a collection of test assemblies).
        /// The index matches the index that as determined by the test runner when it enumerates the tests
        /// in the assemblies (in the same order).
        /// </summary>
        public int TestIndex
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
    }
}
