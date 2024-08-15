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
            string json = @"{
    ""DisplayName"": ""Some device"",
    ""Configuration"":
    {
        ""First key"": ""Some value"",
        ""Second key for file"":
        {
            ""File"": ""config.txt""
        },
        ""Second value key"": ""Second value"",
        ""Binary file key"":
        {
            ""File"": ""config.bin""
        },
        ""Missing file key"":
        {
            ""File"": ""config.does.not.exist""
        }
    }
}";
            #endregion

            DeploymentConfiguration actual = DeploymentConfiguration.Parse(json, jsonDirectoryPath);

            string actualJson = actual.ToJson();
            Assert.AreEqual(@"{
  ""DisplayName"": ""Some device"",
  ""Configuration"": {
    ""First key"": ""Some value"",
    ""Second value key"": ""Second value"",
    ""Binary file key"": {
      ""File"": ""config.bin""
    },
    ""Missing file key"": {
      ""File"": ""config.does.not.exist""
    },
    ""Second key for file"": {
      ""File"": ""config.txt""
    }
  }
}".Replace("\r\n", "\n") + '\n',
                actualJson?.Replace("\r\n", "\n") + '\n');

            Assert.AreEqual("Some device", actual.DisplayName);

            Assert.AreEqual("Some value", actual.GetDeploymentConfigurationValue("First key"));
            Assert.AreEqual(testText, actual.GetDeploymentConfigurationValue("Second key for file"));

            Assert.AreEqual(null, actual.GetDeploymentConfigurationValue("Nu such key"));
            Assert.AreEqual(null, actual.GetDeploymentConfigurationValue("Missing file key"));

            CollectionAssert.AreEqual(testBinary, actual.GetDeploymentConfigurationFile("Binary file key"));
            Assert.AreEqual(null, actual.GetDeploymentConfigurationValue("Missing file key"));
        }
    }
}
