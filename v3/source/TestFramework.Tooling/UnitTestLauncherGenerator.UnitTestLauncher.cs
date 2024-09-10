// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

//======================================================================
//
// This file is generated. Changes to the code will be lost.
//
//======================================================================

namespace nanoFramework.TestFramework.Tools
{
    /// <summary>
    /// This class is a unit test launcher for nanoFramework
    /// </summary>
    public partial class UnitTestLauncher
    {
        #region Fields
        private readonly string _reportPrefix;
        private Type _testClass;
        private string _testClassPrefix;
        private object _testClassInstance;
        #endregion

        #region Entry point
        /// <summary>
        /// Run the selected unit tests from the test assembly
        /// </summary>
        /// <param name="reportPrefix">Prefix to use for messages from the unit test launcher
        /// about the execution of the unit tests</param>
        public static void Run(string reportPrefix)
        {
            Console.WriteLine();
            new UnitTestLauncher(reportPrefix).RunUnitTests();
            Console.WriteLine($"{reportPrefix}:{Communication.AllTestsDone}");
        }
        #endregion

        #region Construction
        private UnitTestLauncher(string reportPrefix)
        {
            _reportPrefix = reportPrefix;
        }
        #endregion

        #region Execution of selected unit tests
        /// <summary>
        /// Get test class information for a candidate test class.
        /// The <see cref="RunUnitTests"/> method will be generated based on the selection of tests.
        /// </summary>
        private partial void RunUnitTests();
        #endregion

        #region Execution of unit tests in a test class
        public delegate void RunSetupMethods(RunSetupMethod runSetupMethod);

        public delegate void RunSetupMethod(string setupMethodName, object[] configurationData);

        public delegate void RunCleanupMethods(RunCleanupMethod runCleanupMethod);

        public delegate void RunCleanupMethod(string cleanupMethodName);

        public delegate void RunTestMethods(RunTestMethod runTestMethod, RunDataRowTestMethod runDataRowTest);

        public delegate void RunTestMethod(string testMethodName, object[] configurationData);

        public delegate void RunDataRowTestMethod(string testMethodName, object[] configurationData, params int[] dataRowIndices);

