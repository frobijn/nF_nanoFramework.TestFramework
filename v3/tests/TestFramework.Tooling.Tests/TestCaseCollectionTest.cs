// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework;
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
        #region TestFramework.Tooling.Tests.Original.v2
        [TestMethod]
        public void TestCases_Original_v2()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Original.v2");
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);
            var logger = new LogMessengerMock();
            string pathPrefix = Path.GetDirectoryName(projectFilePath) + Path.DirectorySeparatorChar;

            var actual = new TestCaseCollection(assemblyFilePath, projectFilePath, logger);

            Assert.IsNotNull(actual.TestCases);
            logger.AssertEqual("");

            AssertTestCaseCollectionFQNDisplayName(actual.TestCases, "");
        }
        #endregion

        #region TestFramework.Tooling.Tests.Discovery.v2
        [TestMethod]
        public void TestCases_Discovery_v2()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v2");
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);
            var logger = new LogMessengerMock();
            string pathPrefix = Path.GetDirectoryName(projectFilePath) + Path.DirectorySeparatorChar;

            var actual = new TestCaseCollection(assemblyFilePath, projectFilePath, logger);

            Assert.IsNotNull(actual.TestCases);
            logger.AssertEqual(
$@"Detailed: {pathPrefix}TestAllCurrentAttributes.cs(13,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Detailed: {pathPrefix}TestAllCurrentAttributes.cs(19,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Detailed: {pathPrefix}TestWithMethods.cs(9,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.
Detailed: {pathPrefix}TestWithMethods.cs(14,21): Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.");

            AssertTestCaseCollectionFQNDisplayName(actual.TestCases,
$@"bdd27c86e409fa6571be00f0b9dbbbb6 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod(H)' TestMethod 'TestMethod [{Constants.RealHardware_Description}]'
65838de5013775d71d010d247f8e5aff 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod(V)' TestMethod 'TestMethod [{Constants.VirtualDevice_Description}]'
226da6ce16710bd2f34ad83199ace342 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,H)' TestMethod1 'TestMethod1(1,1) [{Constants.RealHardware_Description}]'
d8abc492bf942414cdd239e220d38059 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,V)' TestMethod1 'TestMethod1(1,1) [{Constants.VirtualDevice_Description}]'
74c4e9b5dcf19adb4f9cb992ae99495d 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(1,H)' TestMethod1 'TestMethod1(2,2) [{Constants.RealHardware_Description}]'
15d9e49c18629d0be187064d41fb7d0f 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(1,V)' TestMethod1 'TestMethod1(2,2) [{Constants.VirtualDevice_Description}]'
c0cc03d124355e676b8e4bc8a42d4fb7 'TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test(H)' Test 'Test [{Constants.RealHardware_Description}]'
003dc3fc49fd019df294d6b401f8f22b 'TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test(V)' Test 'Test [{Constants.VirtualDevice_Description}]'
9547eb5c688c03c8385ad17dd0c9bef6 'TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2(H)' Test2 'Test2 [{Constants.RealHardware_Description}]'
a1a079b0db290aa5ecc8d9127cabb92d 'TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2(V)' Test2 'Test2 [{Constants.VirtualDevice_Description}]'");

            AssertSourceLocationCategories(actual.TestCases,
$@"bdd27c86e409fa6571be00f0b9dbbbb6 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '{Constants.RealHardware_TestCategory}' DC()
65838de5013775d71d010d247f8e5aff @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '{Constants.VirtualDevice_TestCategory}' DC()
226da6ce16710bd2f34ad83199ace342 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '{Constants.RealHardware_TestCategory}' DC()
d8abc492bf942414cdd239e220d38059 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '{Constants.VirtualDevice_TestCategory}' DC()
74c4e9b5dcf19adb4f9cb992ae99495d @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '{Constants.RealHardware_TestCategory}' DC()
15d9e49c18629d0be187064d41fb7d0f @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '{Constants.VirtualDevice_TestCategory}' DC()
c0cc03d124355e676b8e4bc8a42d4fb7 @{pathPrefix}TestWithMethods.cs(9,21) '{Constants.RealHardware_TestCategory}' DC()
003dc3fc49fd019df294d6b401f8f22b @{pathPrefix}TestWithMethods.cs(9,21) '{Constants.VirtualDevice_TestCategory}' DC()
9547eb5c688c03c8385ad17dd0c9bef6 @{pathPrefix}TestWithMethods.cs(14,21) '{Constants.RealHardware_TestCategory}' DC()
a1a079b0db290aa5ecc8d9127cabb92d @{pathPrefix}TestWithMethods.cs(14,21) '{Constants.VirtualDevice_TestCategory}' DC()");

            AssertRunInformation(actual.TestCases,
$@"
bdd27c86e409fa6571be00f0b9dbbbb6 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod(H)
65838de5013775d71d010d247f8e5aff RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod(V)
226da6ce16710bd2f34ad83199ace342 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,H)
d8abc492bf942414cdd239e220d38059 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,V)
74c4e9b5dcf19adb4f9cb992ae99495d RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(1,H)
15d9e49c18629d0be187064d41fb7d0f RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(1,V)
c0cc03d124355e676b8e4bc8a42d4fb7 RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test(H)
003dc3fc49fd019df294d6b401f8f22b RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test(V)
9547eb5c688c03c8385ad17dd0c9bef6 RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2(H)
a1a079b0db290aa5ecc8d9127cabb92d RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2(V)");

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

            var actual = new TestCaseCollection(assemblyFilePath, projectFilePath, logger);

            Assert.IsNotNull(actual.TestCases);
            logger.AssertEqual(
$@"Warning: {pathPrefix}TestWithALotOfErrors.cs(10,6): Warning: Only one attribute that implements 'ITestClass' is allowed. Only the first one is used, subsequent attributes are ignored.
Error: {pathPrefix}TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]', 'int', 'long' or 'string'.
Error: {pathPrefix}TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.
Warning: {pathPrefix}TestWithALotOfErrors.cs(41,21): Warning: No other attributes are allowed when the attributes that implement 'ICleanup'/'IDeploymentConfiguration'/'ISetup' are present. Extra attributes are ignored.
Error: {pathPrefix}TestWithALotOfErrors.cs(55,10): Error: The number of arguments of the method does not match the number of configuration keys specified by the attribute that implements 'IDeploymentConfiguration'.");

            AssertTestCaseCollectionFQNDisplayName(actual.TestCases,
$@"da8179d6ea2e83a5b61ee50a44c17531 'TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1(H)' Method1 'Method1 [{Constants.RealHardware_Description}]'
aa63fbb99e8b200516280d443e0d277d 'TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1(V)' Method1 'Method1 [{Constants.VirtualDevice_Description}]'
744bb7b504486ae1aa901a583c0b115e 'TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2(H)' Method2 'Method2 [{Constants.RealHardware_Description}]'
56bb901b20d9bd73b5853c78eac7cbf7 'TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2(V)' Method2 'Method2 [{Constants.VirtualDevice_Description}]'
bfbff67071f41af4b42bd7e759fc6327 'TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method(H)' Method 'Method [{Constants.RealHardware_Description}]'
617ca895b9a042c97e1f054789ee5761 'TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method(V)' Method 'Method [{Constants.VirtualDevice_Description}]'
2e26e846f84aeef331bd2ff84b90dd67 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod(H)' TestMethod 'TestMethod [{Constants.RealHardware_Description}]'
37c6ec5990f5a853218f76821a5c722a 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod(V)' TestMethod 'TestMethod [{Constants.VirtualDevice_Description}]'
4e33494c66bec3b773c137e84a71c350 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,H)' TestMethod1 'TestMethod1(1,1) [{Constants.RealHardware_Description}]'
66be3356034131538150f0b896ce59b5 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,V)' TestMethod1 'TestMethod1(1,1) [{Constants.VirtualDevice_Description}]'
11f2fda7e15c757e8e287fe962bdae46 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(1,H)' TestMethod1 'TestMethod1(2,2) [{Constants.RealHardware_Description}]'
1351080163b5deceb9c6524a2a3fe70d 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(1,V)' TestMethod1 'TestMethod1(2,2) [{Constants.VirtualDevice_Description}]'
d0a530fa5d890d3a69bd8f8d6f8e9dac 'TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile(H)' TestOnDeviceWithSomeFile 'TestOnDeviceWithSomeFile [{Constants.RealHardware_Description}]'
85f57b33220030021b8bac3bfb97e1cf 'TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile(V)' TestOnDeviceWithSomeFile 'TestOnDeviceWithSomeFile [{Constants.VirtualDevice_Description}]'
6b1afc5c66a7b9ade3ce5ac1aae501ec 'TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray(H)' TestThatIsNowInDisarray 'TestThatIsNowInDisarray [{Constants.RealHardware_Description}]'
484bbbedb5fa6ac326962528321d6e20 'TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray(V)' TestThatIsNowInDisarray 'TestThatIsNowInDisarray [{Constants.VirtualDevice_Description}]'
6e7c86e9413e1c146a76e11d0d047d6e 'TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test(H)' Test 'Test [{Constants.RealHardware_Description}]'
fc3a8adf4c9e4f328f8291616b981637 'TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test(V)' Test 'Test [{Constants.VirtualDevice_Description}]'
03b81bed3e6ed7a91a6de5381588f5d2 'TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2(H)' Test2 'Test2 [{Constants.RealHardware_Description}]'
ffb799c935dc7c2ef878c4e3c8962517 'TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2(V)' Test2 'Test2 [{Constants.VirtualDevice_Description}]'
cb7e8546af000da23a8a3247b9cb7ef1 'TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories(H)' MethodWithCategories 'MethodWithCategories [{Constants.RealHardware_Description}]'
11445655632a0d3c27a63b4c124c99ea 'TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories(V)' MethodWithCategories 'MethodWithCategories [{Constants.VirtualDevice_Description}]'
51cc42b00761fa799cef6fd617bc9b60 'TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods(H)' MethodWithNewTestMethods 'MethodWithNewTestMethods [{Constants.RealHardware_Description}]'
771b68f14abb4f96e2bfc93e6cb81378 'TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods(V)' MethodWithNewTestMethods 'MethodWithNewTestMethods [{Constants.VirtualDevice_Description}]'");

            AssertSourceLocationCategories(actual.TestCases,
$@"da8179d6ea2e83a5b61ee50a44c17531 @{pathPrefix}TestClassVariants.cs(33,21) '@TEST', '@Hardware nanoDevice' DC()
aa63fbb99e8b200516280d443e0d277d @{pathPrefix}TestClassVariants.cs(33,21) '@Virtual nanoDevice' DC()
744bb7b504486ae1aa901a583c0b115e @{pathPrefix}TestClassVariants.cs(40,21) '@TEST', '@Hardware nanoDevice' DC()
56bb901b20d9bd73b5853c78eac7cbf7 @{pathPrefix}TestClassVariants.cs(40,21) '@Virtual nanoDevice' DC()
bfbff67071f41af4b42bd7e759fc6327 @{pathPrefix}TestClassVariants.cs(13,28) '@TEST', '@Hardware nanoDevice' DC()
617ca895b9a042c97e1f054789ee5761 @{pathPrefix}TestClassVariants.cs(13,28) '@Virtual nanoDevice' DC()
2e26e846f84aeef331bd2ff84b90dd67 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@TEST', '@Hardware nanoDevice' DC()
37c6ec5990f5a853218f76821a5c722a @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual nanoDevice' DC()
4e33494c66bec3b773c137e84a71c350 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@TEST', '@Hardware nanoDevice' DC()
66be3356034131538150f0b896ce59b5 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual nanoDevice' DC()
11f2fda7e15c757e8e287fe962bdae46 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@TEST', '@Hardware nanoDevice' DC()
1351080163b5deceb9c6524a2a3fe70d @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual nanoDevice' DC()
d0a530fa5d890d3a69bd8f8d6f8e9dac @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@TEST', '@DeviceWithSomeFile', '@Hardware nanoDevice' DC()
85f57b33220030021b8bac3bfb97e1cf @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@Virtual nanoDevice' DC()
6b1afc5c66a7b9ade3ce5ac1aae501ec @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@TEST', '@Hardware nanoDevice' DC()
484bbbedb5fa6ac326962528321d6e20 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@Virtual nanoDevice' DC()
6e7c86e9413e1c146a76e11d0d047d6e @{pathPrefix}TestWithMethods.cs(16,21) '@TEST', '@Hardware nanoDevice' DC()
fc3a8adf4c9e4f328f8291616b981637 @{pathPrefix}TestWithMethods.cs(16,21) '@Virtual nanoDevice' DC()
03b81bed3e6ed7a91a6de5381588f5d2 @{pathPrefix}TestWithMethods.cs(21,21) '@TEST', '@Hardware nanoDevice' DC(Byte[] 'Make and model')
ffb799c935dc7c2ef878c4e3c8962517 @{pathPrefix}TestWithMethods.cs(21,21) '@Virtual nanoDevice' DC(Byte[] 'Make and model')
cb7e8546af000da23a8a3247b9cb7ef1 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(15,21) '@TEST', '@Hardware nanoDevice' DC()
11445655632a0d3c27a63b4c124c99ea @{pathPrefix}TestWithNewTestMethodsAttributes.cs(15,21) '@Virtual nanoDevice' DC()
51cc42b00761fa799cef6fd617bc9b60 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(21,21) '@TEST', '@ESP32', '@Hardware nanoDevice' DC(Int32 'RGB LED pin', Int64 'Device ID')
771b68f14abb4f96e2bfc93e6cb81378 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(21,21) '@Virtual nanoDevice' DC(Int32 'RGB LED pin', Int64 'Device ID')");

            AssertRunInformation(actual.TestCases,
$@"da8179d6ea2e83a5b61ee50a44c17531 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1(H)
aa63fbb99e8b200516280d443e0d277d RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1(V)
744bb7b504486ae1aa901a583c0b115e RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2(H)
56bb901b20d9bd73b5853c78eac7cbf7 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2(V)
bfbff67071f41af4b42bd7e759fc6327 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method(H)
617ca895b9a042c97e1f054789ee5761 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method(V)
2e26e846f84aeef331bd2ff84b90dd67 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod(H)
37c6ec5990f5a853218f76821a5c722a RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod(V)
4e33494c66bec3b773c137e84a71c350 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,H)
66be3356034131538150f0b896ce59b5 RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,V)
11f2fda7e15c757e8e287fe962bdae46 RH=True VD=False GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(1,H)
1351080163b5deceb9c6524a2a3fe70d RH=False VD=True GS=Setup() GC=Cleanup FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(1,V)
d0a530fa5d890d3a69bd8f8d6f8e9dac RH=True VD=False GS=Setup(String 'xyzzy', Int64 'Device ID', Int32 'Address') GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile(H)
85f57b33220030021b8bac3bfb97e1cf RH=False VD=True GS=Setup(String 'xyzzy', Int64 'Device ID', Int32 'Address') GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile(V)
6b1afc5c66a7b9ade3ce5ac1aae501ec RH=True VD=False GS=Setup(String 'xyzzy', Int64 'Device ID', Int32 'Address') GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray(H)
484bbbedb5fa6ac326962528321d6e20 RH=False VD=True GS=Setup(String 'xyzzy', Int64 'Device ID', Int32 'Address') GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray(V)
6e7c86e9413e1c146a76e11d0d047d6e RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test(H)
fc3a8adf4c9e4f328f8291616b981637 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test(V)
03b81bed3e6ed7a91a6de5381588f5d2 RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2(H)
ffb799c935dc7c2ef878c4e3c8962517 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2(V)
cb7e8546af000da23a8a3247b9cb7ef1 RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories(H)
11445655632a0d3c27a63b4c124c99ea RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories(V)
51cc42b00761fa799cef6fd617bc9b60 RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods(H)
771b68f14abb4f96e2bfc93e6cb81378 RH=False VD=True GS= GC= FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods(V)");

            // All tests should run somewhere
            Assert.AreEqual(0, (from tc in actual.TestCases
                                where !tc.ShouldRunOnVirtualDevice && !tc.ShouldRunOnRealHardware
                                select tc).Count());

            // Assert selection of real hardware test cases
            var esp32Device = new TestDeviceProxy(new TestDeviceMock(Guid.NewGuid().ToString(), "ESP32"));
            foreach (TestCase testCase in actual.TestCases)
            {
                if (testCase.ShouldRunOnRealHardware && testCase.Categories.Contains("@ESP32"))
                {
                    Assert.IsTrue(testCase.RealHardwareDeviceSelectors?.Any());
                    Assert.IsTrue((from s in testCase.RealHardwareDeviceSelectors
                                   where s.ShouldTestOnDevice(esp32Device)
                                   select s).Any());
                }
            }
        }
        #endregion

        #region TestFramework.Tooling.Tests.Hardware_esp32.v3
        /// <summary>
        /// The purpose of this test is to verify that a test assembly can be analysed
        /// that depends on hardware-specific native assemblies.
        /// </summary>
        [TestMethod]
        public void TestCases_Hardware_esp32_v3()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Hardware_esp32.v3");
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);
            var logger = new LogMessengerMock();
            string pathPrefix = Path.GetDirectoryName(projectFilePath) + Path.DirectorySeparatorChar;

            var actual = new TestCaseCollection(assemblyFilePath, projectFilePath, logger);

            Assert.IsNotNull(actual.TestCases);
            logger.AssertEqual("");

            AssertTestCaseCollectionFQNDisplayName(actual.TestCases,
$@"bd0ab951e7bd52eeb853d76fd2c91a89 'TestFramework.Tooling.Hardware_esp32.Tests.HardwareSpecificTest.UseEsp32NativeAssembly(H)' UseEsp32NativeAssembly 'UseEsp32NativeAssembly'");

            AssertSourceLocationCategories(actual.TestCases,
$@"bd0ab951e7bd52eeb853d76fd2c91a89 @{pathPrefix}HardwareSpecificTest.cs(18,21) '@ESP32', '{Constants.RealHardware_TestCategory}' DC()");

            AssertRunInformation(actual.TestCases,
$@"bd0ab951e7bd52eeb853d76fd2c91a89 RH=True VD=False GS= GC= FQN=TestFramework.Tooling.Hardware_esp32.Tests.HardwareSpecificTest.UseEsp32NativeAssembly(H)");
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
Verbose: Project file for assembly '{assemblyFilePath3}' not found.");

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
                                                    new LogMessengerMock());
            var actual = new TestCaseCollection(new string[] { assemblyFilePath1, assemblyFilePath3, assemblyFilePath2 },
                                                (f) => ProjectSourceInventory.FindProjectFilePath(f, null),
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
                                                    logger);

            logger = new LogMessengerMock();
            var actual = new TestCaseCollection(new string[] { assemblyFilePath1, assemblyFilePath3, assemblyFilePath2 },
                                                (f) => null,
                                                logger);
            Assert.IsNotNull(actual.TestCases);
            Assert.AreEqual(withLogger.TestCases.Count(), actual.TestCases.Count());
        }
        #endregion

        #region Selection
        [TestMethod]
        public void TestCases_NFUnitTests_SelectAllByProperties()
        {
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v2");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath2 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath2);

            var logger = new LogMessengerMock();
            var original = new TestCaseCollection(new string[] { assemblyFilePath1, assemblyFilePath2 },
                                                  (f) => ProjectSourceInventory.FindProjectFilePath(f, null),
                                                  logger);
            if (original.TestCases.Count() == 0)
            {
                Assert.Inconclusive("Original collection of test cases could not be constructed");
            }
            string expectedDiscoveryMessages = logger.ToString();

            // Select all test cases
            var selectionSpecification = (from tc in original.TestCases
                                          orderby tc.AssemblyFilePath, tc.ShouldRunOnVirtualDevice ? 0 : 1, tc.Id.ToString()
                                          select (tc.AssemblyFilePath, tc.FullyQualifiedName)).ToList();
            logger = new LogMessengerMock();
            var actual = new TestCaseCollection(selectionSpecification,
                                                (f) => ProjectSourceInventory.FindProjectFilePath(f, logger),
                                                logger);

            // Assert that there are no selection-related messages
            logger.AssertEqual(expectedDiscoveryMessages);

            // Assert that all test case are present
            Assert.AreEqual(
                string.Join("\n",
                    from tc in original.TestCases
                    orderby tc.AssemblyFilePath, tc.FullyQualifiedName, tc.DisplayName
                    select $"{tc.Id:N} ({tc.FullyQualifiedName}) {s_stripDevice.Replace(tc.DisplayName, "")} VD={tc.ShouldRunOnVirtualDevice} RH={tc.ShouldRunOnRealHardware}"
                ) + '\n',
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.FullyQualifiedName, tc.DisplayName
                    select $"{tc.Id:N} ({tc.FullyQualifiedName}) {s_stripDevice.Replace(tc.DisplayName, "")} VD={tc.ShouldRunOnVirtualDevice} RH={tc.ShouldRunOnRealHardware}"
                ) + '\n'
            );

            // Assert the selection index
            foreach (TestCaseSelection selection in actual.TestOnVirtualDevice)
            {
                foreach ((int selectionIndex, TestCase testCase) in selection.TestCases)
                {
                    Assert.IsTrue(selectionIndex >= 0);
                    (string assemblyFilePath, string fullyQualifiedName) = selectionSpecification[selectionIndex];

                    Assert.AreEqual(testCase.AssemblyFilePath, assemblyFilePath);
                    Assert.AreEqual(testCase.FullyQualifiedName, fullyQualifiedName);
                }
            }
            foreach (TestCaseSelection selection in actual.TestOnRealHardware)
            {
                foreach ((int selectionIndex, TestCase testCase) in selection.TestCases)
                {
                    Assert.IsTrue(selectionIndex >= 0);
                    (string assemblyFilePath, string fullyQualifiedName) = selectionSpecification[selectionIndex];

                    Assert.AreEqual(testCase.AssemblyFilePath, assemblyFilePath);
                    Assert.AreEqual(testCase.FullyQualifiedName, fullyQualifiedName);
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
                                                  logger);
            if (original.TestCases.Count() == 0)
            {
                Assert.Inconclusive("Original collection of test cases could not be constructed");
            }
            string expectedDiscoveryMessages = logger.ToString();

            // Select some test cases
            logger = new LogMessengerMock();
            var actual = new TestCaseCollection(
                new (string, string)[]
                {
                    (assemblyFilePath1, "TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,H)"),
                    (assemblyFilePath1, "TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(2,H)"),
                    (assemblyFilePath1, "TestFramework.Tooling.Tests.NFUnitTest.NoSuchClass.NoSuchMethod(V)"),
                    (assemblyFilePath2, "TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories(V)"),
                    (assemblyFilePath2, "TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories(H)"),
                    (assemblyFilePath1, "TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2(H)"),
                },
                (f) => ProjectSourceInventory.FindProjectFilePath(f, logger),
                logger);

            // Assert the selection-related messages
            logger.AssertEqual(
                expectedDiscoveryMessages +
$@"Verbose: Test case 'TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(2,H)' from '{assemblyFilePath1}' is no longer available
Verbose: Test case 'TestFramework.Tooling.Tests.NFUnitTest.NoSuchClass.NoSuchMethod(V)' from '{assemblyFilePath1}' is no longer available
Verbose: Test case 'TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2(H)' from '{assemblyFilePath1}' is no longer available");

            // Assert that the selected test case are present
            Assert.AreEqual(
$@"226da6ce16710bd2f34ad83199ace342 (TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,H)) TestMethod1(1,1) [{Constants.RealHardware_Description}]
cb7e8546af000da23a8a3247b9cb7ef1 (TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories(H)) MethodWithCategories [{Constants.RealHardware_Description}]
11445655632a0d3c27a63b4c124c99ea (TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories(V)) MethodWithCategories [{Constants.VirtualDevice_Description}]
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    orderby tc.AssemblyFilePath, tc.FullyQualifiedName, tc.DisplayName
                    select $"{tc.Id:N} ({tc.FullyQualifiedName}) {tc.DisplayName}"
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
                    orderby tc.AssemblyFilePath, tc.FullyQualifiedName, tc.DisplayName
                    select $"{tc.Id:N} '{tc.FullyQualifiedName}' {tc.MethodName} '{tc.DisplayName}'"
                ) + '\n'
            );
        }

        /// <summary>
        /// Assert source location of the tests, and the categories
        /// </summary>
        private static void AssertSourceLocationCategories(IEnumerable<TestCase> actual, string expected)
        {
            Assert.AreEqual(
                expected.Trim().Replace("\r\n", "\n") + '\n',
                string.Join("\n",
                    from tc in actual
                    orderby tc.AssemblyFilePath, tc.FullyQualifiedName, tc.DisplayName
                    select $"{tc.Id:N} @{tc.TestMethodSourceCodeLocation?.ForMessage()} {string.Join(", ", from t in tc.Categories select $"'{t}'")} DC({string.Join(", ", from t in tc.RequiredConfigurationKeys select $"{t.valueType.Name} '{t.key}'")})"
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
                    orderby tc.AssemblyFilePath, tc.FullyQualifiedName, tc.DisplayName
                    select $"{tc.Id:N} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} GS={SetupMethodString(tc.Group?.SetupMethods)} GC={CleanMethodString(tc.Group?.CleanupMethods)} FQN={tc.FullyQualifiedName}"
                ) + '\n'
            );
        }
        #endregion
    }
}
