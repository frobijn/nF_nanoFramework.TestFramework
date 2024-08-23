// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Representation of a device that is available or selected to run the tests on.
    /// </summary>
    public sealed class TestDevice : ITestDevice
    {
        #region Fields
        private readonly DeploymentConfiguration _configuration;
        #endregion

        #region Construction
        /// <summary>
        /// Create a description of a hardware device
        /// </summary>
        /// <param name="target">Target that denotes the firmware installed on the device.</param>
        /// <param name="platform">Platform that describes the family the device belongs to.</param>
        /// <param name="configuration">Deployment configuration for the device, or <c>null</c>
        /// if no configuration is available.</param>
        public TestDevice(string target, string platform, DeploymentConfiguration configuration)
        {
            Target = target;
            Platform = platform;
            _configuration = configuration;
        }

        /// <summary>
        /// Create a description of a virtual device
        /// </summary>
        /// <param name="configuration">Deployment configuration for the device, or <c>null</c>
        /// if no configuration is available.</param>
        public TestDevice(DeploymentConfiguration configuration)
            : this("nanoCLR", "Virtual Device", configuration)
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// Target that denotes the firmware installed on the device.
        /// </summary>
        public string Target
        {
            get;
        }

        /// <summary>
        /// Platform that describes the family the device belongs to.
        /// </summary>
        public string Platform
        {
            get;
        }
        #endregion

        #region ITestDevice implementation
        /// <inheritdoc/>
        string ITestDevice.TargetName()
            => Target;

        /// <inheritdoc/>
        string ITestDevice.Platform()
            => Platform;

        /// <inheritdoc/>
        public object GetDeploymentConfigurationValue(string configurationKey, Type resultType)
            => _configuration?.GetDeploymentConfigurationValue(configurationKey, resultType);
        #endregion
    }
}
