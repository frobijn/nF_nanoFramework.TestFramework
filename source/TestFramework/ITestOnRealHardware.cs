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
    /// The class implementing <see cref="IAssemblyAttributes"/> can have attributes that implement <see cref="ITestOnRealHardware"/>.
    /// If, for an available test device, one of the attributes
    /// indicates the test can be run on that device, by default all tests in the assembly will be selected for execution.
    /// </para>
    /// <para>
    /// Currently there is no facility to deselection a test for execution on a device if
    /// its test class or the <see cref="IAssemblyAttributes"/> implementation states a test can be executed on that device.
    /// </para>
    /// <para>
    /// For each test method, the attributes that implement <see cref="ITestOnRealHardware"/>
    /// and <see cref="ITestOnVirtualDevice"/> of the method, its test class and the
    /// <see cref="IAssemblyAttributes"/> implementation are collected.
    /// If the set of unique <see cref="ITestOnRealHardware.Description"/> (and "Virtual Device") for
    /// <see cref="ITestOnVirtualDevice"/>) consists of more than one name, a test case is created for each
    /// name. The test framework also adds a test category to test case to make it selectable.
    /// </para>
    /// <para>
    /// If a test method has no (inherited) attributes that implement either <see cref="ITestOnRealHardware"/>
    /// or <see cref="ITestOnVirtualDevice"/>, the test can be executed on the Virtual Device by default and,
    /// if execution on real hardware is allowed, on a single real hardware device.
    /// </para>
    /// </summary>
#if NFTF_REFERENCED_SOURCE_FILE
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
        bool AreDevicesEqual(ITestDevice testDevice1, ITestDevice testDevice2);
    }
}
