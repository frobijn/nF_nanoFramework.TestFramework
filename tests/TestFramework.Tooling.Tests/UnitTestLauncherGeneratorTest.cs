// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Unit test launcher")]
    public sealed class UnitTestLauncherGeneratorTest
    {
        /// <summary>
        /// This is a test that asserts the data used in the Program.cs
        /// file of the TestFramework.Tooling.UnitTestLauncher.Tests project.
        /// </summary>
        [TestMethod]
        public void TestFramework_Tooling_UnitTestLauncher_Tests_Asserts()
        {
            #region Get the test cases in TestFramework.Tooling.Tests.Discovery.v3 
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Execution.v3");
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);
            var logger = new LogMessengerMock();
            var testCases = new TestCaseCollection(assemblyFilePath, true, projectFilePath, logger);
            Assert.AreEqual(
                "\n",
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n'
            );
            #endregion

            #region FailInConstructor - Test
            TestCase testCase = (from tc in testCases.TestCases
                                 where tc.FullyQualifiedName == "TestFramework.Tooling.Tests.NFUnitTest.FailInConstructor.Test"
                                       && tc.ShouldRunOnVirtualDevice
                                 select tc).FirstOrDefault();
            Assert.IsNotNull(testCase);
            Assert.AreEqual(0, testCase.Group.TestGroupIndex, "Index of the FailInConstructor test class");
            Assert.AreEqual(2, testCase.TestIndex, "Index of the Test in FailInConstructor");
            Assert.AreEqual(-1, testCase.DataRowIndex, "Index of the Test in FailInConstructor");
            #endregion

            #region FailInSetup - Test
            testCase = (from tc in testCases.TestCases
                        where tc.FullyQualifiedName == "TestFramework.Tooling.Tests.NFUnitTest.FailInSetup.Test"
                              && tc.ShouldRunOnVirtualDevice
                        select tc).FirstOrDefault();
            Assert.IsNotNull(testCase);
            Assert.AreEqual(1, testCase.Group.TestGroupIndex, "Index of the FailInSetup test class");
            Assert.AreEqual(1, testCase.Group.SetupMethodIndex, "Index of the Test in FailInSetup");
            Assert.AreEqual(2, testCase.TestIndex, "Index of the Test in FailInSetup");
            Assert.AreEqual(-1, testCase.DataRowIndex, "Index of the Test in FailInSetup");
            Assert.AreEqual(3, testCase.Group.CleanupMethodIndex, "Index of the Test in FailInSetup");
            #endregion

            #region FailInTest - Test
            testCase = (from tc in testCases.TestCases
                        where tc.FullyQualifiedName == "TestFramework.Tooling.Tests.NFUnitTest.FailInTest.Test"
                              && tc.ShouldRunOnVirtualDevice
                        select tc).FirstOrDefault();
            Assert.IsNotNull(testCase);
            Assert.AreEqual(2, testCase.Group.TestGroupIndex, "Index of the FailInTest test class");
            Assert.AreEqual(1, testCase.Group.SetupMethodIndex, "Index of the Test in FailInTest");
            Assert.AreEqual(2, testCase.TestIndex, "Index of the Test in FailInTest");
            Assert.AreEqual(-1, testCase.DataRowIndex, "Index of the Test in FailInTest");
            Assert.AreEqual(3, testCase.Group.CleanupMethodIndex, "Index of the Test in FailInTest");
            #endregion

            #region InconclusiveInTest - Test
            testCase = (from tc in testCases.TestCases
                        where tc.FullyQualifiedName == "TestFramework.Tooling.Tests.NFUnitTest.InconclusiveInTest.Test"
                              && tc.ShouldRunOnVirtualDevice
                        select tc).FirstOrDefault();
            Assert.IsNotNull(testCase);
            Assert.AreEqual(3, testCase.Group.TestGroupIndex, "Index of the InconclusiveInTest test class");
            Assert.AreEqual(1, testCase.Group.SetupMethodIndex, "Index of the Test in InconclusiveInTest");
            Assert.AreEqual(2, testCase.TestIndex, "Index of the Test in InconclusiveInTest");
            Assert.AreEqual(-1, testCase.DataRowIndex, "Index of the Test in InconclusiveInTest");
            Assert.AreEqual(3, testCase.Group.CleanupMethodIndex, "Index of the Test in InconclusiveInTest");
            #endregion

            #region CleanupFailedInTest - Test
            testCase = (from tc in testCases.TestCases
                        where tc.FullyQualifiedName == "TestFramework.Tooling.Tests.NFUnitTest.CleanupFailedInTest.Test"
                              && tc.ShouldRunOnVirtualDevice
                        select tc).FirstOrDefault();
            Assert.IsNotNull(testCase);
            Assert.AreEqual(4, testCase.Group.TestGroupIndex, "Index of the CleanupFailedInTest test class");
            Assert.AreEqual(1, testCase.Group.SetupMethodIndex, "Index of the Test in CleanupFailedInTest");
            Assert.AreEqual(2, testCase.TestIndex, "Index of the Test in CleanupFailedInTest");
            Assert.AreEqual(-1, testCase.DataRowIndex, "Index of the Test in CleanupFailedInTest");
            Assert.AreEqual(3, testCase.Group.CleanupMethodIndex, "Index of the Test in CleanupFailedInTest");
            #endregion

            #region FailInCleanUp - Test
            testCase = (from tc in testCases.TestCases
                        where tc.FullyQualifiedName == "TestFramework.Tooling.Tests.NFUnitTest.FailInCleanUp.Test"
                              && tc.ShouldRunOnVirtualDevice
                        select tc).FirstOrDefault();
            Assert.IsNotNull(testCase);
            Assert.AreEqual(5, testCase.Group.TestGroupIndex, "Index of the FailInCleanUp test class");
            Assert.AreEqual(1, testCase.Group.SetupMethodIndex, "Index of the Test in FailInCleanUp");
            Assert.AreEqual(2, testCase.TestIndex, "Index of the Test in FailInCleanUp");
            Assert.AreEqual(-1, testCase.DataRowIndex, "Index of the Test in FailInCleanUp");
            Assert.AreEqual(3, testCase.Group.CleanupMethodIndex, "Index of the Test in FailInCleanUp");
            #endregion

            #region FailInDispose - Test
            testCase = (from tc in testCases.TestCases
                        where tc.FullyQualifiedName == "TestFramework.Tooling.Tests.NFUnitTest.FailInDispose.Test"
                              && tc.ShouldRunOnVirtualDevice
                        select tc).FirstOrDefault();
            Assert.IsNotNull(testCase);
            Assert.AreEqual(6, testCase.Group.TestGroupIndex, "Index of the FailInDispose test class");
            Assert.AreEqual(1, testCase.Group.SetupMethodIndex, "Index of the Test in FailInDispose");
            Assert.AreEqual(2, testCase.TestIndex, "Index of the Test in FailInDispose");
            Assert.AreEqual(-1, testCase.DataRowIndex, "Index of the Test in FailInDispose");
            Assert.AreEqual(3, testCase.Group.CleanupMethodIndex, "Index of the Test in FailInDispose");
            #endregion

            #region NonFailingTest - Test
            testCase = (from tc in testCases.TestCases
                        where tc.FullyQualifiedName == "TestFramework.Tooling.Tests.NFUnitTest.NonFailingTest.Test"
                              && tc.ShouldRunOnVirtualDevice
                        select tc).FirstOrDefault();
            Assert.IsNotNull(testCase);
            Assert.AreEqual(7, testCase.Group.TestGroupIndex, "Index of the NonFailingTest test class");
            Assert.AreEqual(1, testCase.Group.SetupMethodIndex, "Index of the Test in NonFailingTest");
            Assert.AreEqual(2, testCase.TestIndex, "Index of the Test in NonFailingTest");
            Assert.AreEqual(-1, testCase.DataRowIndex, "Index of the Test in NonFailingTest");
            Assert.AreEqual(3, testCase.Group.CleanupMethodIndex, "Index of the Test in NonFailingTest");
            #endregion

            #region TestWithMethods - Test1
            testCase = (from tc in testCases.TestCases
                        where tc.FullyQualifiedName == "TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test1"
                              && tc.ShouldRunOnVirtualDevice
                        select tc).FirstOrDefault();
            Assert.IsNotNull(testCase);
            Assert.AreEqual(11, testCase.Group.TestGroupIndex, "Index of the TestWithMethods test class");
            Assert.AreEqual(-1, testCase.Group.SetupMethodIndex, "Index of the Test1 in TestWithMethods");
            Assert.AreEqual(0, testCase.TestIndex, "Index of the Test1 in TestWithMethods");
            Assert.AreEqual(0, testCase.DataRowIndex, "Index of the first data row in Test1 in TestWithMethods");
            Assert.AreEqual(-1, testCase.Group.CleanupMethodIndex, "Index of the Test1 in TestWithMethods");
            #endregion

            #region TestWithMethods - Test2
            testCase = (from tc in testCases.TestCases
                        where tc.FullyQualifiedName == "TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2"
                              && tc.ShouldRunOnVirtualDevice
                        select tc).FirstOrDefault();
            Assert.IsNotNull(testCase);
            Assert.AreEqual(11, testCase.Group.TestGroupIndex, "Index of the TestWithMethods test class");
            Assert.AreEqual(-1, testCase.Group.SetupMethodIndex, "Index of the Test2 in TestWithMethods");
            Assert.AreEqual(1, testCase.TestIndex, "Index of the Test2 in TestWithMethods");
            Assert.AreEqual(-1, testCase.DataRowIndex, "Index of the Test2 in TestWithMethods");
            Assert.AreEqual(-1, testCase.Group.CleanupMethodIndex, "Index of the Test2 in TestWithMethods");
            #endregion
        }
    }
}