        /// <summary>
        /// Run the unit tests in the assembly
        /// </summary>
        /// <param name="testClass">Type of the test class to execute tests of</param>
        /// <param name="instantiate">Instructs how to instantiate and setup/cleanup the class. Numerical value of a combination of <see cref="TestClassInitialisation"/> values.</param>
        /// <param name="runSetup">Method that will call all setup methods (except the constructor). Pass <c>null</c> if there are none.</param>
        /// <param name="runCleanup">Method that will call all cleanup methods (except <c>IDisposable.Dispose</c>). Pass <c>null</c> if there are none.</param>
        /// <param name="runMethods">Method that will call all (selected) test methods.</param>
        public void ForClass(Type testClass, int instantiate, RunSetupMethods runSetup, RunCleanupMethods runCleanup, RunTestMethods runMethods)
        {
            #region Verify the presence of setup/cleanup methods
            _testClass = testClass;
            _testClassPrefix = _reportPrefix + ":C:" + testClass.FullName;
            Console.WriteLine($"{_testClassPrefix}:0:{Communication.Start}");
            try
            {
                ConstructorInfo constructor = null;
                if ((instantiate & (int)TestClassInitialisation.InstantiateForAllMethods) != 0
                    || (instantiate & (int)TestClassInitialisation.InstantiatePerTestMethod) != 0)
                {
                    constructor = testClass.GetConstructor(s_defaultConstructor);
                    if (constructor is null)
                    {
                        Console.WriteLine($"{_testClassPrefix}:0:{Communication.MethodError}:Class does not have a public default constructor");
                        return;
                    }
                }
                #endregion

                if ((instantiate & (int)TestClassInitialisation.InstantiatePerTestMethod) != 0)
                {
                    #region Instantiate a test class, setup and cleanup per method
                    bool InitialiseInstance(string prefix)
                    {
                        _testClassInstance = null;
                        return InitialiseTestClass(prefix, constructor, runSetup);
                    }
                    void DisposeOfInstance(string prefix)
                    {
                        DisposeOfTestClass(prefix, runCleanup, true);
                    }

                    runMethods(
                        (testMethodName, configurationData)
                            => RunTestMethodTest(testClass, InitialiseInstance, DisposeOfInstance, testMethodName, configurationData),

                        (testMethodName, configurationData, dataRowIndices)
                            => RunDataRowTests(testClass, InitialiseInstance, DisposeOfInstance, testMethodName, configurationData, dataRowIndices)
                    );
                    #endregion
                }
                else if ((instantiate & (int)TestClassInitialisation.SetupCleanupPerTestMethod) != 0)
                {
                    #region Instantiate a test class once or not at all, setup and cleanup per method
                    _testClassInstance = null;
                    if (constructor is not null)
                    {
                        if (!InitialiseTestClass(_testClassPrefix, constructor, null))
                        {
                            return;
                        }
                    }

                    bool InitialiseInstance(string prefix)
                    {
                        return InitialiseTestClass(prefix, null, runSetup);
                    }
                    void DisposeOfInstance(string prefix)
                    {
                        DisposeOfTestClass(prefix, runCleanup, false);
                    }

                    runMethods(
                        (testMethodName, configurationData)
                            => RunTestMethodTest(testClass, InitialiseInstance, DisposeOfInstance, testMethodName, configurationData),

                        (testMethodName, configurationData, dataRowIndices)
                            => RunDataRowTests(testClass, InitialiseInstance, DisposeOfInstance, testMethodName, configurationData, dataRowIndices)
                    );

                    if (constructor is not null)
                    {
                        DisposeOfTestClass(_testClassPrefix, null, true);
                    }
                    #endregion
                }
                else
                {
                    #region Instantiate a test class, setup and cleanup per method
                    _testClassInstance = null;
                    if (!InitialiseTestClass(_testClassPrefix, constructor, runSetup))
                    {
                        return;
                    }

                    runMethods(
                        (testMethodName, configurationData)
                            => RunTestMethodTest(testClass, null, null, testMethodName, configurationData),

                        (testMethodName, configurationData, dataRowIndices)
                            => RunDataRowTests(testClass, null, null, testMethodName, configurationData, dataRowIndices)
                    );

                    DisposeOfTestClass(_testClassPrefix, runCleanup, true);
                    #endregion
                }
            }
            finally
            {
                Console.WriteLine($"{_testClassPrefix}:0:{Communication.Done}");
            }
        }
        private static readonly Type[] s_defaultConstructor = new Type[] { };

