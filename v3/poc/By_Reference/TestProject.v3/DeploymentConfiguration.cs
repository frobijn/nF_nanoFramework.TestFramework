// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace NFUnitTest
{
    [TestClass]
    public class DeploymentConfiguration
    {
        [Setup]
        [DeploymentConfiguration("RGB LED I/O port")]
        public void Setup(int rgbIoPort)
        {
            OutputHelper.WriteLine($"If no deployment information is present, the argument is -1");
            Assert.AreNotEqual(-1, rgbIoPort, "Fails on the Virtual nanoDevice");
        }

        [TestOnRealHardware]
        [DeploymentConfiguration("SSID name")]
        public void TestMethodWithConfigurationData(string ssidName)
        {
            Assert.IsNotNull(ssidName, "Fails if no deployment configuration is configured for the nanoDevice");
        }

        [TestOnRealHardware]
        [DeploymentConfiguration("DevBoard configuration")]
        [DataRow(42)]
        [DataRow(123)]
        public void DataRowWithConfigurationData(byte[] devBoardConfiguration, int testData)
        {
            OutputHelper.WriteLine($"Test data: {testData}");
            Assert.IsNotNull(devBoardConfiguration, "Fails if no deployment configuration is configured for the nanoDevice and the no file is specified for 'DevBoard configuration', or that file does not exist");
        }


        [TestIfConfigurationIsPresent("SSID name"),
            DeploymentConfiguration("SSID name")]
        [TestCategory("Framework extensions")]
        public void TestMethodOnlyRunIfConfigurationIsPresent(string ssidName)
        {
            Assert.IsNotNull(ssidName, "Cannot happen: for Virtual nanoDevice the Setup fails, for real hardware [TestIfConfigurationIsPresent] prevents execution");
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class TestIfConfigurationIsPresentAttribute : Attribute, ITestOnRealHardware
    {
        /// <summary>
        /// Indicate that a test can only be run if binary data is available for the specified
        /// deployment configuration key.
        /// </summary>
        /// <param name="deploymentConfigurationKey"></param>
        public TestIfConfigurationIsPresentAttribute(string deploymentConfigurationKey)
        {
            _deploymentConfigurationKey = deploymentConfigurationKey;
        }
        private readonly string _deploymentConfigurationKey;


        string ITestOnRealHardware.Description
            => $"Any device for which '{_deploymentConfigurationKey}' is available.";

        bool ITestOnRealHardware.AreDevicesEqual(ITestDevice testDevice1, ITestDevice testDevice2)
            => true;

        bool ITestOnRealHardware.ShouldTestOnDevice(ITestDevice testDevice)
            => testDevice.GetDeploymentConfigurationValue(_deploymentConfigurationKey, typeof(string)) is not null;
    }

}
