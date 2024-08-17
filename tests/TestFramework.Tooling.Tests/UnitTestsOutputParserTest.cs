// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tools;
using TestFramework.Tooling.Tests.Helpers;

using TestResult = nanoFramework.TestFramework.Tooling.TestResult;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Test execution")]
    [TestCategory("Unit test launcher")]
    public sealed class UnitTestsOutputParserTest : TestUsingTestFrameworkToolingTestsDiscovery_v3
    {
        private static string AsString(UnitTestLauncher.Communication value, bool communicateByNames)
        {
            return communicateByNames ? value.ToString() : ((int)value).ToString();
        }

        /// <summary>
        /// Single test method executed as if it were a member of a static class.
        /// Other tests are not run. Output offered in one chunk.
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ParseOutput_SingleTestMethod_Pass_StaticClass(bool communicateByNames)
        {
            #region Create parser
            var actualTestResults = new List<TestResult>();
            var actual = new UnitTestsOutputParser(
                TestSelection,
                null,
                ReportPrefix,
                (result) => actualTestResults.AddRange(result)
            );
            #endregion

            #region Send output
            actual.AddOutput(
    $@"
Some information about the assemblies

{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
Some output from the test
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}:50000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}
");
            actual.Flush();
            #endregion

            #region Assert
            actualTestResults.AssertResults(TestSelection,
@"----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
DisplayName : 'TestMethod - Test has not been run'
Duration    : 0 ticks
Outcome     : None
ErrorMessage: 'Test has not been run'
Messages    :
Test has not been run.
    
*** Deployment ***
    
Some information about the assemblies
    
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#0
DisplayName : 'TestMethod1(1,1) - Test has not been run'
Duration    : 0 ticks
Outcome     : None
ErrorMessage: 'Test has not been run'
Messages    :
Test has not been run.
    
*** Deployment ***
    
Some information about the assemblies
    
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#1
DisplayName : 'TestMethod1(2,2) - Test has not been run'
Duration    : 0 ticks
Outcome     : None
ErrorMessage: 'Test has not been run'
Messages    :
Test has not been run.
    
*** Deployment ***
    
Some information about the assemblies
    
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
DisplayName : 'Test - Passed'
Duration    : 50000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Some output from the test
Test passed after 5 ms
    
*** Deployment ***
    
Some information about the assemblies
    
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
DisplayName : 'Test2 - Test has not been run'
Duration    : 0 ticks
Outcome     : None
ErrorMessage: 'Test has not been run'
Messages    :
Test has not been run.
    
*** Deployment ***
    
Some information about the assemblies
    
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
DisplayName : 'TestOnDeviceWithSomeFile - Test has not been run'
Duration    : 0 ticks
Outcome     : None
ErrorMessage: 'Test has not been run'
Messages    :
Test has not been run.
    
*** Deployment ***
    
Some information about the assemblies
    
----------------------------------------");
            #endregion
        }

        /// <summary>
        /// All test methods executed, output offered in one chunk.
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ParseOutput_AllTestMethods_NonStaticClasses_Pass_SingleOutput(bool communicateByNames)
        {
            #region Output and expectations
            string output = $@"
Some information about the assemblies

{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
Message from the constructor
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:10000:{AsString(UnitTestLauncher.Communication.Setup, communicateByNames)}
Message from the Setup method
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:30000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_TestMethodName}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
Output from the test method

More output from the test method

{ReportPrefix}:M:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_TestMethodName}:50000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#0:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
Test with data from the first data row attribute
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#0:10000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#1:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
Test with data from the second data row attribute
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#1:20000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Cleanup, communicateByNames)}
Message from the cleanup method
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Dispose, communicateByNames)}
Message from the Dispose method
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:40000:{AsString(UnitTestLauncher.Communication.CleanUpComplete, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}

{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:10000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}:50000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method2Name}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method2Name}:70000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Dispose, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.CleanUpComplete, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}

{ReportPrefix}:C:{TestWithFrameworkExtensions_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:10000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:50000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Dispose, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:10000:{AsString(UnitTestLauncher.Communication.CleanUpComplete, communicateByNames)}
{ReportPrefix}:C:{TestWithFrameworkExtensions_FQN}:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}
";
            string expectedTestResults = @"----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