        private bool InitialiseTestClass(string prefix, ConstructorInfo constructor, RunSetupMethods runSetup)
        {
            long startTime = DateTime.UtcNow.Ticks;
            try
            {
                if (constructor is not null && _testClassInstance is null)
                {
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Instantiate}");
                    try
                    {
                        _testClassInstance = constructor.Invoke(null);
                    }
                    finally
                    {
                        Console.WriteLine();
                    }
                }

                bool success = true;
                if (runSetup is not null)
                {
                    runSetup((methodName, deploymentConfiguration) =>
                    {
                        if (success)
                        {
                            MethodInfo setupMethod = _testClass.GetMethod(methodName);
                            if (setupMethod is null)
                            {
                                success = false;
                                Console.WriteLine($"{_testClassPrefix}:0:{Communication.MethodError}:Method '{methodName}' not found");
                            }
                            else
                            {
                                Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Setup}:{methodName}");
                                try
                                {
                                    setupMethod.Invoke(_testClassInstance, deploymentConfiguration);
                                }
                                finally
                                {
                                    Console.WriteLine();
                                }
                            }
                        }
                    });
                }
                if (success)
                {
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.SetupComplete}");
                }
                return success;
            }
            catch (SkipTestException ex)
            {
                Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Skipped}:{ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.SetupFail}:{ex.Message}");
                return false;
            }
        }

        private delegate bool InstanceInitialiser(string prefix);
        private delegate void InstanceDisposer(string prefix);

        private void RunTestMethodTest(Type testClass, InstanceInitialiser setup, InstanceDisposer cleanup, string testMethodName, object[] configurationData)
        {
            string prefix = _reportPrefix + ":M:" + testClass.FullName + '.' + testMethodName;

            MethodInfo testMethod = testClass.GetMethod(testMethodName);

            if (testMethod is null)
            {
                Console.WriteLine($"{prefix}:0:{Communication.MethodError}:Test method '{testMethodName}' not found");
            }
            else
            {
                RunTest(
                    prefix,
                    setup, cleanup,
                    () => testMethod.Invoke(_testClassInstance, configurationData ?? s_noArguments)
                );
            }
        }
        private static readonly object[] s_noArguments = new object[] { };

        private void RunDataRowTests(Type testClass, InstanceInitialiser setup, InstanceDisposer cleanup, string testMethodName, object[] configurationData, int[] dataRowIndices)
        {
            string prefix = _reportPrefix + ":D:" + testClass.FullName + '.' + testMethodName + "#";
            MethodInfo testMethod = testClass.GetMethod(testMethodName);

            if (testMethod is null)
            {
                foreach (int dataRowIndex in dataRowIndices)
                {
                    Console.WriteLine($"{prefix}{dataRowIndex}:0:{Communication.MethodError}:Test method '{testMethodName}' not found");
                }
            }
            else
            {
                object[] attributes = testMethod.GetCustomAttributes(true);
                int dataRowIndex = -1;
                for (int i = 0; i < attributes.Length; i++)
                {
                    if (attributes[i] is IDataRow dataRow)
                    {
                        ++dataRowIndex;
                        foreach (int idx in dataRowIndices)
                        {
                            if (idx == dataRowIndex)
                            {
                                object[] parameters = dataRow.MethodParameters;
                                if (configurationData is not null && configurationData.Length > 0)
                                {
                                    object[] combined = new object[configurationData.Length + parameters.Length];
                                    configurationData.CopyTo(combined, 0);
                                    parameters.CopyTo(combined, configurationData.Length);
                                    parameters = combined;
                                }

                                RunTest(
                                    prefix + dataRowIndex.ToString(),
                                    setup, cleanup,
                                    () => testMethod.Invoke(_testClassInstance, parameters)
                                );
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void RunTest(string prefix, InstanceInitialiser setup, InstanceDisposer cleanup, Action testMethod)
        {
            long startTime = DateTime.UtcNow.Ticks;
            Console.WriteLine($"{prefix}:0:{Communication.Start}");
            if (setup is null || setup(prefix))
            {
                try
                {
                    try
                    {
                        testMethod();
                    }
                    finally
                    {
                        Console.WriteLine();
                    }
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Pass}");
                }
                catch (SkipTestException ex)
                {
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Skipped}:{ex.Message}");
                }
                catch (SetupFailedException ex)
                {
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.SetupFail}:{ex.Message}");
                }
                catch (CleanupFailedException ex)
                {
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.CleanupFail}:{ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Fail}:{ex.Message}");
                }
            }
            if (cleanup is not null)
            {
                cleanup(prefix);
            }
        }

        private void DisposeOfTestClass(string prefix, RunCleanupMethods runCleanup, bool dispose)
        {
            long startTime = DateTime.UtcNow.Ticks;
            object testClassInstance = _testClassInstance;
            if (dispose)
            {
                _testClassInstance = null;
            }
            try
            {
                bool success = true;
                if (runCleanup is not null)
                {
                    runCleanup((methodName) =>
                    {
                        if (success)
                        {
                            MethodInfo cleanUpMethod = _testClass.GetMethod(methodName);
                            if (cleanUpMethod is null)
                            {
                                success = false;
                                Console.WriteLine($"{_testClassPrefix}:0:{Communication.MethodError}:Method '{methodName}' not found");
                            }
                            else
                            {
                                Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Cleanup}:{methodName}");
                                try
                                {
                                    cleanUpMethod.Invoke(testClassInstance, s_noArguments);
                                }
                                finally
                                {
                                    Console.WriteLine();
                                }
                            }
                        }
                    });
                }
                if (success)
                {
                    if (dispose && (testClassInstance is IDisposable disposable))
                    {
                        Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Dispose}");
                        try
                        {
                            disposable.Dispose();
                        }
                        finally
                        {
                            Console.WriteLine();
                        }
                    }
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.CleanUpComplete}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.CleanupFail}:{ex.Message}");
                return;
            }
        }
        #endregion
    }
}

