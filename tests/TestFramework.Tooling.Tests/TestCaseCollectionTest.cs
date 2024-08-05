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
        #region TestFramework.Tooling.Tests.NFUnitTest
        [TestMethod]
        [TestCategory("Test cases")]
        public void TestCases_NFUnitTest_VirtualDevice()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest");
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
$@"#1 RII=True RH=False VD=True G=1 OATO=True
#2 RII=True RH=False VD=True G=1 OATO=True
#3 RII=True RH=False VD=True G=1 OATO=True
#4 RII=True RH=False VD=True G=2 OATO=True
#5 RII=True RH=False VD=True G=2 OATO=True
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} RII={tc.RunInIsolation} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} G={tc.Group?.TestGroupIndex} OATO={tc.Group?.RunOneAfterTheOther}"
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
        public void TestCases_NFUnitTest_VirtualDevice_RealHardware()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest");
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
$@"#1 RII=True RH=False VD=True G=1 OATO=True
#1 RII=True RH=True VD=False G=1 OATO=True
#2 RII=True RH=False VD=True G=1 OATO=True
#2 RII=True RH=True VD=False G=1 OATO=True
#3 RII=True RH=False VD=True G=1 OATO=True
#3 RII=True RH=True VD=False G=1 OATO=True
#4 RII=True RH=False VD=True G=2 OATO=True
#4 RII=True RH=True VD=False G=2 OATO=True
#5 RII=True RH=False VD=True G=2 OATO=True
#5 RII=True RH=True VD=False G=2 OATO=True
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} RII={tc.RunInIsolation} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} G={tc.Group?.TestGroupIndex} OATO={tc.Group?.RunOneAfterTheOther}"
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

        #region TestFramework.Tooling.Tests.NFUnitTest.New
        [TestMethod]
        [TestCategory("Test cases")]
        public void TestCases_NFUnitTest_New()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest.New");
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
#1 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod 'TestMethod [test]'
#2 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [Virtual Device]'
#2 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(1,1) [test]'
#3 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [Virtual Device]'
#3 TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 'TestMethod1(2,2) [test]'
#4 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunOneByOne.Method 'Method [Virtual Device]'
#4 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunOneByOne.Method 'Method [test]'
#5 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunInParallel.Method 'Method [Virtual Device]'
#5 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunInParallel.Method 'Method [test]'
#6 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne.Method1 'Method1 [Virtual Device]'
#6 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne.Method1 'Method1 [test]'
#7 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne.Method2 'Method2 [Virtual Device]'
#7 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne.Method2 'Method2 [test]'
#8 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne.Method1 'Method1 [Virtual Device]'
#8 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne.Method1 'Method1 [test]'
#9 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne.Method2 'Method2 [Virtual Device]'
#9 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne.Method2 'Method2 [test]'
#10 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method1 'Method1 [Virtual Device]'
#10 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method1 'Method1 [test]'
#11 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2 'Method2 [Virtual Device]'
#11 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2 'Method2 [test]'
#12 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelOverruled.RunInIsolationBecauseOfAssemblyAttribute 'RunInIsolationBecauseOfAssemblyAttribute [Virtual Device]'
#12 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelOverruled.RunInIsolationBecauseOfAssemblyAttribute 'RunInIsolationBecauseOfAssemblyAttribute [test]'
#13 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelOverruled.RunInParallelBecauseOfMethodAttribute 'RunInParallelBecauseOfMethodAttribute [Virtual Device]'
#13 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelOverruled.RunInParallelBecauseOfMethodAttribute 'RunInParallelBecauseOfMethodAttribute [test]'
#14 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallel.RunInIsolationBecauseOfMethodAttribute 'RunInIsolationBecauseOfMethodAttribute [Virtual Device]'
#14 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallel.RunInIsolationBecauseOfMethodAttribute 'RunInIsolationBecauseOfMethodAttribute [test]'
#15 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallel.RunInParallelBecauseOfClassAttribute 'RunInParallelBecauseOfClassAttribute [Virtual Device]'
#15 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallel.RunInParallelBecauseOfClassAttribute 'RunInParallelBecauseOfClassAttribute [test]'
#16 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelButNotItsMethods.RunParallelWithOthersOneByOneInClass1 'RunParallelWithOthersOneByOneInClass1 [Virtual Device]'
#16 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelButNotItsMethods.RunParallelWithOthersOneByOneInClass1 'RunParallelWithOthersOneByOneInClass1 [test]'
#17 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelButNotItsMethods.RunParallelWithOthersOneByOneInClass2 'RunParallelWithOthersOneByOneInClass2 [Virtual Device]'
#17 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelButNotItsMethods.RunParallelWithOthersOneByOneInClass2 'RunParallelWithOthersOneByOneInClass2 [test]'
#18 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray 'TestThatIsNowInDisarray [Virtual Device]'
#18 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray 'TestThatIsNowInDisarray [test]'
#19 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestDoublePrecisionCalculation 'TestDoublePrecisionCalculation [Virtual Device]'
#19 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestDoublePrecisionCalculation 'TestDoublePrecisionCalculation [test]'
#19 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestDoublePrecisionCalculation 'TestDoublePrecisionCalculation [DoublePrecisionDevice]'
#20 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [Virtual Device]'
#20 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test [test]'
#21 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [Virtual Device]'
#21 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2 [test]'
#22 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits 'MethodWithTraits [Virtual Device]'
#22 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits 'MethodWithTraits [test]'
#23 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods [Virtual Device]'
#23 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods [test]'
#23 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods [esp32]'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} {tc.FullyQualifiedName} '{tc.DisplayName}'"
                ) + '\n'
            );
            Assert.AreEqual(23, actual.TestMethodsInAssembly(assemblyFilePath));

            // Assert source location and traits
            Assert.AreEqual(
$@"#1 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device'
#1 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@test'
#2 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device'
#2 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@test'
#3 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device'
#3 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@test'
#4 @{pathPrefix}TestClassVariants.cs(13,28) 'TestClass demonstration', '@Virtual Device'
#4 @{pathPrefix}TestClassVariants.cs(13,28) 'TestClass demonstration', '@test'
#5 @{pathPrefix}TestClassVariants.cs(25,28) 'TestClass demonstration', '@Virtual Device'
#5 @{pathPrefix}TestClassVariants.cs(25,28) 'TestClass demonstration', '@test'
#6 @{pathPrefix}TestClassVariants.cs(43,28) 'TestClass demonstration', '@Virtual Device'
#6 @{pathPrefix}TestClassVariants.cs(43,28) 'TestClass demonstration', '@test'
#7 @{pathPrefix}TestClassVariants.cs(50,28) 'TestClass demonstration', '@Virtual Device'
#7 @{pathPrefix}TestClassVariants.cs(50,28) 'TestClass demonstration', '@test'
#8 @{pathPrefix}TestClassVariants.cs(66,28) 'TestClass demonstration', '@Virtual Device'
#8 @{pathPrefix}TestClassVariants.cs(66,28) 'TestClass demonstration', '@test'
#9 @{pathPrefix}TestClassVariants.cs(73,28) 'TestClass demonstration', '@Virtual Device'
#9 @{pathPrefix}TestClassVariants.cs(73,28) 'TestClass demonstration', '@test'
#10 @{pathPrefix}TestClassVariants.cs(85,28) 'TestClass demonstration', '@Virtual Device'
#10 @{pathPrefix}TestClassVariants.cs(85,28) 'TestClass demonstration', '@test'
#11 @{pathPrefix}TestClassVariants.cs(92,28) 'TestClass demonstration', '@Virtual Device'
#11 @{pathPrefix}TestClassVariants.cs(92,28) 'TestClass demonstration', '@test'
#12 @{pathPrefix}TestRunInParallel.cs(12,21) '@Virtual Device'
#12 @{pathPrefix}TestRunInParallel.cs(12,21) '@test'
#13 @{pathPrefix}TestRunInParallel.cs(17,21) '@Virtual Device'
#13 @{pathPrefix}TestRunInParallel.cs(17,21) '@test'
#14 @{pathPrefix}TestRunInParallel.cs(27,21) '@Virtual Device'
#14 @{pathPrefix}TestRunInParallel.cs(27,21) '@test'
#15 @{pathPrefix}TestRunInParallel.cs(32,21) '@Virtual Device'
#15 @{pathPrefix}TestRunInParallel.cs(32,21) '@test'
#16 @{pathPrefix}TestRunInParallel.cs(42,21) '@Virtual Device'
#16 @{pathPrefix}TestRunInParallel.cs(42,21) '@test'
#17 @{pathPrefix}TestRunInParallel.cs(47,21) '@Virtual Device'
#17 @{pathPrefix}TestRunInParallel.cs(47,21) '@test'
#18 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@Virtual Device'
#18 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@test'
#19 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@Virtual Device'
#19 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@test'
#19 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@DoublePrecisionDevice'
#20 @{pathPrefix}TestWithMethods.cs(13,21) '@Virtual Device'
#20 @{pathPrefix}TestWithMethods.cs(13,21) '@test'
#21 @{pathPrefix}TestWithMethods.cs(18,21) '@Virtual Device'
#21 @{pathPrefix}TestWithMethods.cs(18,21) '@test'
#22 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(14,21) 'Example trait', 'Other trait', '@Virtual Device'
#22 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(14,21) 'Example trait', 'Other trait', '@test'
#23 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(19,21) '@Virtual Device'
#23 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(19,21) '@test'
#23 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(19,21) '@esp32'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} @{tc.TestMethodSourceCodeLocation?.ForMessage()} {string.Join(", ", from t in tc.Traits select $"'{t}'")}"
                ) + '\n'
            );

            // Assert run information
            Assert.AreEqual(
$@"#1 RII=True RH=False VD=True G=1 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
#1 RII=True RH=True VD=False G=1 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
#2 RII=True RH=False VD=True G=1 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
#2 RII=True RH=True VD=False G=1 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
#3 RII=True RH=False VD=True G=1 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
#3 RII=True RH=True VD=False G=1 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
#4 RII=True RH=False VD=True G=2 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunOneByOne.Method
#4 RII=True RH=True VD=False G=2 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunOneByOne.Method
#5 RII=False RH=False VD=True G=3 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunInParallel.Method
#5 RII=False RH=True VD=False G=3 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunInParallel.Method
#6 RII=True RH=False VD=True G=4 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne.Method1
#6 RII=True RH=True VD=False G=4 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne.Method1
#7 RII=True RH=False VD=True G=4 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne.Method2
#7 RII=True RH=True VD=False G=4 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne.Method2
#8 RII=True RH=False VD=True G=5 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne.Method1
#8 RII=True RH=True VD=False G=5 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne.Method1
#9 RII=True RH=False VD=True G=5 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne.Method2
#9 RII=True RH=True VD=False G=5 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne.Method2
#10 RII=False RH=False VD=True G=6 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method1
#10 RII=False RH=True VD=False G=6 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method1
#11 RII=False RH=False VD=True G=6 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2
#11 RII=False RH=True VD=False G=6 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2
#12 RII=True RH=False VD=True G=7 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelOverruled.RunInIsolationBecauseOfAssemblyAttribute
#12 RII=True RH=True VD=False G=7 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelOverruled.RunInIsolationBecauseOfAssemblyAttribute
#13 RII=False RH=False VD=True G=7 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelOverruled.RunInParallelBecauseOfMethodAttribute
#13 RII=False RH=True VD=False G=7 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelOverruled.RunInParallelBecauseOfMethodAttribute
#14 RII=True RH=False VD=True G=8 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallel.RunInIsolationBecauseOfMethodAttribute
#14 RII=True RH=True VD=False G=8 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallel.RunInIsolationBecauseOfMethodAttribute
#15 RII=False RH=False VD=True G=8 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallel.RunInParallelBecauseOfClassAttribute
#15 RII=False RH=True VD=False G=8 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallel.RunInParallelBecauseOfClassAttribute
#16 RII=False RH=False VD=True G=9 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelButNotItsMethods.RunParallelWithOthersOneByOneInClass1
#16 RII=False RH=True VD=False G=9 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelButNotItsMethods.RunParallelWithOthersOneByOneInClass1
#17 RII=False RH=False VD=True G=9 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelButNotItsMethods.RunParallelWithOthersOneByOneInClass2
#17 RII=False RH=True VD=False G=9 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelButNotItsMethods.RunParallelWithOthersOneByOneInClass2
#18 RII=True RH=False VD=True G=11 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray
#18 RII=True RH=True VD=False G=11 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray
#19 RII=True RH=False VD=True G=11 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestDoublePrecisionCalculation
#19 RII=True RH=True VD=False G=11 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestDoublePrecisionCalculation
#19 RII=True RH=True VD=False G=11 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestDoublePrecisionCalculation
#20 RII=True RH=False VD=True G=12 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
#20 RII=True RH=True VD=False G=12 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
#21 RII=True RH=False VD=True G=12 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
#21 RII=True RH=True VD=False G=12 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
#22 RII=True RH=False VD=True G=13 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits
#22 RII=True RH=True VD=False G=13 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits
#23 RII=True RH=False VD=True G=13 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods
#23 RII=True RH=True VD=False G=13 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods
#23 RII=True RH=True VD=False G=13 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} RII={tc.RunInIsolation} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} G={tc.Group?.TestGroupIndex} OATO={tc.Group?.RunOneAfterTheOther} FQN={tc.FullyQualifiedName}"
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
        public void TestCases_NFUnitTest_New_NoRealHardware()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest.New");
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
#4 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunOneByOne.Method 'Method'
#5 TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunInParallel.Method 'Method'
#6 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne.Method1 'Method1'
#7 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne.Method2 'Method2'
#8 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne.Method1 'Method1'
#9 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne.Method2 'Method2'
#10 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method1 'Method1'
#11 TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2 'Method2'
#12 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelOverruled.RunInIsolationBecauseOfAssemblyAttribute 'RunInIsolationBecauseOfAssemblyAttribute'
#13 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelOverruled.RunInParallelBecauseOfMethodAttribute 'RunInParallelBecauseOfMethodAttribute'
#14 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallel.RunInIsolationBecauseOfMethodAttribute 'RunInIsolationBecauseOfMethodAttribute'
#15 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallel.RunInParallelBecauseOfClassAttribute 'RunInParallelBecauseOfClassAttribute'
#16 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelButNotItsMethods.RunParallelWithOthersOneByOneInClass1 'RunParallelWithOthersOneByOneInClass1'
#17 TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelButNotItsMethods.RunParallelWithOthersOneByOneInClass2 'RunParallelWithOthersOneByOneInClass2'
#18 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray 'TestThatIsNowInDisarray'
#19 TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestDoublePrecisionCalculation 'TestDoublePrecisionCalculation'
#20 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test 'Test'
#21 TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 'Test2'
#22 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits 'MethodWithTraits'
#23 TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods 'MethodWithNewTestMethods'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} {tc.FullyQualifiedName} '{tc.DisplayName}'"
                ) + '\n'
            );
            Assert.AreEqual(23, actual.TestMethodsInAssembly(assemblyFilePath));

            // Assert source location and traits
            Assert.AreEqual(
$@"#1 @{pathPrefix}TestAllCurrentAttributes.cs(13,21) '@Virtual Device'
#2 @{pathPrefix}TestAllCurrentAttributes.cs(17,10) '@Virtual Device'
#3 @{pathPrefix}TestAllCurrentAttributes.cs(18,10) '@Virtual Device'
#4 @{pathPrefix}TestClassVariants.cs(13,28) 'TestClass demonstration', '@Virtual Device'
#5 @{pathPrefix}TestClassVariants.cs(25,28) 'TestClass demonstration', '@Virtual Device'
#6 @{pathPrefix}TestClassVariants.cs(43,28) 'TestClass demonstration', '@Virtual Device'
#7 @{pathPrefix}TestClassVariants.cs(50,28) 'TestClass demonstration', '@Virtual Device'
#8 @{pathPrefix}TestClassVariants.cs(66,28) 'TestClass demonstration', '@Virtual Device'
#9 @{pathPrefix}TestClassVariants.cs(73,28) 'TestClass demonstration', '@Virtual Device'
#10 @{pathPrefix}TestClassVariants.cs(85,28) 'TestClass demonstration', '@Virtual Device'
#11 @{pathPrefix}TestClassVariants.cs(92,28) 'TestClass demonstration', '@Virtual Device'
#12 @{pathPrefix}TestRunInParallel.cs(12,21) '@Virtual Device'
#13 @{pathPrefix}TestRunInParallel.cs(17,21) '@Virtual Device'
#14 @{pathPrefix}TestRunInParallel.cs(27,21) '@Virtual Device'
#15 @{pathPrefix}TestRunInParallel.cs(32,21) '@Virtual Device'
#16 @{pathPrefix}TestRunInParallel.cs(42,21) '@Virtual Device'
#17 @{pathPrefix}TestRunInParallel.cs(47,21) '@Virtual Device'
#18 @{pathPrefix}TestWithFrameworkExtensions.cs(13,21) '@Virtual Device'
#19 @{pathPrefix}TestWithFrameworkExtensions.cs(19,21) '@Virtual Device'
#20 @{pathPrefix}TestWithMethods.cs(13,21) '@Virtual Device'
#21 @{pathPrefix}TestWithMethods.cs(18,21) '@Virtual Device'
#22 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(14,21) 'Example trait', 'Other trait', '@Virtual Device'
#23 @{pathPrefix}TestWithNewTestMethodsAttributes.cs(19,21) '@Virtual Device'
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} @{tc.TestMethodSourceCodeLocation?.ForMessage()} {string.Join(", ", from t in tc.Traits select $"'{t}'")}"
                ) + '\n'
            );

            // Assert run information
            Assert.AreEqual(
$@"#1 RII=True RH=False VD=True G=1 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
#2 RII=True RH=False VD=True G=1 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
#3 RII=True RH=False VD=True G=1 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1
#4 RII=True RH=False VD=True G=2 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunOneByOne.Method
#5 RII=False RH=False VD=True G=3 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunInParallel.Method
#6 RII=True RH=False VD=True G=4 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne.Method1
#7 RII=True RH=False VD=True G=4 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne.Method2
#8 RII=True RH=False VD=True G=5 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne.Method1
#9 RII=True RH=False VD=True G=5 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne.Method2
#10 RII=False RH=False VD=True G=6 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method1
#11 RII=False RH=False VD=True G=6 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2
#12 RII=True RH=False VD=True G=7 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelOverruled.RunInIsolationBecauseOfAssemblyAttribute
#13 RII=False RH=False VD=True G=7 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelOverruled.RunInParallelBecauseOfMethodAttribute
#14 RII=True RH=False VD=True G=8 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallel.RunInIsolationBecauseOfMethodAttribute
#15 RII=False RH=False VD=True G=8 OATO=False FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallel.RunInParallelBecauseOfClassAttribute
#16 RII=False RH=False VD=True G=9 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelButNotItsMethods.RunParallelWithOthersOneByOneInClass1
#17 RII=False RH=False VD=True G=9 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestRunInParallelButNotItsMethods.RunParallelWithOthersOneByOneInClass2
#18 RII=True RH=False VD=True G=11 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray
#19 RII=True RH=False VD=True G=11 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestDoublePrecisionCalculation
#20 RII=True RH=False VD=True G=12 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
#21 RII=True RH=False VD=True G=12 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
#22 RII=True RH=False VD=True G=13 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits
#23 RII=True RH=False VD=True G=13 OATO=True FQN=TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from tc in actual.TestCases
                    select $"#{tc.TestIndex} RII={tc.RunInIsolation} RH={tc.ShouldRunOnRealHardware} VD={tc.ShouldRunOnVirtualDevice} G={tc.Group?.TestGroupIndex} OATO={tc.Group?.RunOneAfterTheOther} FQN={tc.FullyQualifiedName}"
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
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string pathPrefix1 = Path.GetDirectoryName(projectFilePath1) + Path.DirectorySeparatorChar;
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest.New");
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
$@"Verbose: {pathPrefix2}TestWithALotOfErrors.cs(10,6): Only one attribute that implements 'ITestClass' is allowed. Only the first one is used, subsequent attributes are ignored.
Verbose: {pathPrefix2}TestWithALotOfErrors.cs(19,10): Only one method of a class can have attribute implements 'ISetup'. Subsequent attribute is ignored.
Verbose: {pathPrefix2}TestWithALotOfErrors.cs(31,10): Only one method of a class can have attribute that implements 'ICleanup'. Subsequent attribute is ignored.
Verbose: {pathPrefix2}TestWithALotOfErrors.cs(39,10): Only one method of a class can have attribute implements 'ISetup'. Subsequent attribute is ignored.
Verbose: {pathPrefix2}TestWithALotOfErrors.cs(38,10): Only one method of a class can have attribute that implements 'ICleanup'. Subsequent attribute is ignored.
Verbose: {pathPrefix2}TestWithALotOfErrors.cs(41,21): No other attributes are allowed when the attributes that implement 'ICleanup'/'ISetup' are present. Extra attributes are ignored.
Detailed: {pathPrefix1}TestAllCurrentAttributes.cs(13,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Detailed: {pathPrefix1}TestAllCurrentAttributes.cs(19,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Detailed: {pathPrefix1}TestWithMethods.cs(9,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Detailed: {pathPrefix1}TestWithMethods.cs(14,21): Method, class and assembly have no attributes to indicate on what device the test should be run.
Verbose: Project file for assembly '{assemblyFilePath3}' not found
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n'
            );

            // Assert that only the NFUnitTests assemblies are included
            Assert.AreEqual(
$@"{assemblyFilePath2}
{assemblyFilePath1}
".Replace("\r\n", "\n"),
                string.Join("\n",
                        actual.AssemblyFilePaths
                    ) + '\n'
            );

            // Test methods
            Assert.AreEqual(5, actual.TestMethodsInAssembly(assemblyFilePath1));
            Assert.AreEqual(23, actual.TestMethodsInAssembly(assemblyFilePath2));
            Assert.AreEqual(0, actual.TestMethodsInAssembly(assemblyFilePath3));
        }

        [TestMethod]
        [TestCategory("Test cases")]
        public void TestCases_Multiple_Assemblies_NoLogger()
        {
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string pathPrefix1 = Path.GetDirectoryName(projectFilePath1) + Path.DirectorySeparatorChar;
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest.New");
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
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string pathPrefix1 = Path.GetDirectoryName(projectFilePath1) + Path.DirectorySeparatorChar;
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest.New");
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
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest.New");
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
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.NFUnitTest.New");
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
                    (assemblyFilePath2, "Method2 [Virtual Device]", "TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2"),
                    (assemblyFilePath2, "Method2 [test]", "TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2"),
                    (assemblyFilePath1, "Method2 [test]", "TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2"),
                },
                (f) => ProjectSourceInventory.FindProjectFilePath(f, logger),
                true,
                logger);

            // Assert the selection-related messages
            Assert.AreEqual(
                expectedDiscoveryMessages +
$@"Verbose: Test case 'TestMethod1(2,2) [some_platform]' (TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1) from '{assemblyFilePath1}' is no longer available
Verbose: Test case 'NoSuchMethod' (TestFramework.Tooling.Tests.NFUnitTest.NoSuchClass.NoSuchMethod) from '{assemblyFilePath1}' is no longer available
Verbose: Test case 'Method2 [test]' (TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2) from '{assemblyFilePath1}' is no longer available
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n'
            );

            // Assert that the selected test case are present
            Assert.AreEqual(
@"#11 (TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2) Method2 [Virtual Device]
#11 (TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel.Method2) Method2 [test]
#2 (TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1) TestMethod1(1,1) [Real hardware]
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