DisplayName : 'TestMethod - Passed'
Duration    : 50000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Output from the test method

More output from the test method

Test passed after 5 ms

*** Setup ***
Message from the constructor
Message from the Setup method
Setup completed after 3 ms

*** Cleanup ***
Message from the cleanup method
Message from the Dispose method
Cleanup completed after 4 ms

*** Deployment ***

Some information about the assemblies

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#0
DisplayName : 'TestMethod1(1,1) - Passed'
Duration    : 10000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Test with data from the first data row attribute
Test passed after 1 ms

*** Setup ***
Message from the constructor
Message from the Setup method
Setup completed after 3 ms

*** Cleanup ***
Message from the cleanup method
Message from the Dispose method
Cleanup completed after 4 ms

*** Deployment ***

Some information about the assemblies

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#1
DisplayName : 'TestMethod1(2,2) - Passed'
Duration    : 20000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Test with data from the second data row attribute
Test passed after 2 ms

*** Setup ***
Message from the constructor
Message from the Setup method
Setup completed after 3 ms

*** Cleanup ***
Message from the cleanup method
Message from the Dispose method
Cleanup completed after 4 ms

*** Deployment ***

Some information about the assemblies

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
DisplayName : 'Test - Passed'
Duration    : 50000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Test passed after 5 ms

*** Setup ***
Setup completed after 1 ms

*** Cleanup ***
Cleanup completed after < 1 ms

*** Deployment ***

Some information about the assemblies

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
DisplayName : 'Test2 - Passed'
Duration    : 70000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Test passed after 7 ms

*** Setup ***
Setup completed after 1 ms

*** Cleanup ***
Cleanup completed after < 1 ms

*** Deployment ***

Some information about the assemblies

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
DisplayName : 'TestOnDeviceWithSomeFile - Passed'
Duration    : 50000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Setup completed after 1 ms
Test passed after 5 ms
Cleanup completed after 1 ms

*** Deployment ***

Some information about the assemblies

----------------------------------------";
            #endregion

            #region Create parser
            var actualTestResults = new List<TestResult>();
            var actual = new UnitTestsOutputParser(
                TestSelection,
                null,
                ReportPrefix,
                (result) => actualTestResults.AddRange(result)
            );
            #endregion

            #region Send output
            actual.AddOutput(output);
            actual.Flush();
            #endregion

            #region Assert
            actualTestResults.AssertResults(TestSelection, expectedTestResults);
            #endregion
        }

        /// <summary>
        /// Same test as <see cref="AllTestMethods_NonStaticClasses_Pass_SingleOutput"/>,
        /// but the output is offered in small chunks.
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ParseOutput_AllTestMethods_NonStaticClasses_Pass_OutputInParts(bool communicateByNames)
        {
            #region Output and expectations
            string output = $@"
Some information about the assemblies

{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
Message from the constructor
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:10000:{AsString(UnitTestLauncher.Communication.Setup, communicateByNames)}
Message from the Setup method
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:30000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_TestMethodName}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
Output from the test method

More output from the test method

{ReportPrefix}:M:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_TestMethodName}:50000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#0:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
Test with data from the first data row attribute
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#0:10000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#1:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
Test with data from the second data row attribute
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#1:20000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Cleanup, communicateByNames)}
Message from the cleanup method
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Dispose, communicateByNames)}
Message from the Dispose method
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:40000:{AsString(UnitTestLauncher.Communication.CleanUpComplete, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}

{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:10000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}:50000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method2Name}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method2Name}:70000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Dispose, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.CleanUpComplete, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}

{ReportPrefix}:C:{TestWithFrameworkExtensions_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:10000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:50000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Dispose, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:10000:{AsString(UnitTestLauncher.Communication.CleanUpComplete, communicateByNames)}
{ReportPrefix}:C:{TestWithFrameworkExtensions_FQN}:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}
";
            string expectedTestResults = @"----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
DisplayName : 'TestMethod - Passed'
Duration    : 50000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Output from the test method

More output from the test method

Test passed after 5 ms

*** Setup ***
Message from the constructor
Message from the Setup method
Setup completed after 3 ms

*** Cleanup ***
Message from the cleanup method
Message from the Dispose method
Cleanup completed after 4 ms

