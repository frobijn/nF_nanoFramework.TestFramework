// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Mark a method that should be called to setup the test context
    /// before the tests. The specified deployment configuration is passed to
    /// the setup method as parameter. The use of this attribute implies that the method
    /// is a setup method; the use of <see cref="SetupAttribute"/> is optional.
    /// The <see cref="ITestClass"/>/<see cref="TestClassAttribute"/> determines whether a
    /// setup method is called before each test method or once before all test methods.
    /// A test class can have at most one setup method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class DeploymentConfigurationAttribute : Attribute, IDeploymentConfiguration
    {
        #region Fields
        private readonly string[] _configurationKeys;
        #endregion

        #region Construction
        /// <summary>
        /// Mark the method as a setup method and specify the deployment configuration to
        /// be passed as parameter to the setup method.
        /// </summary>
        /// <param name="configurationKeys">The keys that identify what part of the deployment configuration
        /// should be passed to the setup method. Each key should have a corresponding
        /// argument of the setup method that is of type <c>byte[]</c> or <c>string</c>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurationKeys"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="configurationKeys"/> is empty</exception>
        /// <remarks>
        /// Due to technical limitations the arguments are of type <c>object</c> rather than <c>string</c>.
        /// </remarks>
        public DeploymentConfigurationAttribute(params object[] configurationKeys)
        {
            if (configurationKeys == null)
            {
                throw new ArgumentNullException($"{nameof(configurationKeys)} can not be null");
            }

            if (configurationKeys.Length == 0)
            {
                throw new ArgumentException($"{nameof(configurationKeys)} can not be empty");
            }
            _configurationKeys = new string[configurationKeys.Length];
            for (int i = 0; i < configurationKeys.Length; i++)
            {
                _configurationKeys[i] = configurationKeys[i].ToString();
            }
        }
        #endregion

        #region IDeploymentConfiguration implementation
        /// <summary>
        /// Get the keys that identify what part of the deployment configuration
        /// should be passed to the setup method.
        /// </summary>
        string[] IDeploymentConfiguration.ConfigurationKeys
            => _configurationKeys;
        #endregion
    }
}
