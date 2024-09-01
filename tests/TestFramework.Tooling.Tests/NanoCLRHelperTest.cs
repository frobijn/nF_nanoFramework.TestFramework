// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Test execution")]
    [TestCategory("Virtual device")]
    public sealed class NanoCLRHelperTest
    {
        public TestContext TestContext { get; set; }

        private static readonly Regex s_replaceVersion = new Regex(@"v[0-9]+(\.[0-9]+)+", RegexOptions.Compiled);


        [TestMethod]
        public void NanoCLR_Local_Download()
        {
            string nanoCLRFilePath = Path.Combine(TestDirectoryHelper.GetTestDirectory(TestContext), "nanoclr.exe");
            var logger = new LogMessengerMock();

            var actual = new NanoCLRHelper(nanoCLRFilePath, null, true, logger);

            Assert.AreEqual(
$@"Verbose: Install/update nanoclr tool
Verbose: Install/update successful. Running Vx
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {s_replaceVersion.Replace(m.message, "Vx")}"
                    ) + '\n'
            );
            Assert.AreEqual(nanoCLRFilePath, actual.NanoCLRFilePath);
            Assert.IsTrue(File.Exists(nanoCLRFilePath));
            Assert.AreEqual(true, actual.NanoClrIsInstalled);
        }

        [TestMethod]
        public void NanoCLR_Local_NotAvailable()
        {
            string nanoCLRFilePath = Path.Combine(TestDirectoryHelper.GetTestDirectory(TestContext), "nanoclr.exe");
            var logger = new LogMessengerMock();

            var actual = new NanoCLRHelper(nanoCLRFilePath, null, false, logger);

            Assert.AreEqual(
$@"Error: *** Failed to locate nanoCLR instance '{nanoCLRFilePath}' ***
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {s_replaceVersion.Replace(m.message, "Vx")}"
                    ) + '\n'
            );
            Assert.AreEqual(nanoCLRFilePath, actual.NanoCLRFilePath);
            Assert.IsFalse(File.Exists(nanoCLRFilePath));
            Assert.AreEqual(false, actual.NanoClrIsInstalled);
        }

        [TestMethod]
        [DoNotParallelize]
        public void NanoCLR_Global_RequiredVersion_Available()
        {
            var logger = new LogMessengerMock();

            // No update needed
            var actual = new NanoCLRHelper(null, "1.0.0", false, logger);

            Assert.AreEqual(
$@"Verbose: Install/update nanoclr tool
Verbose: Running nanoclr Vx
Verbose: Update nanoCLR instance
Verbose: nanoCLR instance updated to Vx
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {s_replaceVersion.Replace(m.message, "Vx")}"
                    ) + '\n'
            );
            Assert.AreEqual("nanoclr", actual.NanoCLRFilePath);
            Assert.AreEqual(true, actual.NanoClrIsInstalled);
        }

        [TestMethod]
        [DoNotParallelize]
        public void NanoCLR_Global_Update()
        {
            var logger = new LogMessengerMock();

            // No update needed
            var actual = new NanoCLRHelper(null, null, true, logger);

            Assert.AreEqual(
$@"Verbose: Install/update nanoclr tool
Verbose: Running nanoclr Vx
Verbose: No need to update. Running Vx
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {s_replaceVersion.Replace(m.message, "Vx")}"
                    ) + '\n'
            );
            Assert.AreEqual("nanoclr", actual.NanoCLRFilePath);
            Assert.AreEqual(true, actual.NanoClrIsInstalled);
        }

        [TestMethod]
        [DoNotParallelize]
        public void NanoCLR_Global_NoUpdate()
        {
            var logger = new LogMessengerMock();

            var actual = new NanoCLRHelper(null, null, false, logger);

            Assert.AreEqual(
$@"Verbose: Install/update nanoclr tool
Verbose: Running nanoclr Vx
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {s_replaceVersion.Replace(m.message, "Vx")}"
                    ) + '\n'
            );
            Assert.AreEqual("nanoclr", actual.NanoCLRFilePath);
            Assert.AreEqual(true, actual.NanoClrIsInstalled);
        }
    }
}
