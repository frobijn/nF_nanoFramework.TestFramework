// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Interface implemented by attributes that provide information about the device
    /// a test should be executed on.
    /// <para>
    /// A test method can have attributes that implement <see cref="ITestOnRealHardware"/>.
    /// If, for an available test device, one of the attributes
    /// indicates the test can be run on that device, the test will be selected for execution.
    /// If not, the test method will be selected for execution if the default for the test class is
    /// to execute the test on the device.
    /// </para>
    /// <para>
    /// A test class can have attributes that implement <see cref="ITestOnRealHardware"/>.
    /// If, for an available test device, one of the attributes
    /// indicates the test can be run on that device, by default all tests defined by the test class
    /// will be selected for execution.
    /// </para>
    /// <para>
    /// An assembly can have attributes that implement <see cref="ITestOnRealHardware"/>.
    /// If, for an available test device, one of the attributes
    /// indicates the test can be run on that device, by default all tests in the assembly will be selected for execution.
    /// </para>
    /// <para>
    /// Currently there is no facility to deselection a test for execution on a device if the tests of
    /// its test class or its assembly can be executed by default on that device.
    /// </para>
    /// <para>
    /// For each test method, the attributes that implement <see cref="ITestOnRealHardware"/>
    /// and <see cref="ITestOnVirtualDevice"/> of the method, its test class and its assembly are collected.
    /// If the set of unique <see cref="ITestOnRealHardware.Description"/> (and "Virtual Device") for
    /// <see cref="ITestOnVirtualDevice"/>) consists of more than one name, a test case is created for each
    /// name. The test framework also adds a trait to test case to make it selectable.
    /// </para>
    /// </summary>
#if REFERENCED_IN_NFUNITMETADATA
    internal
#else
    public
#endif
     interface ITestOnRealHardware
    {
        /// <summary>
        /// Get a (short!) description of the devices that are suitable to execute the test on.
        /// This is added to the name of the test.
        /// </summary>
        string Description
        {
            get;
        }

        /// <summary>
        /// Indicates whether the test should be executed on the device.
        /// </summary>
        /// <param name="testDevice">Device that is available to execute the test.</param>
        /// <returns>Returns <c>true</c> if the test should be run, <c>false</c> otherwise.</returns>
        bool ShouldTestOnDevice(ITestDevice testDevice);

        /// <summary>
        /// Indicates whether the test should be executed on every available devices for which
        /// <see cref="ShouldTestOnDevice(ITestDevice)"/> of this attribute returns <c>true</c>. If the property
        /// is <c>false</c>, the test is executed only on the first of those devices.
        /// </summary>
        bool TestOnAllDevices
        {
            get;
        }
    }
}