*** Deployment ***

Some information about the assemblies

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#0
DisplayName : 'TestMethod1(1,1) - Passed'
Duration    : 10000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Test with data from the first data row attribute
Test passed after 1 ms

*** Setup ***
Message from the constructor
Message from the Setup method
Setup completed after 3 ms

*** Cleanup ***
Message from the cleanup method
Message from the Dispose method
Cleanup completed after 4 ms

*** Deployment ***

Some information about the assemblies

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#1
DisplayName : 'TestMethod1(2,2) - Passed'
Duration    : 20000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Test with data from the second data row attribute
Test passed after 2 ms

*** Setup ***
Message from the constructor
Message from the Setup method
Setup completed after 3 ms

*** Cleanup ***
Message from the cleanup method
Message from the Dispose method
Cleanup completed after 4 ms

*** Deployment ***

Some information about the assemblies

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
DisplayName : 'Test - Passed'
Duration    : 50000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Test passed after 5 ms

*** Setup ***
Setup completed after 1 ms

*** Cleanup ***
Cleanup completed after < 1 ms

*** Deployment ***

Some information about the assemblies

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
DisplayName : 'Test2 - Passed'
Duration    : 70000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Test passed after 7 ms

*** Setup ***
Setup completed after 1 ms

*** Cleanup ***
Cleanup completed after < 1 ms

*** Deployment ***

Some information about the assemblies

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
DisplayName : 'TestOnDeviceWithSomeFile - Passed'
Duration    : 50000 ticks
Outcome     : Passed
ErrorMessage: ''
Messages    :
Setup completed after 1 ms
Test passed after 5 ms
Cleanup completed after 1 ms

*** Deployment ***

Some information about the assemblies

----------------------------------------";
            #endregion

            #region Create parser
            var actualTestResults = new List<TestResult>();
            var actual = new UnitTestsOutputParser(
                TestSelection,
                null,
                ReportPrefix,
                (result) => actualTestResults.AddRange(result)
            );
            #endregion

            #region Send output
            string outputNoLastEOL = output.TrimEnd();
            int blockSize = ReportPrefix.Length + 5;
            for (int i = 0; i < outputNoLastEOL.Length; i += blockSize)
            {
                string block = outputNoLastEOL.Substring(i, i + blockSize < outputNoLastEOL.Length ? blockSize : outputNoLastEOL.Length - i);
                actual.AddOutput(block);
            }
            actual.Flush();
            #endregion

            #region Assert
            actualTestResults.AssertResults(TestSelection, expectedTestResults);
            #endregion
        }

        /// <summary>
        /// Single test method executed as if it were a member of a static class.
        /// Other tests are not run. Output offered in one chunk.
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ParseOutput_SetupCleanupFails_StaticClass(bool communicateByNames)
        {
            #region Create parser
            var actualTestResults = new List<TestResult>();
            var actual = new UnitTestsOutputParser(
                TestSelection,
                null,
                ReportPrefix,
                (result) => actualTestResults.AddRange(result)
            );
            #endregion

            #region Send output
            actual.AddOutput(
$@"{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:10000:{AsString(UnitTestLauncher.Communication.Setup, communicateByNames)}
Exception in setup!
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:30000:{AsString(UnitTestLauncher.Communication.SetupFail, communicateByNames)}:AssertException

No man's land

{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:10000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}:50000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method2Name}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method2Name}:70000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Cleanup, communicateByNames)}
Exception in cleanup!
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:100000:{AsString(UnitTestLauncher.Communication.CleanupFail, communicateByNames)}:Exception

