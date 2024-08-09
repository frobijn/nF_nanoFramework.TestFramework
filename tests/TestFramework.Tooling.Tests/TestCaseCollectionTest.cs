// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    [TestCategory("Test cases")]
    public class TestCaseCollectionTest
    {
        #region TestFramework.Tooling.Tests.Discovery.v2
        [TestMethod]
        public void TestCases_Discovery_v2_VirtualDevice()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v2");
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);
            var logger = new LogMessengerMock();
            string pathPrefix = Path.GetDirectoryName(projectFilePath) + Path.DirectorySeparatorChar;

            var actual = new TestCaseCollection(assemblyFilePath, false, projectFilePath, logger);

            Assert.IsNotNull(actual.TestCases);
            Assert.AreEqual(
$@"Detailed: {pathPrefix}TestAllCurrentAttributes.cs(13,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Detailed: {pathPrefix}TestAllCurrentAttributes.cs(19,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Detailed: {pathPrefix}TestWithMethods.cs(9,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Detailed: {pathPrefix}TestWithMethods.cs(14,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n'
            );

            // Assert collection, index, FQN and name
            Assert.AreEqual(
$@"G0T0 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod'
G0T1D0 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1)'
G0T1D1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2)'
G1T0 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test'
G1T1 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} {tc.FullyQualifiedName} '{tc.DisplayName}'"
                ) + '\n'
            );

            // Assert source location and traits
            Assert.AreEqual(
$@"G0T0 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device'
G0T1D0 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device'
G0T1D1 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device'
G1T0 @{pathPrefix}TestWithMethods.cs(9,21) '@Virtual Device'
G1T1 @{pathPrefix}TestWithMethods.cs(14,21) '@Virtual Device'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} @{tc.TestMethodSourceCodeLocation?.ForMessage()} {string.Join(", ", from t in tc.Traits select $"'{t}'")}"
                ) + '\n'
            );

            // Assert run information
            Assert.AreEqual(
$@"G0T0 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
G0T1D0 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G0T1D1 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G1T0 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
G1T1 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} GS={tc.Group?.SetupMethodIndex} GC={tc.Group?.CleanupMethodIndex} FQN={tc.FullyQualifiedName}"
                ) + '\n'
            );

            // No test case for real hardware, only virtual device
            Assert.AreEqual(0, (from tc in actual.TestCases
                                where tc.ShouldRunOnRealHardware
                                select tc).Count());
            Assert.AreEqual(0, (from tc in actual.TestCases
                                where !tc.ShouldRunOnVirtualDevice
                                select tc).Count());
        }

        [TestMethod]
        public void TestCases_Discovery_v2_VirtualDevice_RealHardware()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v2");
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);
            var logger = new LogMessengerMock();
            string pathPrefix = Path.GetDirectoryName(projectFilePath) + Path.DirectorySeparatorChar;

            var actual = new TestCaseCollection(assemblyFilePath, true, projectFilePath, logger);

            Assert.IsNotNull(actual.TestCases);
            Assert.AreEqual(
$@"Detailed: {pathPrefix}TestAllCurrentAttributes.cs(13,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Detailed: {pathPrefix}TestAllCurrentAttributes.cs(19,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Detailed: {pathPrefix}TestWithMethods.cs(9,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Detailed: {pathPrefix}TestWithMethods.cs(14,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n'
            );

            // Assert collection, index, FQN and name
            Assert.AreEqual(
$@"G0T0 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [Virtual Device]'
G0T0 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [Real hardware]'
G0T1D0 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Virtual Device]'
G0T1D0 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Real hardware]'
G0T1D1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Virtual Device]'
G0T1D1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Real hardware]'
G1T0 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Virtual Device]'
G1T0 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Real hardware]'
G1T1 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Virtual Device]'
G1T1 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Real hardware]'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} {tc.FullyQualifiedName} '{tc.DisplayName}'"
                ) + '\n'
            );

            // Assert source location and traits
            Assert.AreEqual(
$@"G0T0 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device'
G0T0 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Real hardware'
G0T1D0 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device'
G0T1D0 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Real hardware'
G0T1D1 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device'
G0T1D1 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Real hardware'
G1T0 @{pathPrefix}TestWithMethods.cs(9,21) '@Virtual Device'
G1T0 @{pathPrefix}TestWithMethods.cs(9,21) '@Real hardware'
G1T1 @{pathPrefix}TestWithMethods.cs(14,21) '@Virtual Device'
G1T1 @{pathPrefix}TestWithMethods.cs(14,21) '@Real hardware'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} @{tc.TestMethodSourceCodeLocation?.ForMessage()} {string.Join(", ", from t in tc.Traits select $"'{t}'")}"
                ) + '\n'
            );

            // Assert run information
            Assert.AreEqual(
$@"G0T0 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
G0T0 RH=True VD=False GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
G0T1D0 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G0T1D0 RH=True VD=False GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G0T1D1 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G0T1D1 RH=True VD=False GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G1T0 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
G1T0 RH=True VD=False GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
G1T1 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
G1T1 RH=True VD=False GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} GS={tc.Group?.SetupMethodIndex} GC={tc.Group?.CleanupMethodIndex} FQN={tc.FullyQualifiedName}"
                ) + '\n'
            );

            // All tests should run somewhere
            Assert.AreEqual(0, (from tc in actual.TestCases
                                where !tc.ShouldRunOnVirtualDevice && !tc.ShouldRunOnRealHardware
                                select tc).Count());

            // Assert selection of real hardware test cases
            var anyDevice = new TestDeviceProxy(new TestDeviceMock(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            foreach (TestCase testCase in actual.TestCases)
            {
                if (testCase.ShouldRunOnRealHardware)
                {
                    Assert.AreEqual(true, testCase.SelectDevicesForExecution(new TestDeviceProxy[] { anyDevice }).Any());
                }
            }
        }
        #endregion

        #region TestFramework.Tooling.Tests.Discovery.v3
        [TestMethod]
        public void TestCases_Discovery_v3()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);
            var logger = new LogMessengerMock();
            string pathPrefix = Path.GetDirectoryName(projectFilePath) + Path.DirectorySeparatorChar;

            var actual = new TestCaseCollection(assemblyFilePath, true, projectFilePath, logger);

            Assert.IsNotNull(actual.TestCases);
            Assert.AreEqual(
$@"Verbose: {pathPrefix}TestWithALotOfErrors.cs(10,6): Only one attribute that implements 'ITestClass' is allowed. Only the first one is used, subsequent attributes are ignored.
Verbose: {pathPrefix}TestWithALotOfErrors.cs(19,10): Only one method of a class can have attribute implements 'ISetup'. Subsequent attribute is ignored.
Verbose: {pathPrefix}TestWithALotOfErrors.cs(31,10): Only one method of a class can have attribute that implements 'ICleanup'. Subsequent attribute is ignored.
Verbose: {pathPrefix}TestWithALotOfErrors.cs(39,10): Only one method of a class can have attribute implements 'ISetup'. Subsequent attribute is ignored.
Verbose: {pathPrefix}TestWithALotOfErrors.cs(38,10): Only one method of a class can have attribute that implements 'ICleanup'. Subsequent attribute is ignored.
Verbose: {pathPrefix}TestWithALotOfErrors.cs(41,21): No other attributes are allowed when the attributes that implement 'ICleanup'/'ISetup' are present. Extra attributes are ignored.
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n'
            );

            // Assert collection, index, FQN and name
            Assert.AreEqual(
$@"G0T0 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [Virtual Device]'
G0T0 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [Real hardware]'
G0T1D0 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Virtual Device]'
G0T1D0 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Real hardware]'
G0T1D1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Virtual Device]'
G0T1D1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Real hardware]'
G1T0 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method 'Method [Virtual Device]'
G1T0 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method 'Method [Real hardware]'
G2T0 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1 'Method1 [Virtual Device]'
G2T0 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1 'Method1 [Real hardware]'
G2T1 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2 'Method2 [Virtual Device]'
G2T1 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2 'Method2 [Real hardware]'
G4T0 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray 'TestThatIsNowInDisarray [Virtual Device]'
G4T0 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray 'TestThatIsNowInDisarray [Real hardware]'
G4T1 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile 'TestOnDeviceWithSomeFile [Virtual Device]'
G4T1 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile 'TestOnDeviceWithSomeFile [Real hardware]'
G5T0 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Virtual Device]'
G5T0 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Real hardware]'
G5T1 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Virtual Device]'
G5T1 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Real hardware]'
G6T0 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits 'MethodWithTraits [Virtual Device]'
G6T0 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits 'MethodWithTraits [Real hardware]'
G6T1 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods [Virtual Device]'
G6T1 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods [Real hardware]'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} {tc.FullyQualifiedName} '{tc.DisplayName}'"
                ) + '\n'
            );

            // Assert source location and traits
            Assert.AreEqual(
$@"G0T0 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device'
G0T0 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@test', '@Real hardware'
G0T1D0 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device'
G0T1D0 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@test', '@Real hardware'
G0T1D1 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device'
G0T1D1 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@test', '@Real hardware'
G1T0 @{pathPrefix}TestClassVariants.cs(13,28) '@Virtual Device'
G1T0 @{pathPrefix}TestClassVariants.cs(13,28) '@test', '@Real hardware'
G2T0 @{pathPrefix}TestClassVariants.cs(33,21) '@Virtual Device'
G2T0 @{pathPrefix}TestClassVariants.cs(33,21) '@test', '@Real hardware'
G2T1 @{pathPrefix}TestClassVariants.cs(40,21) '@Virtual Device'
G2T1 @{pathPrefix}TestClassVariants.cs(40,21) '@test', '@Real hardware'
G4T0 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@Virtual Device'
G4T0 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@test', '@Real hardware'
G4T1 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@Virtual Device'
G4T1 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@test', '@DeviceWithSomeFile', '@Real hardware'
G5T0 @{pathPrefix}TestWithMethods.cs(13,21) '@Virtual Device'
G5T0 @{pathPrefix}TestWithMethods.cs(13,21) '@test', '@Real hardware'
G5T1 @{pathPrefix}TestWithMethods.cs(18,21) '@Virtual Device'
G5T1 @{pathPrefix}TestWithMethods.cs(18,21) '@test', '@Real hardware'
G6T0 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(15,21) '@Virtual Device'
G6T0 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(15,21) '@test', '@Real hardware'
G6T1 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(20,21) '@Virtual Device'
G6T1 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(20,21) '@test', '@esp32', '@Real hardware'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} @{tc.TestMethodSourceCodeLocation?.ForMessage()} {string.Join(", ", from t in tc.Traits select $"'{t}'")}"
                ) + '\n'
            );

            // Assert run information
            Assert.AreEqual(
$@"G0T0 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
G0T0 RH=True VD=False GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
G0T1D0 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G0T1D0 RH=True VD=False GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G0T1D1 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G0T1D1 RH=True VD=False GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G1T0 RH=False VD=True GS=1 GC=2 FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method
G1T0 RH=True VD=False GS=1 GC=2 FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method
G2T0 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1
G2T0 RH=True VD=False GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1
G2T1 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2
G2T1 RH=True VD=False GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2
G4T0 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray
G4T0 RH=True VD=False GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray
G4T1 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
G4T1 RH=True VD=False GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
G5T0 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
G5T0 RH=True VD=False GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
G5T1 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
G5T1 RH=True VD=False GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
G6T0 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits
G6T0 RH=True VD=False GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits
G6T1 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods
G6T1 RH=True VD=False GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} GS={tc.Group?.SetupMethodIndex} GC={tc.Group?.CleanupMethodIndex} FQN={tc.FullyQualifiedName}"
                ) + '\n'
            );

            // All tests should run somewhere
            Assert.AreEqual(0, (from tc in actual.TestCases
                                where !tc.ShouldRunOnVirtualDevice && !tc.ShouldRunOnRealHardware
                                select tc).Count());

            // Assert selection of real hardware test cases
            var esp32Device = new TestDeviceProxy(new TestDeviceMock(Guid.NewGuid().ToString(), "esp32"));
            foreach (TestCase testCase in actual.TestCases)
            {
                if (testCase.ShouldRunOnRealHardware && testCase.Traits.Contains("@esp32"))
                {
                    Assert.AreEqual(true, testCase.SelectDevicesForExecution(new TestDeviceProxy[] { esp32Device }).Any());
                }
            }
        }

        [TestMethod]
        public void TestCases_Discovery_v3_NoRealHardware()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);
            var logger = new LogMessengerMock();
            string pathPrefix = Path.GetDirectoryName(projectFilePath) + Path.DirectorySeparatorChar;

            var actual = new TestCaseCollection(assemblyFilePath, false, projectFilePath, logger);

            Assert.IsNotNull(actual.TestCases);
            Assert.AreEqual(
$@"Verbose: {pathPrefix}TestWithALotOfErrors.cs(10,6): Only one attribute that implements 'ITestClass' is allowed. Only the first one is used, subsequent attributes are ignored.
Verbose: {pathPrefix}TestWithALotOfErrors.cs(19,10): Only one method of a class can have attribute implements 'ISetup'. Subsequent attribute is ignored.
Verbose: {pathPrefix}TestWithALotOfErrors.cs(31,10): Only one method of a class can have attribute that implements 'ICleanup'. Subsequent attribute is ignored.
Verbose: {pathPrefix}TestWithALotOfErrors.cs(39,10): Only one method of a class can have attribute implements 'ISetup'. Subsequent attribute is ignored.
Verbose: {pathPrefix}TestWithALotOfErrors.cs(38,10): Only one method of a class can have attribute that implements 'ICleanup'. Subsequent attribute is ignored.
Verbose: {pathPrefix}TestWithALotOfErrors.cs(41,21): No other attributes are allowed when the attributes that implement 'ICleanup'/'ISetup' are present. Extra attributes are ignored.
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n'
            );

            // Assert collection, index, FQN and name
            Assert.AreEqual(
$@"G0T0 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod'
G0T1D0 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1)'
G0T1D1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2)'
G1T0 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method 'Method'
G2T0 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1 'Method1'
G2T1 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2 'Method2'
G4T0 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray 'TestThatIsNowInDisarray'
G4T1 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile 'TestOnDeviceWithSomeFile'
G5T0 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test'
G5T1 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2'
G6T0 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits 'MethodWithTraits'
G6T1 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} {tc.FullyQualifiedName} '{tc.DisplayName}'"
                ) + '\n'
            );

            // Assert source location and traits
            Assert.AreEqual(
$@"G0T0 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device'
G0T1D0 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device'
G0T1D1 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device'
G1T0 @{pathPrefix}TestClassVariants.cs(13,28) '@Virtual Device'
G2T0 @{pathPrefix}TestClassVariants.cs(33,21) '@Virtual Device'
G2T1 @{pathPrefix}TestClassVariants.cs(40,21) '@Virtual Device'
G4T0 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@Virtual Device'
G4T1 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@Virtual Device'
G5T0 @{pathPrefix}TestWithMethods.cs(13,21) '@Virtual Device'
G5T1 @{pathPrefix}TestWithMethods.cs(18,21) '@Virtual Device'
G6T0 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(15,21) '@Virtual Device'
G6T1 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(20,21) '@Virtual Device'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} @{tc.TestMethodSourceCodeLocation?.ForMessage()} {string.Join(", ", from t in tc.Traits select $"'{t}'")}"
                ) + '\n'
            );

            // Assert run information
            Assert.AreEqual(
$@"G0T0 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
G0T1D0 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G0T1D1 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G1T0 RH=False VD=True GS=1 GC=2 FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method
G2T0 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1
G2T1 RH=False VD=True GS=2 GC=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2
G4T0 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray
G4T1 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
G5T0 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
G5T1 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
G6T0 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits
G6T1 RH=False VD=True GS=-1 GC=-1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} GS={tc.Group?.SetupMethodIndex} GC={tc.Group?.CleanupMethodIndex} FQN={tc.FullyQualifiedName}"
                ) + '\n'
            );

            // No test case for real hardware
            Assert.AreEqual(0, (from tc in actual.TestCases
                                where tc.ShouldRunOnRealHardware
                                select tc).Count());
            Assert.AreEqual(0, (from tc in actual.TestCases
                                where !tc.ShouldRunOnVirtualDevice
                                select tc).Count());
        }
        #endregion

        #region Multiple assemblies, no source, no logger
        [TestMethod]
        public void TestCases_Multiple_Assemblies()
        {
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v2");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string pathPrefix1 = Path.GetDirectoryName(projectFilePath1) + Path.DirectorySeparatorChar;
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath2 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath2);
            string pathPrefix2 = Path.GetDirectoryName(projectFilePath2) + Path.DirectorySeparatorChar;
            string assemblyFilePath3 = typeof(TestCaseCollection).Assembly.Location;

            var logger = new LogMessengerMock();
            var actual = new TestCaseCollection(new string[] { assemblyFilePath1, assemblyFilePath3, assemblyFilePath2 },
                                                (f) => ProjectSourceInventory.FindProjectFilePath(f, null),
                                                true,
                                                logger);
            Assert.IsNotNull(actual.TestCases);
            Assert.AreEqual(
$@"Detailed: {pathPrefix1}TestAllCurrentAttributes.cs(13,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Detailed: {pathPrefix1}TestAllCurrentAttributes.cs(19,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Detailed: {pathPrefix1}TestWithMethods.cs(9,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Detailed: {pathPrefix1}TestWithMethods.cs(14,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Verbose: {pathPrefix2}TestWithALotOfErrors.cs(10,6): Only one attribute that implements 'ITestClass' is allowed. Only the first one is used, subsequent attributes are ignored.
Verbose: {pathPrefix2}TestWithALotOfErrors.cs(19,10): Only one method of a class can have attribute implements 'ISetup'. Subsequent attribute is ignored.
Verbose: {pathPrefix2}TestWithALotOfErrors.cs(31,10): Only one method of a class can have attribute that implements 'ICleanup'. Subsequent attribute is ignored.
Verbose: {pathPrefix2}TestWithALotOfErrors.cs(39,10): Only one method of a class can have attribute implements 'ISetup'. Subsequent attribute is ignored.
Verbose: {pathPrefix2}TestWithALotOfErrors.cs(38,10): Only one method of a class can have attribute that implements 'ICleanup'. Subsequent attribute is ignored.
Verbose: {pathPrefix2}TestWithALotOfErrors.cs(41,21): No other attributes are allowed when the attributes that implement 'ICleanup'/'ISetup' are present. Extra attributes are ignored.
Verbose: Project file for assembly '{assemblyFilePath3}' not found
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n'
            );

            // Check only the number of test cases
            Assert.AreEqual(
$@"{assemblyFilePath1} #5
{assemblyFilePath2} #12
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from sel in actual.TestOnVirtualDevice
                        orderby sel.AssemblyFilePath
                        select $"{sel.AssemblyFilePath} #{sel.TestCases.Count}"
                    ) + '\n'
            );
            Assert.AreEqual(
            $@"{assemblyFilePath1} #5
{assemblyFilePath2} #12
".Replace("\r\n", "\n"),
                            string.Join("\n",
                                    from sel in actual.TestOnRealHardware
                                    orderby sel.AssemblyFilePath
                                    select $"{sel.AssemblyFilePath} #{sel.TestCases.Count}"
                                ) + '\n'
                        );
        }

        [TestMethod]
        public void TestCases_Multiple_Assemblies_NoLogger()
        {
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v2");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string pathPrefix1 = Path.GetDirectoryName(projectFilePath1) + Path.DirectorySeparatorChar;
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath2 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath2);
            string pathPrefix2 = Path.GetDirectoryName(projectFilePath2) + Path.DirectorySeparatorChar;
            string assemblyFilePath3 = typeof(TestCaseCollection).Assembly.Location;

            var withLogger = new TestCaseCollection(new string[] { assemblyFilePath1, assemblyFilePath3, assemblyFilePath2 },
                                                    (f) => ProjectSourceInventory.FindProjectFilePath(f, null),
                                                    true,
                                                    new LogMessengerMock());
            var actual = new TestCaseCollection(new string[] { assemblyFilePath1, assemblyFilePath3, assemblyFilePath2 },
                                                (f) => ProjectSourceInventory.FindProjectFilePath(f, null),
                                                true,
                                                null);
            Assert.IsNotNull(actual.TestCases);
            Assert.AreEqual(withLogger.TestCases.Count(), actual.TestCases.Count());
        }

        [TestMethod]
        public void TestCases_Multiple_Assemblies_NoSourceCode()
        {
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v2");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string pathPrefix1 = Path.GetDirectoryName(projectFilePath1) + Path.DirectorySeparatorChar;
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath2 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath2);
            string pathPrefix2 = Path.GetDirectoryName(projectFilePath2) + Path.DirectorySeparatorChar;
            string assemblyFilePath3 = typeof(TestCaseCollection).Assembly.Location;
            var logger = new LogMessengerMock();

            var withLogger = new TestCaseCollection(new string[] { assemblyFilePath1, assemblyFilePath3, assemblyFilePath2 },
                                                    (f) => ProjectSourceInventory.FindProjectFilePath(f, null),
                                                    true,
                                                    logger);

            logger = new LogMessengerMock();
            var actual = new TestCaseCollection(new string[] { assemblyFilePath1, assemblyFilePath3, assemblyFilePath2 },
                                                (f) => null,
                                                true,
                                                logger);
            Assert.IsNotNull(actual.TestCases);
            Assert.AreEqual(withLogger.TestCases.Count(), actual.TestCases.Count());
        }
        #endregion

        #region Selection
        [TestMethod]
        [DataRow(true, true)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(false, false)]
        public void TestCases_NFUnitTests_SelectAll(bool originalAllowHardware, bool selectionAllowHardware)
        {
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v2");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath2 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath2);

            var logger = new LogMessengerMock();
            var original = new TestCaseCollection(new string[] { assemblyFilePath1, assemblyFilePath2 },
                                                  (f) => ProjectSourceInventory.FindProjectFilePath(f, null),
                                                  originalAllowHardware,
                                                  logger);
            if (original.TestCases.Count() == 0)
            {
                Assert.Inconclusive("Original collection of test cases could not be constructed");
            }
            string expectedDiscoveryMessages = string.Join("\n",
                                                from m in logger.Messages
                                                select $"{m.level}: {m.message}"
                                            ) + '\n';

            // Select all test cases
            var selectionSpecification = (from tc in original.TestCases
                                          orderby tc.AssemblyFilePath, tc.ShouldRunOnVirtualDevice ? 0 : 1, -tc.TestIndex
                                          select (tc.AssemblyFilePath, tc.DisplayName, tc.FullyQualifiedName)).ToList();
            logger = new LogMessengerMock();
            var actual = new TestCaseCollection(selectionSpecification,
                                                (f) => ProjectSourceInventory.FindProjectFilePath(f, logger),
                                                selectionAllowHardware,
                                                logger);

            // Assert that there are no selection-related messages
            Assert.AreEqual(
                expectedDiscoveryMessages,
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n'
            );

            // Assert that all test case are present in the same order
            Assert.AreEqual(
                string.Join("\n",
                    from tc in original.TestCases
                    where tc.ShouldRunOnVirtualDevice || (originalAllowHardware && selectionAllowHardware && tc.ShouldRunOnRealHardware)
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} ({tc.FullyQualifiedName}) {s_stripDevice.Replace(tc.DisplayName, "")} VD={tc.ShouldRunOnVirtualDevice} RH={tc.ShouldRunOnRealHardware}"
                ) + '\n',
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} ({tc.FullyQualifiedName}) {s_stripDevice.Replace(tc.DisplayName, "")} VD={tc.ShouldRunOnVirtualDevice} RH={tc.ShouldRunOnRealHardware}"
                ) + '\n'
            );

            // Assert the selection index
            foreach (TestCaseSelection selection in actual.TestOnVirtualDevice)
            {
                foreach ((int selectionIndex, TestCase testCase) in selection.TestCases)
                {
                    Assert.IsTrue(selectionIndex >= 0);
                    (string AssemblyFilePath, string DisplayName, string FullyQualifiedName) = selectionSpecification[selectionIndex];

                    Assert.AreEqual(testCase.AssemblyFilePath, AssemblyFilePath);
                    Assert.AreEqual(testCase.FullyQualifiedName, FullyQualifiedName);

                    int idx = testCase.DisplayName.IndexOf('[');
                    string displayBaseName = idx < 0 ? testCase.DisplayName : testCase.DisplayName.Substring(0, idx).Trim();
                    string deviceName = $"{displayBaseName} [Virtual Device]";
                    Assert.IsTrue(DisplayName == displayBaseName || DisplayName == deviceName);
                }
            }
            foreach (TestCaseSelection selection in actual.TestOnRealHardware)
            {
                foreach ((int selectionIndex, TestCase testCase) in selection.TestCases)
                {
                    Assert.IsTrue(selectionIndex >= 0);
                    (string AssemblyFilePath, string DisplayName, string FullyQualifiedName) = selectionSpecification[selectionIndex];

                    Assert.AreEqual(testCase.AssemblyFilePath, AssemblyFilePath);
                    Assert.AreEqual(testCase.FullyQualifiedName, FullyQualifiedName);

                    int idx = testCase.DisplayName.IndexOf('[');
                    string displayBaseName = idx < 0 ? testCase.DisplayName : testCase.DisplayName.Substring(0, idx).Trim();
                    string deviceName = $"{displayBaseName} [Real hardware]";
                    Assert.IsTrue(DisplayName == displayBaseName || DisplayName == deviceName);
                }
            }
        }
        private static readonly Regex s_stripDevice = new Regex(@"\s\[[^]]+\]", RegexOptions.Compiled);

        [TestMethod]
        public void TestCases_NFUnitTests_SelectAFewAndNonExisting()
        {
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v2");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath2 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath2);
            string assemblyFilePath3 = typeof(TestCaseCollection).Assembly.Location;

            var logger = new LogMessengerMock();
            var original = new TestCaseCollection(new string[] { assemblyFilePath1, assemblyFilePath2 },
                                                  (f) => ProjectSourceInventory.FindProjectFilePath(f, null),
                                                  true,
                                                  logger);
            if (original.TestCases.Count() == 0)
            {
                Assert.Inconclusive("Original collection of test cases could not be constructed");
            }
            string expectedDiscoveryMessages = string.Join("\n",
                                                from m in logger.Messages
                                                select $"{m.level}: {m.message}"
                                            ) + '\n';

            // Select some test cases
            logger = new LogMessengerMock();
            var actual = new TestCaseCollection(
                new (string, string, string)[]
                {
                    (assemblyFilePath1, "TestMethod1(1,1) [Real hardware]", "TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1"),
                    (assemblyFilePath1, "TestMethod1(2,2) [some_platform]", "TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1"),
                    (assemblyFilePath1, "NoSuchMethod", "TestFramework.Tooling.Tests.NFUnitTest.NoSuchClass.NoSuchMethod"),
                    (assemblyFilePath2, "MethodWithTraits [Virtual Device]", "TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits"),
                    (assemblyFilePath2, "MethodWithTraits [Real hardware]", "TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits"),
                    (assemblyFilePath1, "Method2 [Real hardware]", "TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2"),
                },
                (f) => ProjectSourceInventory.FindProjectFilePath(f, logger),
                true,
                logger);

            // Assert the selection-related messages
            Assert.AreEqual(
                expectedDiscoveryMessages +
$@"Verbose: Test case 'TestMethod1(2,2) [some_platform]' (TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1) from '{assemblyFilePath1}' is no longer available
Verbose: Test case 'NoSuchMethod' (TestFramework.Tooling.Tests.NFUnitTest.NoSuchClass.NoSuchMethod) from '{assemblyFilePath1}' is no longer available
Verbose: Test case 'Method2 [Real hardware]' (TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2) from '{assemblyFilePath1}' is no longer available
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n'
            );

            // Assert that the selected test case are present
            Assert.AreEqual(
@"G0T1D0 (TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1) TestMethod1(1,1) [Real hardware]
G6T0 (TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits) MethodWithTraits [Virtual Device]
G6T0 (TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits) MethodWithTraits [Real hardware]
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} ({tc.FullyQualifiedName}) {tc.DisplayName}"
                ) + '\n'
            );
        }
        #endregion
    }
}
