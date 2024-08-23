// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Test execution")]
    public sealed class DeploymentConfigurationTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void DeploymentConfiguration_SerializationAndMethods()
        {
            #region Preparation
            string jsonDirectoryPath = Path.Combine(TestDirectoryHelper.GetTestDirectory(TestContext), "MakeAndModel");
            Directory.CreateDirectory(jsonDirectoryPath);
            string testText = "This is text";
            File.WriteAllText(Path.Combine(jsonDirectoryPath, "config.txt"), testText);
            byte[] testBinary = new byte[] { 1, 2, 3, 4, 5, 42 };
            File.WriteAllBytes(Path.Combine(jsonDirectoryPath, "config.bin"), testBinary);
            string specificationFilePath = Path.Combine(jsonDirectoryPath, "sub", "deployment.json");
            Directory.CreateDirectory(Path.GetDirectoryName(specificationFilePath));
            File.WriteAllText(specificationFilePath, @"{
    ""DisplayName"": ""Some device"",
    ""Configuration"":
    {
        ""First key"": ""Some value"",
        ""Second key for file"":
        {
            ""File"": ""../config.txt""
        },
        ""Second value key"": ""Second value"",
        ""Binary file key"":
        {
            ""File"": ""../config.bin""
        },
        ""Missing file key"":
        {
            ""File"": ""config.does.not.exist""
        },
        ""Integer value"": 42,
        ""Integer value as text"": ""42""
    }
}");
            #endregion

            DeploymentConfiguration actual = DeploymentConfiguration.Parse(specificationFilePath);

            Assert.IsNotNull(actual);
            Assert.AreEqual("Some device", actual.DisplayName);

            // Test actual.Values by using the GetDeploymentConfiguration* methods

            Assert.AreEqual("Some value", actual.GetDeploymentConfigurationValue("First key", typeof(string)));
            Assert.AreEqual(testText, actual.GetDeploymentConfigurationValue("Second key for file", typeof(string)));

            Assert.AreEqual(null, actual.GetDeploymentConfigurationValue("No such key", typeof(string)));
            Assert.AreEqual(null, actual.GetDeploymentConfigurationValue("Missing file key", typeof(string)));

            CollectionAssert.AreEqual(testBinary, (byte[])actual.GetDeploymentConfigurationValue("Binary file key", typeof(byte[])));
            Assert.AreEqual(null, actual.GetDeploymentConfigurationValue("Missing file key", typeof(byte[])));

            Assert.AreEqual((int)-1, actual.GetDeploymentConfigurationValue("Missing file key", typeof(int)));
            Assert.AreEqual((long)-1L, actual.GetDeploymentConfigurationValue("Missing file key", typeof(long)));
            Assert.AreEqual(null, actual.GetDeploymentConfigurationValue("Missing file key", typeof(string)));

            Assert.AreEqual((int)42, actual.GetDeploymentConfigurationValue("Integer value", typeof(int)));
            Assert.AreEqual((long)42L, actual.GetDeploymentConfigurationValue("Integer value", typeof(long)));
            Assert.AreEqual("42", actual.GetDeploymentConfigurationValue("Integer value", typeof(string)));

            Assert.AreEqual((int)42, actual.GetDeploymentConfigurationValue("Integer value as text", typeof(int)));
            Assert.AreEqual((long)42L, actual.GetDeploymentConfigurationValue("Integer value as text", typeof(long)));
            Assert.AreEqual("42", actual.GetDeploymentConfigurationValue("Integer value as text", typeof(string)));
        }
    }
}
