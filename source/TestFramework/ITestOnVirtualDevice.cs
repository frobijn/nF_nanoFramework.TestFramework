// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Interface implemented by attributes that indicate that a test should be executed on the Virtual Device.
    /// <para>
    /// If a test method has attribute(s) that implement <see cref="ITestOnVirtualDevice"/> it will be
    /// selected for execution on the Virtual Device.If not, the test method will be selected for execution
    /// if the default for the test class is to execute the test on the Virtual Device.
    /// </para>
    /// <para>
    /// If a test class has attribute(s) that implement <see cref="ITestOnVirtualDevice"/>, by default
    /// all tests defined by the test class will be selected for execution on the Virtual Device.
    /// </para>
    /// <para>
    /// If the assembly has attribute(s) that implement <see cref="ITestOnVirtualDevice"/>, by default
    /// all tests defined in the assembly will be selected for execution on the Virtual Device.
    /// </para>
    /// <para>
    /// Currently there is no facility to deselection a test for execution on the Virtual Device if the tests of
    /// its test class or its assembly can be executed by default on the Virtual Device.
    /// </para>
    /// <para>
    /// For each test method, the attributes that implement <see cref="ITestOnRealHardware"/>
    /// and <see cref="ITestOnVirtualDevice"/> of the method, its test class and its assembly are collected.
    /// If the set of unique <see cref="ITestOnRealHardware.Description"/> (and "Virtual Device") for
    /// <see cref="ITestOnVirtualDevice"/>) consists of more than one name, a test case is created for each
    /// name. The test framework also adds a trait to test case to make it selectable.
    /// </para>
    /// <para>
    /// If a test method has no (inherited) attributes that implement either <see cref="ITestOnRealHardware"/>
    /// or <see cref="ITestOnVirtualDevice"/>, the test can be executed on the Virtual Device by default and,
    /// if execution on real hardware is allowed, on a single real hardware device.
    /// </para>
    /// </summary>
#if REFERENCED_IN_NFUNITMETADATA
    internal
#else
    public
#endif
     interface ITestOnVirtualDevice
    {
    }
}
