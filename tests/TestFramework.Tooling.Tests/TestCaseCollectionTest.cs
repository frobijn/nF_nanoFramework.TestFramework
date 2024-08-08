// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;
using TestFramework.Tooling.Tests.Helpers;

/*
 * See remark about assembly dependencies in ProjectSourceInventoryTest
 */

namespace TestFramework.Tooling.Tests
{
    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class TestCaseCollectionTest
    {
        #region TestFramework.Tooling.Tests.Discovery.v2
        [TestMethod]
        [TestCategory("Test cases")]
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

            // Assert assembly file path
            Assert.AreEqual(1, actual.AssemblyFilePaths.Count);
            Assert.AreEqual(assemblyFilePath, actual.AssemblyFilePaths[0]);
            Assert.AreEqual(0, (from tc in actual.TestCases
                                where tc.AssemblyFilePath != assemblyFilePath
                                select tc).Count());

            // Assert collection, index, FQN and name
            Assert.AreEqual(
$@"#1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod'
#2 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1)'
#3 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2)'
#4 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test'
#5 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} {tc.FullyQualifiedName} '{tc.DisplayName}'"
                ) + '\n'
            );
            Assert.AreEqual(5, actual.TestMethodsInAssembly(assemblyFilePath));

            // Assert source location and traits
            Assert.AreEqual(
$@"#1 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device'
#2 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device'
#3 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device'
#4 @{pathPrefix}TestWithMethods.cs(9,21) '@Virtual Device'
#5 @{pathPrefix}TestWithMethods.cs(14,21) '@Virtual Device'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} @{tc.TestMethodSourceCodeLocation?.ForMessage()} {string.Join(", ", from t in tc.Traits select $"'{t}'")}"
                ) + '\n'
            );

            // Assert run information
            Assert.AreEqual(
$@"#1 RH=False VD=True G=1
#2 RH=False VD=True G=1
#3 RH=False VD=True G=1
#4 RH=False VD=True G=2
#5 RH=False VD=True G=2
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} G={tc.Group?.TestGroupIndex}"
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
        [TestCategory("Test cases")]
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

            // Assert assembly file path
            Assert.AreEqual(1, actual.AssemblyFilePaths.Count);
            Assert.AreEqual(assemblyFilePath, actual.AssemblyFilePaths[0]);
            Assert.AreEqual(0, (from tc in actual.TestCases
                                where tc.AssemblyFilePath != assemblyFilePath
                                select tc).Count());


            // Assert collection, index, FQN and name
            Assert.AreEqual(
$@"#1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [Virtual Device]'
#1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [Real hardware]'
#2 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Virtual Device]'
#2 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Real hardware]'
#3 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Virtual Device]'
#3 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Real hardware]'
#4 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Virtual Device]'
#4 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Real hardware]'
#5 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Virtual Device]'
#5 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Real hardware]'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} {tc.FullyQualifiedName} '{tc.DisplayName}'"
                ) + '\n'
            );
            Assert.AreEqual(5, actual.TestMethodsInAssembly(assemblyFilePath));

            // Assert source location and traits
            Assert.AreEqual(
$@"#1 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device'
#1 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Real hardware'
#2 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device'
#2 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Real hardware'
#3 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device'
#3 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Real hardware'
#4 @{pathPrefix}TestWithMethods.cs(9,21) '@Virtual Device'
#4 @{pathPrefix}TestWithMethods.cs(9,21) '@Real hardware'
#5 @{pathPrefix}TestWithMethods.cs(14,21) '@Virtual Device'
#5 @{pathPrefix}TestWithMethods.cs(14,21) '@Real hardware'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} @{tc.TestMethodSourceCodeLocation?.ForMessage()} {string.Join(", ", from t in tc.Traits select $"'{t}'")}"
                ) + '\n'
            );

            // Assert run information
            Assert.AreEqual(
$@"#1 RH=False VD=True G=1
#1 RH=True VD=False G=1
#2 RH=False VD=True G=1
#2 RH=True VD=False G=1
#3 RH=False VD=True G=1
#3 RH=True VD=False G=1
#4 RH=False VD=True G=2
#4 RH=True VD=False G=2
#5 RH=False VD=True G=2
#5 RH=True VD=False G=2
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} G={tc.Group?.TestGroupIndex}"
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
        [TestCategory("Test cases")]
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

            // Assert assembly file path
            Assert.AreEqual(1, actual.AssemblyFilePaths.Count);
            Assert.AreEqual(assemblyFilePath, actual.AssemblyFilePaths[0]);
            Assert.AreEqual(0, (from tc in actual.TestCases
                                where tc.AssemblyFilePath != assemblyFilePath
                                select tc).Count());

            // Assert collection, index, FQN and name
            Assert.AreEqual(
$@"#1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [Virtual Device]'
#1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [Real hardware]'
#2 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Virtual Device]'
#2 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Real hardware]'
#3 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Virtual Device]'
#3 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Real hardware]'
#4 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method 'Method [Virtual Device]'
#4 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method 'Method [Real hardware]'
#5 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1 'Method1 [Virtual Device]'
#5 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1 'Method1 [Real hardware]'
#6 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2 'Method2 [Virtual Device]'
#6 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2 'Method2 [Real hardware]'
#7 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray 'TestThatIsNowInDisarray [Virtual Device]'
#7 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray 'TestThatIsNowInDisarray [Real hardware]'
#8 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile 'TestOnDeviceWithSomeFile [Virtual Device]'
#8 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile 'TestOnDeviceWithSomeFile [Real hardware]'
#9 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Virtual Device]'
#9 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Real hardware]'
#10 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Virtual Device]'
#10 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Real hardware]'
#11 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits 'MethodWithTraits [Virtual Device]'
#11 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits 'MethodWithTraits [Real hardware]'
#12 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods [Virtual Device]'
#12 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods [Real hardware]'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} {tc.FullyQualifiedName} '{tc.DisplayName}'"
                ) + '\n'
            );
            Assert.AreEqual(12, actual.TestMethodsInAssembly(assemblyFilePath));

            // Assert source location and traits
            Assert.AreEqual(
$@"#1 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device'
#1 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@test', '@Real hardware'
#2 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device'
#2 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@test', '@Real hardware'
#3 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device'
#3 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@test', '@Real hardware'
#4 @{pathPrefix}TestClassVariants.cs(13,28) '@Virtual Device'
#4 @{pathPrefix}TestClassVariants.cs(13,28) '@test', '@Real hardware'
#5 @{pathPrefix}TestClassVariants.cs(33,21) '@Virtual Device'
#5 @{pathPrefix}TestClassVariants.cs(33,21) '@test', '@Real hardware'
#6 @{pathPrefix}TestClassVariants.cs(40,21) '@Virtual Device'
#6 @{pathPrefix}TestClassVariants.cs(40,21) '@test', '@Real hardware'
#7 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@Virtual Device'
#7 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@test', '@Real hardware'
#8 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@Virtual Device'
#8 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@test', '@DeviceWithSomeFile', '@Real hardware'
#9 @{pathPrefix}TestWithMethods.cs(13,21) '@Virtual Device'
#9 @{pathPrefix}TestWithMethods.cs(13,21) '@test', '@Real hardware'
#10 @{pathPrefix}TestWithMethods.cs(18,21) '@Virtual Device'
#10 @{pathPrefix}TestWithMethods.cs(18,21) '@test', '@Real hardware'
#11 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(15,21) '@Virtual Device'
#11 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(15,21) '@test', '@Real hardware'
#12 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(20,21) '@Virtual Device'
#12 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(20,21) '@test', '@esp32', '@Real hardware'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} @{tc.TestMethodSourceCodeLocation?.ForMessage()} {string.Join(", ", from t in tc.Traits select $"'{t}'")}"
                ) + '\n'
            );

            // Assert run information
            Assert.AreEqual(
$@"#1 RH=False VD=True G=1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
#1 RH=True VD=False G=1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
#2 RH=False VD=True G=1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
#2 RH=True VD=False G=1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
#3 RH=False VD=True G=1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
#3 RH=True VD=False G=1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
#4 RH=False VD=True G=2 FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method
#4 RH=True VD=False G=2 FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method
#5 RH=False VD=True G=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1
#5 RH=True VD=False G=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1
#6 RH=False VD=True G=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2
#6 RH=True VD=False G=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2
#7 RH=False VD=True G=5 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray
#7 RH=True VD=False G=5 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray
#8 RH=False VD=True G=5 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
#8 RH=True VD=False G=5 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
#9 RH=False VD=True G=6 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
#9 RH=True VD=False G=6 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
#10 RH=False VD=True G=6 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
#10 RH=True VD=False G=6 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
#11 RH=False VD=True G=7 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits
#11 RH=True VD=False G=7 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits
#12 RH=False VD=True G=7 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods
#12 RH=True VD=False G=7 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} G={tc.Group?.TestGroupIndex} FQN={tc.FullyQualifiedName}"
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
        [TestCategory("Test cases")]
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

            // Assert assembly file path
            Assert.AreEqual(1, actual.AssemblyFilePaths.Count);
            Assert.AreEqual(assemblyFilePath, actual.AssemblyFilePaths[0]);
            Assert.AreEqual(0, (from tc in actual.TestCases
                                where tc.AssemblyFilePath != assemblyFilePath
                                select tc).Count());

            // Assert collection, index, FQN and name
            Assert.AreEqual(
$@"#1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod'
#2 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1)'
#3 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2)'
#4 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method 'Method'
#5 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1 'Method1'
#6 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2 'Method2'
#7 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray 'TestThatIsNowInDisarray'
#8 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile 'TestOnDeviceWithSomeFile'
#9 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test'
#10 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2'
#11 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits 'MethodWithTraits'
#12 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} {tc.FullyQualifiedName} '{tc.DisplayName}'"
                ) + '\n'
            );
            Assert.AreEqual(12, actual.TestMethodsInAssembly(assemblyFilePath));

            // Assert source location and traits
            Assert.AreEqual(
$@"#1 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device'
#2 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device'
#3 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device'
#4 @{pathPrefix}TestClassVariants.cs(13,28) '@Virtual Device'
#5 @{pathPrefix}TestClassVariants.cs(33,21) '@Virtual Device'
#6 @{pathPrefix}TestClassVariants.cs(40,21) '@Virtual Device'
#7 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@Virtual Device'
#8 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@Virtual Device'
#9 @{pathPrefix}TestWithMethods.cs(13,21) '@Virtual Device'
#10 @{pathPrefix}TestWithMethods.cs(18,21) '@Virtual Device'
#11 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(15,21) '@Virtual Device'
#12 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(20,21) '@Virtual Device'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} @{tc.TestMethodSourceCodeLocation?.ForMessage()} {string.Join(", ", from t in tc.Traits select $"'{t}'")}"
                ) + '\n'
            );

            // Assert run information
            Assert.AreEqual(
$@"#1 RH=False VD=True G=1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
#2 RH=False VD=True G=1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
#3 RH=False VD=True G=1 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
#4 RH=False VD=True G=2 FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method
#5 RH=False VD=True G=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1
#6 RH=False VD=True G=3 FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2
#7 RH=False VD=True G=5 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray
#8 RH=False VD=True G=5 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
#9 RH=False VD=True G=6 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
#10 RH=False VD=True G=6 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
#11 RH=False VD=True G=7 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits
#12 RH=False VD=True G=7 FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} G={tc.Group?.TestGroupIndex} FQN={tc.FullyQualifiedName}"
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
        [TestCategory("Test cases")]
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

            // Assert that only the NFUnitTests assemblies are included
            Assert.AreEqual(
$@"{assemblyFilePath1}
{assemblyFilePath2}
".Replace("\r\n", "\n"),
                string.Join("\n",
                        actual.AssemblyFilePaths
                    ) + '\n'
            );

            // Test methods
            Assert.AreEqual(5, actual.TestMethodsInAssembly(assemblyFilePath1));
            Assert.AreEqual(12, actual.TestMethodsInAssembly(assemblyFilePath2));
            Assert.AreEqual(0, actual.TestMethodsInAssembly(assemblyFilePath3));
        }

        [TestMethod]
        [TestCategory("Test cases")]
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
            Assert.AreEqual(withLogger.TestCases.Count, actual.TestCases.Count);
        }

        [TestMethod]
        [TestCategory("Test cases")]
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
            Assert.AreEqual(withLogger.TestCases.Count, actual.TestCases.Count);
        }
        #endregion

        #region Selection
        [TestMethod]
        [TestCategory("Test cases")]
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
            if (original.TestCases.Count == 0)
            {
                Assert.Inconclusive("Original collection of test cases could not be constructed");
            }
            string expectedDiscoveryMessages = string.Join("\n",
                                                from m in logger.Messages
                                                select $"{m.level}: {m.message}"
                                            ) + '\n';

            // Select all test cases
            logger = new LogMessengerMock();
            var actual = new TestCaseCollection(from tc in original.TestCases.Reverse()
                                                select (tc.AssemblyFilePath, tc.DisplayName, tc.FullyQualifiedName),
                                                (f) => ProjectSourceInventory.FindProjectFilePath(f, logger),
                                                selectionAllowHardware,
                                                out Dictionary<int, int> testCaseIndex,
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
                    select $"#{tc.TestIndex} ({tc.FullyQualifiedName}) {s_stripDevice.Replace(tc.DisplayName, "")} VD={tc.ShouldRunOnVirtualDevice} RH={tc.ShouldRunOnRealHardware}"
                ) + '\n',
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} ({tc.FullyQualifiedName}) {s_stripDevice.Replace(tc.DisplayName, "")} VD={tc.ShouldRunOnVirtualDevice} RH={tc.ShouldRunOnRealHardware}"
                ) + '\n'
            );

            // Assert the testCaseIndex
            if (originalAllowHardware == selectionAllowHardware)
            {
                foreach (KeyValuePair<int, int> index in testCaseIndex)
                {
                    Assert.AreEqual(original.TestCases.Count - 1 - index.Key, index.Value);
                }
            }
        }
        private static readonly Regex s_stripDevice = new Regex(@"\s\[[^]]+\]", RegexOptions.Compiled);

        [TestMethod]
        [TestCategory("Test cases")]
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
            if (original.TestCases.Count == 0)
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
@"#2 (TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1) TestMethod1(1,1) [Real hardware]
#11 (TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits) MethodWithTraits [Virtual Device]
#11 (TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits) MethodWithTraits [Real hardware]
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} ({tc.FullyQualifiedName}) {tc.DisplayName}"
                ) + '\n'
            );
        }
        #endregion
    }
}
