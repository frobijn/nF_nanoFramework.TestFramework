// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for a <see cref="IDeploymentConfiguration"/> implementation
    /// </summary>
    public sealed class DeploymentConfigurationProxy : SetupProxy
    {
        #region Fields
        private readonly object _attribute;
        private readonly TestFrameworkImplementation _framework;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="framework">Information about the implementation of the test framework</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal DeploymentConfigurationProxy(object attribute, TestFrameworkImplementation framework, Type interfaceType)
        {
            _attribute = attribute;
            _framework = framework;

            if (_framework._property_IDeploymentConfiguration_ConfigurationKeys is null)
            {
                _framework._property_IDeploymentConfiguration_ConfigurationKeys = interfaceType.GetProperty(nameof(IDeploymentConfiguration.ConfigurationKeys));
                if (_framework._property_IDeploymentConfiguration_ConfigurationKeys is null
                    || _framework._property_IDeploymentConfiguration_ConfigurationKeys.PropertyType != typeof(string[]))
                {
                    _framework._property_IDeploymentConfiguration_ConfigurationKeys = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(IDeploymentConfiguration)}.${nameof(IDeploymentConfiguration.ConfigurationKeys)}");
                }
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the keys that identify what part of the deployment configuration
        /// should be passed to the setup method. Each key should have a corresponding
        /// argument of the setup method that is of type <c>byte[]</c> or <c>string</c>.
        /// </summary>
        public string[] ConfigurationKeys
            => (string[])_framework._property_IDeploymentConfiguration_ConfigurationKeys.GetValue(_attribute, null);
        #endregion
    }
}