{ReportPrefix}:C:{TestWithFrameworkExtensions_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Setup, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:10000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:50000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Cleanup, communicateByNames)}
Exception in cleanup!
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:10000:{AsString(UnitTestLauncher.Communication.CleanupFail, communicateByNames)}:Exception
{ReportPrefix}:C:{TestWithFrameworkExtensions_FQN}:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}
");
            actual.Flush();
            #endregion

            #region Assert
            actualTestResults.AssertResults(TestSelection,
@"----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
DisplayName : 'TestMethod - Setup failed'
Duration    : 0 ticks
Outcome     : Failed
ErrorMessage: 'Setup failed'
Messages    :
Test has not been run.

*** Setup ***
Exception in setup!
Execution of setup method failed after 3 ms: AssertException
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#0
DisplayName : 'TestMethod1(1,1) - Setup failed'
Duration    : 0 ticks
Outcome     : Failed
ErrorMessage: 'Setup failed'
Messages    :
Test has not been run.

*** Setup ***
Exception in setup!
Execution of setup method failed after 3 ms: AssertException
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#1
DisplayName : 'TestMethod1(2,2) - Setup failed'
Duration    : 0 ticks
Outcome     : Failed
ErrorMessage: 'Setup failed'
Messages    :
Test has not been run.

*** Setup ***
Exception in setup!
Execution of setup method failed after 3 ms: AssertException
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
DisplayName : 'Test - Cleanup failed'
Duration    : 50000 ticks
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
Messages    :
Test passed after 5 ms

*** Setup ***
Setup completed after 1 ms

*** Cleanup ***
Exception in cleanup!
Execution of cleanup method failed after 10 ms: Exception
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
DisplayName : 'Test2 - Cleanup failed'
Duration    : 70000 ticks
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
Messages    :
Test passed after 7 ms

*** Setup ***
Setup completed after 1 ms

*** Cleanup ***
Exception in cleanup!
Execution of cleanup method failed after 10 ms: Exception
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
DisplayName : 'TestOnDeviceWithSomeFile - Cleanup failed'
Duration    : 10000 ticks
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
Messages    :
Setup completed after 1 ms
Test passed after 5 ms
Exception in cleanup!
Cleanup for the test failed after 1 ms: Exception
----------------------------------------");
            #endregion
        }

        /// <summary>
        /// Single test method executed as if it were a member of a static class.
        /// Other tests are not run. Output offered in one chunk.
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ParseOutput_SetupCleanupFails_NonStaticClass(bool communicateByNames)
        {
            #region Create parser
            var actualTestResults = new List<TestResult>();
            var actual = new UnitTestsOutputParser(
                TestSelection,
                null,
                ReportPrefix,
                (result) => actualTestResults.AddRange(result)
            );
            #endregion

            #region Send output
            actual.AddOutput(
$@"{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
Exception in constructor!
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:10000:{AsString(UnitTestLauncher.Communication.SetupFail, communicateByNames)}:AssertException

{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:10000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}:50000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method2Name}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method2Name}:70000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Dispose, communicateByNames)}
Exception in Dispose!
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.CleanupFail, communicateByNames)}:Exception

{ReportPrefix}:C:{TestWithFrameworkExtensions_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
Exception in constructor!
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:10000:{AsString(UnitTestLauncher.Communication.SetupFail, communicateByNames)}:Exception
{ReportPrefix}:C:{TestWithFrameworkExtensions_FQN}:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}
");
            actual.Flush();
            #endregion

            #region Assert
            actualTestResults.AssertResults(TestSelection,
@"----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
DisplayName : 'TestMethod - Setup failed'
Duration    : 0 ticks
Outcome     : Failed
ErrorMessage: 'Setup failed'
Messages    :
Test has not been run.

*** Setup ***
Exception in constructor!
Constructor of test class failed after 1 ms: AssertException
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#0
DisplayName : 'TestMethod1(1,1) - Setup failed'
Duration    : 0 ticks
Outcome     : Failed
ErrorMessage: 'Setup failed'
Messages    :
Test has not been run.

*** Setup ***
Exception in constructor!
Constructor of test class failed after 1 ms: AssertException
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#1
DisplayName : 'TestMethod1(2,2) - Setup failed'
Duration    : 0 ticks
Outcome     : Failed
ErrorMessage: 'Setup failed'
Messages    :
Test has not been run.

*** Setup ***
Exception in constructor!
Constructor of test class failed after 1 ms: AssertException
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
DisplayName : 'Test - Cleanup failed'
Duration    : 50000 ticks
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
Messages    :
Test passed after 5 ms

*** Setup ***
Setup completed after 1 ms

*** Cleanup ***
Exception in Dispose!
IDisposable.Dispose of test class failed after < 1 ms: Exception
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
DisplayName : 'Test2 - Cleanup failed'
Duration    : 70000 ticks
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
Messages    :
Test passed after 7 ms

*** Setup ***
Setup completed after 1 ms

*** Cleanup ***
Exception in Dispose!
IDisposable.Dispose of test class failed after < 1 ms: Exception
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
DisplayName : 'TestOnDeviceWithSomeFile - Setup failed'
Duration    : 10000 ticks
Outcome     : Failed
ErrorMessage: 'Setup failed'
Messages    :
Exception in constructor!
Setup for the test failed after 1 ms: Exception
----------------------------------------");
            #endregion
        }

        /// <summary>
        /// Single test method executed as if it were a member of a static class.
        /// Other tests are not run. Output offered in one chunk.
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ParseOutput_TestsFail(bool communicateByNames)
        {
            #region Create parser
            var actualTestResults = new List<TestResult>();
            var actual = new UnitTestsOutputParser(
                TestSelection,
                null,
                ReportPrefix,
                (result) => actualTestResults.AddRange(result)
            );
            #endregion

            #region Send output
            actual.AddOutput(
$@"
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_TestMethodName}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
Cannot create the test context for the test (within the test method)
{ReportPrefix}:M:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_TestMethodName}:50000:{AsString(UnitTestLauncher.Communication.SetupFail, communicateByNames)}:AssertFail
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#0:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
This test should be skipped
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#0:10000:{AsString(UnitTestLauncher.Communication.Skipped, communicateByNames)}:No need to run this test
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#1:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
Exception!
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#1:20000:{AsString(UnitTestLauncher.Communication.Fail, communicateByNames)}:Exception
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Dispose, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}

