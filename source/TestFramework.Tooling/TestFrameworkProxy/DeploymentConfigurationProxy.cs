// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for a <see cref="IDeploymentConfiguration"/> implementation
    /// </summary>
    public sealed class DeploymentConfigurationProxy : AttributeProxy
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

        #region Helpers
        /// <summary>
        /// Get a list of the configuration data to pass to a method, and the argument type that receives the data.
        /// </summary>
        /// <param name="method">Method this attribute is applied to</param>
        /// <param name="allowExtraElements">Indicates whether the method has other, non-configuration related arguments that follow
        /// the configuration-related arguments</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        /// <returns></returns>
        public IReadOnlyList<(string key, bool asBytes)> GetDeploymentConfigurationArguments(MethodInfo method, bool allowExtraElements, LogMessenger logger)
        {
            var configurationKeys = new List<(string key, bool asBytes)>();

            string[] keys = ConfigurationKeys;
            ParameterInfo[] arguments = method.GetParameters();
            if ((!allowExtraElements && keys.Length < arguments.Length)
                || keys.Length > arguments.Length)
            {
                logger?.Invoke(LoggingLevel.Error, $"{Source?.ForMessage() ?? $"{method.ReflectedType.FullName}.{method.Name}"}: Error: The number of arguments of the method does not match the number of configuration keys specified by the attribute that implements '{nameof(IDeploymentConfiguration)}'.");
            }
            else if ((from a in arguments.Take(keys.Length)
                      where a.ParameterType != typeof(byte[]) && a.ParameterType != typeof(string)
                      select a).Any())
            {
                logger?.Invoke(LoggingLevel.Error, $"{Source?.ForMessage() ?? $"{method.ReflectedType.FullName}.{method.Name}"}: Error: An argument of the method must be of type 'byte[]' or 'string'.");
            }
            else
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    configurationKeys.Add((keys[i], arguments[i].ParameterType == typeof(byte[])));
                }
            }
            return configurationKeys;
        }
        #endregion
    }
}
