// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            logger.AssertEqual(
$@"Detailed: {pathPrefix}TestAllCurrentAttributes.cs(13,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Detailed: {pathPrefix}TestAllCurrentAttributes.cs(19,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Detailed: {pathPrefix}TestWithMethods.cs(9,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Detailed: {pathPrefix}TestWithMethods.cs(14,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.");

            AssertTestCaseCollectionFQNDisplayName(actual.TestCases,
$@"G000T000 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod'
G000T001D00 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1)'
G000T001D01 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2)'
G001T000 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test'
G001T001 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2'");

            AssertSourceLocationTraits(actual.TestCases,
$@"G000T000 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device' DC()
G000T001D00 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device' DC()
G000T001D01 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device' DC()
G001T000 @{pathPrefix}TestWithMethods.cs(9,21) '@Virtual Device' DC()
G001T001 @{pathPrefix}TestWithMethods.cs(14,21) '@Virtual Device' DC()");

            AssertRunInformation(actual.TestCases,
$@"G000T000 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
G000T001D00 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G000T001D01 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G001T000 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
G001T001 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2");

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
            logger.AssertEqual(
$@"Detailed: {pathPrefix}TestAllCurrentAttributes.cs(13,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Detailed: {pathPrefix}TestAllCurrentAttributes.cs(19,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Detailed: {pathPrefix}TestWithMethods.cs(9,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Detailed: {pathPrefix}TestWithMethods.cs(14,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.");

            AssertTestCaseCollectionFQNDisplayName(actual.TestCases,
$@"G000T000 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [Virtual Device]'
G000T000 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [Real hardware]'
G000T001D00 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Virtual Device]'
G000T001D00 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Real hardware]'
G000T001D01 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Virtual Device]'
G000T001D01 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Real hardware]'
G001T000 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Virtual Device]'
G001T000 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Real hardware]'
G001T001 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Virtual Device]'
G001T001 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Real hardware]'");

            AssertSourceLocationTraits(actual.TestCases,
$@"G000T000 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device' DC()
G000T000 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Real hardware' DC()
G000T001D00 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device' DC()
G000T001D00 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Real hardware' DC()
G000T001D01 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device' DC()
G000T001D01 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Real hardware' DC()
G001T000 @{pathPrefix}TestWithMethods.cs(9,21) '@Virtual Device' DC()
G001T000 @{pathPrefix}TestWithMethods.cs(9,21) '@Real hardware' DC()
G001T001 @{pathPrefix}TestWithMethods.cs(14,21) '@Virtual Device' DC()
G001T001 @{pathPrefix}TestWithMethods.cs(14,21) '@Real hardware' DC()");

            AssertRunInformation(actual.TestCases,
$@"G000T000 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
G000T000 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
G000T001D00 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G000T001D00 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G000T001D01 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G000T001D01 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G001T000 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
G001T000 RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
G001T001 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
G001T001 RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2");

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
                    Assert.IsTrue(testCase.RealHardwareDeviceSelectors?.Any());
                    Assert.IsTrue((from s in testCase.RealHardwareDeviceSelectors
                                   where s.ShouldTestOnDevice(anyDevice)
                                   select s).Any());
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
            logger.AssertEqual(
$@"Warning: {pathPrefix}TestWithALotOfErrors.cs(10,6): Warning: Only one attribute that implements 'ITestClass' is allowed. Only the first one is used, subsequent attributes are ignored.
Error: {pathPrefix}TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]', 'int', 'long' or 'string'.
Error: {pathPrefix}TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.
Warning: {pathPrefix}TestWithALotOfErrors.cs(41,21): Warning: No other attributes are allowed when the attributes that implement 'ICleanup'/'IDeploymentConfiguration'/'ISetup' are present. Extra attributes are ignored.
Error: {pathPrefix}TestWithALotOfErrors.cs(55,10): Error: The number of arguments of the method does not match the number of configuration keys specified by the attribute that implements 'IDeploymentConfiguration'.");

            AssertTestCaseCollectionFQNDisplayName(actual.TestCases,
$@"G001T000 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [Virtual Device]'
G001T000 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [Real hardware]'
G001T001D00 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Virtual Device]'
G001T001D00 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Real hardware]'
G001T001D01 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Virtual Device]'
G001T001D01 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Real hardware]'
G002T000 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method 'Method [Virtual Device]'
G002T000 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method 'Method [Real hardware]'
G003T000 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1 'Method1 [Virtual Device]'
G003T000 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1 'Method1 [Real hardware]'
G003T001 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2 'Method2 [Virtual Device]'
G003T001 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2 'Method2 [Real hardware]'
G005T000 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray 'TestThatIsNowInDisarray [Virtual Device]'
G005T000 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray 'TestThatIsNowInDisarray [Real hardware]'
G005T001 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile 'TestOnDeviceWithSomeFile [Virtual Device]'
G005T001 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile 'TestOnDeviceWithSomeFile [Real hardware]'
G006T000 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Virtual Device]'
G006T000 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Real hardware]'
G006T001 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Virtual Device]'
G006T001 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Real hardware]'
G007T000 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits 'MethodWithTraits [Virtual Device]'
G007T000 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits 'MethodWithTraits [Real hardware]'
G007T001 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods [Virtual Device]'
G007T001 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods [Real hardware]'");

            AssertSourceLocationTraits(actual.TestCases,
$@"G001T000 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device' DC()
G001T000 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@test', '@Real hardware' DC()
G001T001D00 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device' DC()
G001T001D00 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@test', '@Real hardware' DC()
G001T001D01 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device' DC()
G001T001D01 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@test', '@Real hardware' DC()
G002T000 @{pathPrefix}TestClassVariants.cs(13,28) '@Virtual Device' DC()
G002T000 @{pathPrefix}TestClassVariants.cs(13,28) '@test', '@Real hardware' DC()
G003T000 @{pathPrefix}TestClassVariants.cs(33,21) '@Virtual Device' DC()
G003T000 @{pathPrefix}TestClassVariants.cs(33,21) '@test', '@Real hardware' DC()
G003T001 @{pathPrefix}TestClassVariants.cs(40,21) '@Virtual Device' DC()
G003T001 @{pathPrefix}TestClassVariants.cs(40,21) '@test', '@Real hardware' DC()
G005T000 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@Virtual Device' DC()
G005T000 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@test', '@Real hardware' DC()
G005T001 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@Virtual Device' DC()
G005T001 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@test', '@DeviceWithSomeFile', '@Real hardware' DC()
G006T000 @{pathPrefix}TestWithMethods.cs(16,21) '@Virtual Device' DC()
G006T000 @{pathPrefix}TestWithMethods.cs(16,21) '@test', '@Real hardware' DC()
G006T001 @{pathPrefix}TestWithMethods.cs(21,21) '@Virtual Device' DC(Byte[] 'Make and model')
G006T001 @{pathPrefix}TestWithMethods.cs(21,21) '@test', '@Real hardware' DC(Byte[] 'Make and model')
G007T000 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(15,21) '@Virtual Device' DC()
G007T000 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(15,21) '@test', '@Real hardware' DC()
G007T001 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(21,21) '@Virtual Device' DC(Int32 'RGB LED pin', Int64 'Device ID')
G007T001 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(21,21) '@test', '@esp32', '@Real hardware' DC(Int32 'RGB LED pin', Int64 'Device ID')");

            AssertRunInformation(actual.TestCases,
$@"G001T000 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
G001T000 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
G001T001D00 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G001T001D00 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G001T001D01 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G001T001D01 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G002T000 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method
G002T000 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method
G003T000 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1
G003T000 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1
G003T001 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2
G003T001 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2
G005T000 RH=False VD=True GS=Setup(String 'xyzzy', Int64 'Device ID', Int32 'Address') GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray
G005T000 RH=True VD=False GS=Setup(String 'xyzzy', Int64 'Device ID', Int32 'Address') GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray
G005T001 RH=False VD=True GS=Setup(String 'xyzzy', Int64 'Device ID', Int32 'Address') GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
G005T001 RH=True VD=False GS=Setup(String 'xyzzy', Int64 'Device ID', Int32 'Address') GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
G006T000 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
G006T000 RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
G006T001 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
G006T001 RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
G007T000 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits
G007T000 RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits
G007T001 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods
G007T001 RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods");

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
                    Assert.IsTrue(testCase.RealHardwareDeviceSelectors?.Any());
                    Assert.IsTrue((from s in testCase.RealHardwareDeviceSelectors
                                   where s.ShouldTestOnDevice(esp32Device)
                                   select s).Any());
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
            logger.AssertEqual(
$@"Warning: {pathPrefix}TestWithALotOfErrors.cs(10,6): Warning: Only one attribute that implements 'ITestClass' is allowed. Only the first one is used, subsequent attributes are ignored.
Error: {pathPrefix}TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]', 'int', 'long' or 'string'.
Error: {pathPrefix}TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.
Warning: {pathPrefix}TestWithALotOfErrors.cs(41,21): Warning: No other attributes are allowed when the attributes that implement 'ICleanup'/'IDeploymentConfiguration'/'ISetup' are present. Extra attributes are ignored.
Error: {pathPrefix}TestWithALotOfErrors.cs(55,10): Error: The number of arguments of the method does not match the number of configuration keys specified by the attribute that implements 'IDeploymentConfiguration'.");

            AssertTestCaseCollectionFQNDisplayName(actual.TestCases,
$@"G001T000 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod'
G001T001D00 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1)'
G001T001D01 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2)'
G002T000 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method 'Method'
G003T000 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1 'Method1'
G003T001 TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2 'Method2'
G005T000 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray 'TestThatIsNowInDisarray'
G005T001 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile 'TestOnDeviceWithSomeFile'
G006T000 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test'
G006T001 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2'
G007T000 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits 'MethodWithTraits'
G007T001 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods'");

            AssertSourceLocationTraits(actual.TestCases,
$@"G001T000 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device' DC()
G001T001D00 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device' DC()
G001T001D01 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device' DC()
G002T000 @{pathPrefix}TestClassVariants.cs(13,28) '@Virtual Device' DC()
G003T000 @{pathPrefix}TestClassVariants.cs(33,21) '@Virtual Device' DC()
G003T001 @{pathPrefix}TestClassVariants.cs(40,21) '@Virtual Device' DC()
G005T000 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@Virtual Device' DC()
G005T001 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@Virtual Device' DC()
G006T000 @{pathPrefix}TestWithMethods.cs(16,21) '@Virtual Device' DC()
G006T001 @{pathPrefix}TestWithMethods.cs(21,21) '@Virtual Device' DC(Byte[] 'Make and model')
G007T000 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(15,21) '@Virtual Device' DC()
G007T001 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(21,21) '@Virtual Device' DC(Int32 'RGB LED pin', Int64 'Device ID')");

            AssertRunInformation(actual.TestCases,
$@"G001T000 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
G001T001D00 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G001T001D01 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
G002T000 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method
G003T000 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1
G003T001 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2
G005T000 RH=False VD=True GS=Setup(String 'xyzzy', Int64 'Device ID', Int32 'Address') GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray
G005T001 RH=False VD=True GS=Setup(String 'xyzzy', Int64 'Device ID', Int32 'Address') GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
G006T000 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
G006T001 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
G007T000 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits
G007T001 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods");

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
            logger.AssertEqual(
$@"Detailed: {pathPrefix1}TestAllCurrentAttributes.cs(13,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Detailed: {pathPrefix1}TestAllCurrentAttributes.cs(19,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Detailed: {pathPrefix1}TestWithMethods.cs(9,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Detailed: {pathPrefix1}TestWithMethods.cs(14,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Warning: {pathPrefix2}TestWithALotOfErrors.cs(10,6): Warning: Only one attribute that implements 'ITestClass' is allowed. Only the first one is used, subsequent attributes are ignored.
Error: {pathPrefix2}TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]', 'int', 'long' or 'string'.
Error: {pathPrefix2}TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.
Warning: {pathPrefix2}TestWithALotOfErrors.cs(41,21): Warning: No other attributes are allowed when the attributes that implement 'ICleanup'/'IDeploymentConfiguration'/'ISetup' are present. Extra attributes are ignored.
Error: {pathPrefix2}TestWithALotOfErrors.cs(55,10): Error: The number of arguments of the method does not match the number of configuration keys specified by the attribute that implements 'IDeploymentConfiguration'.
Verbose: Project file for assembly 'C:\Projects\GH\nanoFramework\Contributions\Test attributes\nF_nanoFramework.TestFramework\tests\TestFramework.Tooling.Tests\bin\Debug\nanoFramework.TestFramework.Tooling.dll' not found.");

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
            string expectedDiscoveryMessages = logger.ToString();

            // Select all test cases
            var selectionSpecification = (from tc in original.TestCases
                                          orderby tc.AssemblyFilePath, tc.ShouldRunOnVirtualDevice ? 0 : 1, tc.TestCaseId.GetHashCode()
                                          select (tc.AssemblyFilePath, tc.DisplayName, tc.FullyQualifiedName)).ToList();
            logger = new LogMessengerMock();
            var actual = new TestCaseCollection(selectionSpecification,
                                                (f) => ProjectSourceInventory.FindProjectFilePath(f, logger),
                                                selectionAllowHardware,
                                                logger);

            // Assert that there are no selection-related messages
            logger.AssertEqual(expectedDiscoveryMessages);

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
                    (string assemblyFilePath, string displayName, string fullyQualifiedName) = selectionSpecification[selectionIndex];

                    Assert.AreEqual(testCase.AssemblyFilePath, assemblyFilePath);
                    Assert.AreEqual(testCase.FullyQualifiedName, fullyQualifiedName);

                    int idx = testCase.DisplayName.IndexOf('[');
                    string displayBaseName = idx < 0 ? testCase.DisplayName : testCase.DisplayName.Substring(0, idx).Trim();
                    string deviceName = $"{displayBaseName} [Virtual Device]";
                    Assert.IsTrue(displayName == displayBaseName || displayName == deviceName);
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
            string expectedDiscoveryMessages = logger.ToString();

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
            logger.AssertEqual(
                expectedDiscoveryMessages +
$@"Verbose: Test case 'TestMethod1(2,2) [some_platform]' (TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1) from '{assemblyFilePath1}' is no longer available
Verbose: Test case 'NoSuchMethod' (TestFramework.Tooling.Tests.NFUnitTest.NoSuchClass.NoSuchMethod) from '{assemblyFilePath1}' is no longer available
Verbose: Test case 'Method2 [Real hardware]' (TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2) from '{assemblyFilePath1}' is no longer available");

            // Assert that the selected test case are present
            Assert.AreEqual(
@"G000T001D00 (TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1) TestMethod1(1,1) [Real hardware]
G007T000 (TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits) MethodWithTraits [Virtual Device]
G007T000 (TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits) MethodWithTraits [Real hardware]
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} ({tc.FullyQualifiedName}) {tc.DisplayName}"
                ) + '\n'
            );
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Assert collection, FQN and display name
        /// </summary>
        private static void AssertTestCaseCollectionFQNDisplayName(IEnumerable<TestCase> actual, string expected)
        {
            Assert.AreEqual(
                expected.Trim().Replace("\r\n", "\n") + '\n',
                string.Join("\n",
                    from tc in actual
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} {tc.FullyQualifiedName} '{tc.DisplayName}'"
                ) + '\n'
            );
        }

        /// <summary>
        /// Assert source location of the tests, and the traits
        /// </summary>
        private static void AssertSourceLocationTraits(IEnumerable<TestCase> actual, string expected)
        {
            Assert.AreEqual(
                expected.Trim().Replace("\r\n", "\n") + '\n',
                string.Join("\n",
                    from tc in actual
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} @{tc.TestMethodSourceCodeLocation?.ForMessage()} {string.Join(", ", from t in tc.Traits select $"'{t}'")} DC({string.Join(", ", from t in tc.RequiredConfigurationKeys select $"{t.valueType.Name} '{t.key}'")})"
                ) + '\n'
            );
        }

        /// <summary>
        /// Assert the properties relevant for running the tests
        /// </summary>
        private static void AssertRunInformation(IEnumerable<TestCase> actual, string expected)
        {
            string SetupMethodString(IReadOnlyList<TestCaseGroup.SetupMethod> setupMethods)
            {
                if (setupMethods is null || setupMethods.Count == 0)
                {
                    return "";
                }
                var result = new StringBuilder();
                foreach (TestCaseGroup.SetupMethod setupMethod in setupMethods)
                {
                    if (result.Length > 0)
                    {
                        result.Append(";");
                    }
                    result.Append($"{setupMethod.MethodName}({string.Join(", ", from t in setupMethod.RequiredConfigurationKeys select $"{t.valueType.Name} '{t.key}'")})");
                }
                return result.ToString();
            }
            string CleanMethodString(IReadOnlyList<TestCaseGroup.CleanupMethod> cleanupMethods)
            {
                if (cleanupMethods is null || cleanupMethods.Count == 0)
                {
                    return "";
                }
                return string.Join(";", from c in cleanupMethods select c.MethodName);
            }

            Assert.AreEqual(
                expected.Trim().Replace("\r\n", "\n") + '\n',
                string.Join("\n",
                    from tc in actual
                    orderby tc.AssemblyFilePath, tc.TestCaseId, tc.ShouldRunOnVirtualDevice ? 0 : 1
                    select $"{tc.TestCaseId} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} GS={SetupMethodString(tc.Group?.SetupMethods)} GC={CleanMethodString(tc.Group?.CleanupMethods)} FQN={tc.FullyQualifiedName}"
                ) + '\n'
            );
        }
        #endregion
    }
}