{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:10000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
Test proper passed, but teardown of context within test method failed
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}:50000:{AsString(UnitTestLauncher.Communication.CleanupFail, communicateByNames)}:Exception
{ReportPrefix}:M:{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method2Name}:0:{AsString(UnitTestLauncher.Communication.MethodError, communicateByNames)}:Test method not found
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Dispose, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}

{ReportPrefix}:C:{TestWithFrameworkExtensions_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Setup, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:10000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
Exception!
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:50000:{AsString(UnitTestLauncher.Communication.Fail, communicateByNames)}:Exception
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:0:{AsString(UnitTestLauncher.Communication.Cleanup, communicateByNames)}
{ReportPrefix}:M:{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}:10000:{AsString(UnitTestLauncher.Communication.CleanUpComplete, communicateByNames)}:Exception
{ReportPrefix}:C:{TestWithFrameworkExtensions_FQN}:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}
");
            actual.Flush();
            #endregion

            #region Assert
            actualTestResults.AssertResults(TestSelection,
@"----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
DisplayName : 'TestMethod - Setup failed'
Duration    : 50000 ticks
Outcome     : Failed
ErrorMessage: 'Setup failed'
Messages    :
Cannot create the test context for the test (within the test method)
Setup for the test failed after 5 ms: AssertFail

*** Setup ***
Setup completed after < 1 ms

*** Deployment ***

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#0
DisplayName : 'TestMethod1(1,1) - Test skipped'
Duration    : 10000 ticks
Outcome     : Skipped
ErrorMessage: 'Test skipped'
Messages    :
This test should be skipped
Execution of the test is aborted after 1 ms: No need to run this test

*** Setup ***
Setup completed after < 1 ms

*** Deployment ***

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#1
DisplayName : 'TestMethod1(2,2) - Test failed'
Duration    : 20000 ticks
Outcome     : Failed
ErrorMessage: 'Test failed'
Messages    :
Exception!
Test failed after 2 ms: Exception

*** Setup ***
Setup completed after < 1 ms

*** Deployment ***

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
DisplayName : 'Test - Cleanup failed'
Duration    : 50000 ticks
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
Messages    :
Test proper passed, but teardown of context within test method failed
Cleanup for the test failed after 5 ms: Exception

*** Setup ***
Setup completed after 1 ms

*** Deployment ***

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
DisplayName : 'Test2 - Method not found'
Duration    : 0 ticks
Outcome     : Failed
ErrorMessage: 'Method not found'
Messages    :
Test method not found

*** Setup ***
Setup completed after 1 ms

*** Deployment ***

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
DisplayName : 'TestOnDeviceWithSomeFile - Test failed'
Duration    : 50000 ticks
Outcome     : Failed
ErrorMessage: 'Test failed'
Messages    :
Setup completed after 1 ms
Exception!
Test failed after 5 ms: Exception
Cleanup completed after 1 ms

*** Deployment ***

----------------------------------------");
            #endregion
        }

        /// <summary>
        /// Single test method executed as if it were a member of a static class.
        /// Other tests are not run. Output offered in one chunk.
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ParseOutput_ClassOrMethodNotFound(bool communicateByNames)
        {
            #region Create parser
            var actualTestResults = new List<TestResult>();
            var actual = new UnitTestsOutputParser(
                TestSelection,
                null,
                ReportPrefix,
                (result) => actualTestResults.AddRange(result)
            );
            #endregion

            #region Send output
            actual.AddOutput(
$@"
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:30000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_TestMethodName}:0:{AsString(UnitTestLauncher.Communication.MethodError, communicateByNames)}:Test method not found
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#0:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#0:1000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:M:{TestClassWithSetupCleanup_FQN}.NoSuchMethod:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:{TestClassWithSetupCleanup_FQN}.NoSuchMethod:0:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#1:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:D:{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}#1:20000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:C:{TestClassWithSetupCleanup_FQN}:0:{AsString(UnitTestLauncher.Communication.MethodError, communicateByNames)}:Cleanup method not found
{ReportPrefix}:C:NoSuchClass:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:NoSuchClass:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
{ReportPrefix}:C:NoSuchClass:10000:{AsString(UnitTestLauncher.Communication.SetupComplete, communicateByNames)}
{ReportPrefix}:M:NoSuchClass.{TestClassTwoMethods_Method1Name}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:NoSuchClass.{TestClassTwoMethods_Method1Name}:1000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:M:NoSuchClass.{TestClassTwoMethods_Method1Name}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:M:NoSuchClass.{TestClassTwoMethods_Method1Name}:1000:{AsString(UnitTestLauncher.Communication.Pass, communicateByNames)}
{ReportPrefix}:C:NoSuchClass:0:{AsString(UnitTestLauncher.Communication.Dispose, communicateByNames)}
{ReportPrefix}:C:NoSuchClass:0:{AsString(UnitTestLauncher.Communication.Done, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Start, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.Instantiate, communicateByNames)}
{ReportPrefix}:C:{TestClassTwoMethods_FQN}:0:{AsString(UnitTestLauncher.Communication.MethodError, communicateByNames)}:Setup method not found
");
            actual.Flush();
            #endregion

            #region Assert
            actualTestResults.AssertResults(TestSelection,
@"----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
DisplayName : 'TestMethod - Method not found'
Duration    : 0 ticks
Outcome     : Failed
ErrorMessage: 'Method not found'
Messages    :
Test method not found

*** Setup ***
Setup completed after 3 ms

*** Cleanup ***
Cleanup method not found

*** Deployment ***

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#0
DisplayName : 'TestMethod1(1,1) - Cleanup failed'
Duration    : 1000 ticks
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
Messages    :
Test passed after < 1 ms

*** Setup ***
Setup completed after 3 ms

*** Cleanup ***
Cleanup method not found

*** Deployment ***

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#1
DisplayName : 'TestMethod1(2,2) - Cleanup failed'
Duration    : 20000 ticks
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
Messages    :
Test passed after 2 ms

*** Setup ***
Setup completed after 3 ms

*** Cleanup ***
Cleanup method not found

*** Deployment ***

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
DisplayName : 'Test - Setup failed'
Duration    : 0 ticks
Outcome     : Failed
ErrorMessage: 'Setup failed'
Messages    :
Test has not been run.

*** Setup ***
Setup method not found

*** Deployment ***

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
DisplayName : 'Test2 - Setup failed'
Duration    : 0 ticks
Outcome     : Failed
ErrorMessage: 'Setup failed'
Messages    :
Test has not been run.

*** Setup ***
Setup method not found

*** Deployment ***

----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
DisplayName : 'TestOnDeviceWithSomeFile - Test has not been run'
Duration    : 0 ticks
Outcome     : None
ErrorMessage: 'Test has not been run'
Messages    :
Test has not been run.

*** Deployment ***

----------------------------------------");
            #endregion
        }
    }
}
