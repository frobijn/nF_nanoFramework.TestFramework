// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Instructs the test framework to pass deployment configuration or
    /// "make and model" information to the (setup or test) method. The main application is
    /// for tests of the test class that are executed on real hardware, but it
    /// is also supported for the virtual device.
    /// </summary>
    /// <remarks>
    /// If combined with attributes that implement the <see cref="IDataRow"/> attribute, the
    /// arguments matching the deployment configuration must be the first ones, followed by
    /// the <see cref="IDataRow"/> arguments.
    /// </remarks>
#if NFTF_REFERENCED_SOURCE_FILE
    internal
#else
    public
#endif
     interface IDeploymentConfiguration
    {
        /// <summary>
        /// Get the keys that identify what part of the deployment configuration
        /// should be passed to the setup method. Each key should have a corresponding
        /// argument of the setup method that is of type <c>byte[]</c> or <c>string</c>.
        /// A <c>byte[]</c> value is obtained by calling <see cref="ITestDevice.GetDeploymentConfigurationFile(string)"/>,
        /// a string value by <see cref="ITestDevice.GetDeploymentConfigurationValue(string)"/>.
        /// </summary>
        string[] ConfigurationKeys
        {
            get;
        }
    }
}
