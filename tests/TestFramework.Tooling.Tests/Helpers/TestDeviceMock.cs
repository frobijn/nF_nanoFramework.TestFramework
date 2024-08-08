// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
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
        /// <param name="storageFileContent">A dictionary with file paths as key and content as value</param>
        public TestDeviceMock(string targetName, string platform, Dictionary<string, string> storageFileContent = null)
        {
            _targetName = targetName;
            _platform = platform;
            _storageFileContent = storageFileContent ?? new Dictionary<string, string>();
        }
        private readonly Dictionary<string, string> _storageFileContent;
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

        /// <summary>
        /// Get the content of a file that is stored on the device
        /// as an array of bytes.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Returns the content of the file, or <c>null</c> if the file does not exist
        /// or if the device does not support file storage.</returns>
        public byte[] GetStorageFileContentAsBytes(string filePath)
        {
            if (!_storageFileContent.TryGetValue(filePath, out string content) || string.IsNullOrEmpty(content))
            {
                return null;
            }
            else
            {
                return Encoding.UTF8.GetBytes(content);
            }
        }

        /// <summary>
        /// Get the content of a file that is stored on the device
        /// as a string
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Returns the content of the file, or <c>null</c> if the file does not exist
        /// or if the device does not support file storage.</returns>
        public string GetStorageFileContentAsString(string filePath)
        {
            if (!_storageFileContent.TryGetValue(filePath, out string content) || string.IsNullOrEmpty(content))
            {
                return null;
            }
            else
            {
                return content;
            }
        }
        #endregion
    }
}
