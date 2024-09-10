// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.Helpers
{
    /// <summary>
    /// Implementation of the <see cref="ITestDevice"/> interface.
    /// Note that the <see cref="TestDevice"/> must also be part of this project,
    /// as this project simulates an assembly on the nanoCLR platform. Then the <see cref="ITestDevice"/>
    /// and <see cref="TestDevice"/> are both present. The TestFramework.Tooling uses that fact to
    /// create a proxy as an instance of <see cref="TestDevice"/>.
    /// </summary>
    public sealed class TestDeviceMock : ITestDevice
    {
        #region Construction
        /// <summary>
        /// Create an instance that describes the device that is available to execute a test on.
        /// </summary>
        /// <param name="targetName">Target name.</param>
        /// <param name="platform">Target platform.</param>
        /// <param name="deploymentConfiguration">A dictionary with the known configuration keys and value</param>
        public TestDeviceMock(string targetName, string platform, Dictionary<string, object> deploymentConfiguration = null)
        {
            _targetName = targetName;
            _platform = platform;
            _deploymentConfiguration = deploymentConfiguration ?? new Dictionary<string, object>();
        }
        private readonly Dictionary<string, object> _deploymentConfiguration;
        #endregion

        #region ITestDevice implementation
        /// <summary>
        /// Target name.
        /// </summary>
        public string TargetName()
            => _targetName;
        private readonly string _targetName;

        /// <summary>
        /// Target platform.
        /// </summary>
        public string Platform()
            => _platform;
        private readonly string _platform;

        /// <inheritdoc/>
        public object GetDeploymentConfigurationValue(string filePath, Type resultType)
        {
            if (!_deploymentConfiguration.TryGetValue(filePath, out object content))
            {
                if (resultType == typeof(int))
                {
                    return (int)-1;
                }
                else if (resultType == typeof(long))
                {
                    return (long)-1L;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return content;
            }
        }
        #endregion
    }
}
