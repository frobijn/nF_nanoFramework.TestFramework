// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace nanoFramework.TestFramework.Tools
{
    /// <summary>
    /// Proxy for an implementation of the <see cref="ITestDevice"/> that is not
    /// recognizable as an implementation of the interface as it does not implement the interface.
    /// For example, if the code file with <see cref="ITestDevice"/> is added to another project,
    /// that interface will be a different one to the .NET type system as it originates from another
    /// assembly. This proxy can act as a bridge: it is recognized by the test framework code as an
    /// implementation of <see cref="ITestDevice"/>, and delegates all calls to the interface
    /// from the other assembly.
    /// </summary>
#if NFTF_REFERENCED_SOURCE_FILE
    internal
#else
    public
#endif
     sealed class TestDeviceProxy : ITestDevice
    {
        #region Fields
        private readonly object _testDevice;
        private readonly MethodInfo _targetName;
        private readonly MethodInfo _platform;
        private readonly MethodInfo _getDeploymentConfigurationValue;
        private readonly MethodInfo _getDeploymentConfigurationFile;
        #endregion

        #region Construction
        /// <summary>
        /// Create a proxy for an implementation of the <see cref="ITestDevice"/> that uses a different
        /// type instead of <see cref="ITestDevice"/> but with the same signature.
        /// </summary>
        /// <param name="testDevice">Object that implements the <paramref name="otherITestDevice"/> interface or class.</param>
        /// <param name="otherITestDevice">Type that has the same signature <see cref="ITestDevice"/></param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="testDevice"/> is <c>null</c> or if one of the
        /// <see cref="ITestDevice"/> properties/methods is missing from <paramref name="otherITestDevice"/>.</exception>
        public TestDeviceProxy(object testDevice, Type otherITestDevice)
        {
            _testDevice = testDevice;
            _targetName = otherITestDevice.GetMethod(nameof(ITestDevice.TargetName));
            _platform = otherITestDevice.GetMethod(nameof(ITestDevice.Platform));
            _getDeploymentConfigurationValue = otherITestDevice.GetMethod(nameof(ITestDevice.GetDeploymentConfigurationValue));
            _getDeploymentConfigurationFile = otherITestDevice.GetMethod(nameof(ITestDevice.GetDeploymentConfigurationFile));
            if (testDevice is null
                || _targetName is null || _platform is null
                || _getDeploymentConfigurationValue is null
                || _getDeploymentConfigurationFile is null)
            {
                throw new ArgumentException();
            }
        }
        #endregion

        #region ITestDevice implementation
        /// <inheritdoc/>>
        public string TargetName()
            => (string)_targetName.Invoke(_testDevice, null);

        /// <inheritdoc/>>
        public string Platform()
            => (string)_platform.Invoke(_testDevice, null);

        /// <inheritdoc/>>
        public byte[] GetDeploymentConfigurationFile(string filePath)
            => (byte[])_getDeploymentConfigurationFile.Invoke(_testDevice, new object[] { filePath });

        /// <inheritdoc/>>
        public string GetDeploymentConfigurationValue(string filePath)
            => (string)_getDeploymentConfigurationValue.Invoke(_testDevice, new object[] { filePath });
        #endregion
    }
}
