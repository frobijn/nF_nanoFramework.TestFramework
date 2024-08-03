// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
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
        /// <summary>
        /// Create the test case(s) based on the description of the method
        /// </summary>
        /// <param name="method"></param>
        /// <param name="group"></param>
        /// <param name="attributes"></param>
        /// <param name="testClassTestOnVirtualDevice"></param>
        /// <param name="testClassTestOnRealHardwareProxy"></param>
        /// <param name="testClassRunInParallel"></param>
        /// <param name="methodSourceLocation"></param>
        /// <param name="testIndex"></param>
        /// <param name="logger">Method to pass a message to the caller</param>
        /// <returns></returns>
        internal static List<TestCase> Create(MethodInfo method,
            TestCaseGroup group,
            List<AttributeProxy> attributes,
            bool testClassTestOnVirtualDevice,
            Dictionary<string, List<TestOnRealHardwareProxy>> testClassTestOnRealHardwareProxy,
            bool testClassRunInParallel,
            ProjectSourceInventory.ElementDeclaration methodSourceLocation,
            ref int testIndex,
            LogMessenger logger)
        {
            var result = new List<TestCase>();

            var traits = new HashSet<string>();
            foreach (TraitsProxy attribute in attributes.OfType<TraitsProxy>())
            {
                traits.UnionWith(attribute.Traits);
            }

            bool runInIsolation = !RunInParallelProxy.RunInParallel(attributes.OfType<RunInParallelProxy>(), testClassRunInParallel);

            bool testOnVirtualDevice = testClassTestOnVirtualDevice || attributes.OfType<TestOnVirtualDeviceProxy>().Any();
            Dictionary<string, List<TestOnRealHardwareProxy>> testOnRealHardware = TestOnRealHardwareProxy.Collect(testClassTestOnRealHardwareProxy, attributes.OfType<TestOnRealHardwareProxy>());
            int deviceTypeCount = (testOnRealHardware?.Count ?? 0) + (testOnVirtualDevice ? 1 : 0);
            if (deviceTypeCount == 0)
            {
                string methodInSource = methodSourceLocation is null
                    ? $"{method.ReflectedType.Assembly.GetName().Name}:{method.ReflectedType.FullName}.{method.Name}"
                    : methodSourceLocation.ForMessage();
                logger?.Invoke(LoggingLevel.Error, $"{methodInSource}: method, class and assembly have no attributes to indicate on what device the test should be run.");
            }
            else
            {
                var dataRowParameters = (from dataRow in attributes.OfType<DataRowProxy>()
                                         select (dataRow.Source, dataRow.MethodParametersAsString)).ToList();
                if (dataRowParameters.Count == 0)
                {
                    dataRowParameters.Add((methodSourceLocation, ""));
                }
                foreach ((ProjectSourceInventory.ElementDeclaration Source, string MethodParametersAsString) in dataRowParameters)
                {
                    if (testOnVirtualDevice)
                    {
                        result.Add(new TestCase(
                            ref testIndex,
                            group,
                            method, $"{method.Name}{Source}{(deviceTypeCount > 1 ? " [Virtual Device]" : "")}",
                            methodSourceLocation,
                            true, null,
                            runInIsolation,
                            traits, "@Virtual Device"
                        ));
                    }
                    if (!(testOnRealHardware is null))
                    {
                        foreach (KeyValuePair<string, List<TestOnRealHardwareProxy>> device in testOnRealHardware)
                        {
                            string deviceTypeName = testOnRealHardware.Keys.First();
                            result.Add(new TestCase(
                                ref testIndex,
                                group,
                                method, $"{method.Name}{Source}{(deviceTypeCount > 1 ? $" [{deviceTypeName}]" : "")}",
                                methodSourceLocation,
                                false, device.Value,
                                runInIsolation,
                                traits, $"@{deviceTypeName}"
                            ));
                        }
                    }
                    testIndex++;
                }
            }

            return result;
        }

        private TestCase(ref int testIndex,
            TestCaseGroup group,
            MethodInfo method, string displayName,
            ProjectSourceInventory.ElementDeclaration location,
            bool shouldRunOnVirtualDevice,
            List<TestOnRealHardwareProxy> testOnRealHardware,
            bool runInIsolation,
            HashSet<string> traits, string extraTrait)
        {
            TestIndex = ++testIndex;
            DisplayName = displayName;
            FullyQualifiedName = $"{method.ReflectedType.FullName}.{method.Name}";
            TestMethodSourceCodeLocation = location;
            ShouldRunOnVirtualDevice = shouldRunOnVirtualDevice;
            _testOnRealHardware = testOnRealHardware;
            RunInIsolation = runInIsolation;
            _traits = extraTrait is null
                ? traits
                : new HashSet<string>(traits)
                    {
                        extraTrait
                    };
            Group = group;
            Group._testCases.Add(this);
        }
        #endregion

        #region Properties
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

        /// <summary>
        /// Indicates whether the test should be executed in isolation and not run in parallel with other
        /// tests, even if the device would allow for that. If this is <c>false</c>, the <see cref="Group"/>
        /// specifies how the test should be executed.
        /// </summary>
        public bool RunInIsolation
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
