// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Test execution")]
    [TestCategory(Constants.VirtualDevice_TestCategory)]
    public sealed class NanoCLRHelperTest
    {
        public TestContext TestContext { get; set; }

        private static readonly Regex s_replaceVersion = new Regex(@"v[0-9]+(\.[0-9]+)+", RegexOptions.Compiled);

        [TestMethod]
        public void NanoCLR_LockNanoCLR_ForModification()
        {
            string nanoCLRFilePath = Path.Combine(TestDirectoryHelper.GetTestDirectory(TestContext), "nanoclr.exe");

            #region Test exclusive access
            // Get exclusive access in a separate task
            var waitForAccess = new CancellationTokenSource();
            var waitForAccessNotGranted = new CancellationTokenSource();
            var waitForEndAccess = new CancellationTokenSource();
            var lockTask = Task.Run(() =>
            {
                var granted = NanoCLRHelper.LockNanoCLR(nanoCLRFilePath,
                    () =>
                    {
                        waitForAccess.Cancel();
                        waitForEndAccess.Token.WaitHandle.WaitOne();
                    },
                    true);
                if (!granted)
                {
                    waitForAccessNotGranted.Cancel();
                }
            });
            // Wait until exclusive access has (not) been granted
            WaitHandle.WaitAny(new WaitHandle[] {
                waitForAccess.Token.WaitHandle,
                waitForAccessNotGranted.Token.WaitHandle
            });
            if (waitForAccessNotGranted.IsCancellationRequested)
            {
                Assert.Fail("Exclusive access was not granted");
            }
            #endregion

            #region Assert exclusive access
            // Request for exclusive access should be timed out
            Assert.IsFalse(NanoCLRHelper.LockNanoCLR(nanoCLRFilePath,
                    () => { },
                    true,
                    1));

            // Same with cancellation token
            Assert.IsFalse(NanoCLRHelper.LockNanoCLR(nanoCLRFilePath,
                    () => { },
                    true,
                    cancellationToken: new CancellationToken(true)));

            // Same with cancellation token and timeout
            var assertToken = new CancellationTokenSource(1000);
            Assert.IsFalse(NanoCLRHelper.LockNanoCLR(nanoCLRFilePath,
                    () => { },
                    true,
                    1, assertToken.Token));

            assertToken = new CancellationTokenSource(1);
            Assert.IsFalse(NanoCLRHelper.LockNanoCLR(nanoCLRFilePath,
                    () => { },
                    true,
                    1000, assertToken.Token));

            // Request for shared access should also be timed out
            Assert.IsFalse(NanoCLRHelper.LockNanoCLR(nanoCLRFilePath,
                    () => { },
                    false,
                    1));
            #endregion

            #region End exclusive access
            waitForEndAccess.Cancel();
            lockTask.Wait();
            #endregion

            #region Assert exclusive access can be granted again
            Assert.IsTrue(NanoCLRHelper.LockNanoCLR(nanoCLRFilePath,
                    () => { },
                    true,
                    1));
            #endregion
        }

        [TestMethod]
        public void NanoCLR_LockNanoCLR_NotForModification()
        {
            string nanoCLRFilePath = Path.Combine(TestDirectoryHelper.GetTestDirectory(TestContext), "nanoclr.exe");

            #region Test exclusive access
            // Get shared access in a separate task
            var waitForAccess = new CancellationTokenSource();
            var waitForAccessNotGranted = new CancellationTokenSource();
            var waitForEndAccess = new CancellationTokenSource();
            var lockTask = Task.Run(() =>
            {
                var granted = NanoCLRHelper.LockNanoCLR(nanoCLRFilePath,
                    () =>
                    {
                        waitForAccess.Cancel();
                        waitForEndAccess.Token.WaitHandle.WaitOne();
                    },
                    false);
                if (!granted)
                {
                    waitForAccessNotGranted.Cancel();
                }
            });
            // Wait until shared access has (not) been granted
            WaitHandle.WaitAny(new WaitHandle[] {
                waitForAccess.Token.WaitHandle,
                waitForAccessNotGranted.Token.WaitHandle
            });
            if (waitForAccessNotGranted.IsCancellationRequested)
            {
                Assert.Fail("Shared access was not granted");
            }
            #endregion

            #region Assert shared access
            // Request for exclusive access should be timed out
            Assert.IsFalse(NanoCLRHelper.LockNanoCLR(nanoCLRFilePath,
                    () => { },
                    true,
                    1));

            // Request for another shared access should not be timed out
            Assert.IsTrue(NanoCLRHelper.LockNanoCLR(nanoCLRFilePath,
                    () => { },
                    false,
                    1));
            #endregion

            #region End shared access
            waitForEndAccess.Cancel();
            lockTask.Wait();
            #endregion

            #region Assert exclusive access can be granted again
            Assert.IsTrue(NanoCLRHelper.LockNanoCLR(nanoCLRFilePath,
                    () => { },
                    true,
                    1));
            #endregion
        }

        [TestMethod]
        public void NanoCLR_Local_Download()
        {
            string nanoCLRFilePath = Path.Combine(TestDirectoryHelper.GetTestDirectory(TestContext), "nanoclr.exe");
            var logger = new LogMessengerMock();

            var actual = new NanoCLRHelper(nanoCLRFilePath, null, true, logger);

            Assert.AreEqual(
$@"Detailed: Install/update nanoclr tool
Detailed: Install/update successful. Running Vx
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
        public void NanoCLR_Global_RequiredVersion_Available()
        {
            var logger = new LogMessengerMock();

            // No update needed
            var actual = new NanoCLRHelper(null, "1.0.0", false, logger);

            Assert.AreEqual(
$@"Detailed: Install/update nanoclr tool
Detailed: Running nanoclr Vx
Detailed: Update nanoCLR instance
Detailed: nanoCLR instance updated to Vx
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
        public void NanoCLR_Global_Update()
        {
            var logger = new LogMessengerMock();

            // No update needed
            var actual = new NanoCLRHelper(null, null, true, logger);

            Assert.AreEqual(
$@"Detailed: Install/update nanoclr tool
Detailed: Running nanoclr Vx
Detailed: No need to update. Running Vx
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
        public void NanoCLR_Global_NoUpdate()
        {
            var logger = new LogMessengerMock();

            var actual = new NanoCLRHelper(null, null, false, logger);

            Assert.AreEqual(
$@"Detailed: Install/update nanoclr tool
Detailed: Running nanoclr Vx
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
