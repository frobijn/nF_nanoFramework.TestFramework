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
            new UnitTestLauncher(reportPrefix).RunUnitTests();
        }
        #endregion

        #region Construction
        private UnitTestLauncher(string reportPrefix)
        {
            _reportPrefix = reportPrefix;
        }
        #endregion

        #region Selection of tests
        /// <summary>
        /// Get test class information for a candidate test class.
        /// The <see cref="RunUnitTests"/> method will be generated based on the selection of tests.
        /// </summary>
        private partial void RunUnitTests();
        #endregion

        #region Execution of unit tests
        public delegate void RunTestMethods(ForTestMethod runTestMethod, ForDataRowTestMethod runDataRowTest);

        public delegate void ForTestMethod(string testMethodName);

        public delegate void ForDataRowTestMethod(string testMethodName, params int[] dataRowIndices);

        /// <summary>
        /// Run the unit tests in the assembly
        /// </summary>
        /// <param name="testClass">Type of the test class to execute tests of</param>
        /// <param name="instantiate">Instructs how to instantiate and setup/cleanup the class. Numerical value of a combination of <see cref="TestClassInitialisation"/> values.</param>
        public void ForClass(Type testClass, int instantiate, string setupMethodName, object[] deploymentConfiguration, string cleanupMethodName, RunTestMethods runMethods)
        {
            #region Verify the presence of setup/cleanup methods
            string testClassPrefix = _reportPrefix + ":C:" + testClass.FullName;
            Console.WriteLine($"{testClassPrefix}:0:{Communication.Start}");

            ConstructorInfo constructor = null;
            MethodInfo setupMethod = null;
            MethodInfo cleanupMethod = null;
            if ((instantiate & (int)TestClassInitialisation.InstantiateForAllMethods) != 0
                    || (instantiate & (int)TestClassInitialisation.InstantiatePerTestMethod) != 0)
            {
                constructor = testClass.GetConstructor(s_defaultConstructor);
                if (constructor is null)
                {
                    Console.WriteLine($"{testClassPrefix}:0:{Communication.MethodError}:Class does not have a public default constructor");
                    return;
                }
            }

            if (setupMethodName is not null)
            {
                setupMethod = testClass.GetMethod(setupMethodName);
                if (setupMethod is null)
                {
                    Console.WriteLine($"{testClassPrefix}:0:{Communication.MethodError}:Method '{setupMethodName}' not found");
                    return;
                }
            }

            if (cleanupMethodName is not null)
            {
                cleanupMethod = testClass.GetMethod(cleanupMethodName);
                if (cleanupMethod is null)
                {
                    Console.WriteLine($"{testClassPrefix}:0:{Communication.MethodError}:Method '{cleanupMethodName}' not found");
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
                    return InitialiseTestClass(prefix, constructor, setupMethod, deploymentConfiguration);
                }
                void DisposeOfInstance(string prefix)
                {
                    DisposeOfTestClass(prefix, cleanupMethod, true);
                }

                runMethods(
                    (testMethodName)
                        => RunTestMethod(testClass, InitialiseInstance, DisposeOfInstance, testMethodName),

                    (testMethodName, dataRowIndices)
                        => RunDataRowTests(testClass, InitialiseInstance, DisposeOfInstance, testMethodName, dataRowIndices)
                );
                #endregion
            }
            else if ((instantiate & (int)TestClassInitialisation.SetupCleanupPerTestMethod) != 0)
            {
                #region Instantiate a test class once or not at all, setup and cleanup per method
                _testClassInstance = null;
                if (constructor is not null)
                {
                    if (!InitialiseTestClass(testClassPrefix, constructor, null, null))
                    {
                        return;
                    }
                }

                bool InitialiseInstance(string prefix)
                {
                    return InitialiseTestClass(prefix, null, setupMethod, deploymentConfiguration);
                }
                void DisposeOfInstance(string prefix)
                {
                    DisposeOfTestClass(prefix, cleanupMethod, false);
                }

                runMethods(
                    (testMethodName)
                        => RunTestMethod(testClass, InitialiseInstance, DisposeOfInstance, testMethodName),

                    (testMethodName, dataRowIndices)
                        => RunDataRowTests(testClass, InitialiseInstance, DisposeOfInstance, testMethodName, dataRowIndices)
                );
                #endregion
            }
            else
            {
                #region Instantiate a test class, setup and cleanup per method
                _testClassInstance = null;
                if (!InitialiseTestClass(testClassPrefix, constructor, setupMethod, deploymentConfiguration))
                {
                    return;
                }

                runMethods(
                    (testMethodName)
                        => RunTestMethod(testClass, null, null, testMethodName),

                    (testMethodName, dataRowIndices)
                        => RunDataRowTests(testClass, null, null, testMethodName, dataRowIndices)
                );

                DisposeOfTestClass(testClassPrefix, cleanupMethod, true);
                #endregion
            }
        }
        private static readonly Type[] s_defaultConstructor = new Type[] { };

        private bool InitialiseTestClass(string prefix, ConstructorInfo constructor, MethodInfo setupMethod, object[] deploymentConfiguration)
        {
            long startTime = DateTime.UtcNow.Ticks;
            try
            {
                if (constructor is not null && _testClassInstance is null)
                {
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Instantiate}");
                    _testClassInstance = constructor.Invoke(null);
                }

                if (setupMethod is not null)
                {
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Setup}");
                    setupMethod.Invoke(_testClassInstance, deploymentConfiguration);
                }
                Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.SetupComplete}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.SetupFail}:{ex.Message}");
                return false;
            }
        }

        private delegate bool InstanceInitialiser(string prefix);
        private delegate void InstanceDisposer(string prefix);

        private void RunTestMethod(Type testClass, InstanceInitialiser setup, InstanceDisposer cleanup, string testMethodName)
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
                    () => testMethod.Invoke(_testClassInstance, s_noArguments)
                );
            }
        }
        private static readonly object[] s_noArguments = new object[] { };

        private void RunDataRowTests(Type testClass, InstanceInitialiser setup, InstanceDisposer cleanup, string testMethodName, int[] dataRowIndices)
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
                                RunTest(
                                    prefix + dataRowIndex.ToString(),
                                    setup, cleanup,
                                    () => testMethod.Invoke(_testClassInstance, dataRow.MethodParameters)
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
                    testMethod();
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

        private void DisposeOfTestClass(string prefix, MethodInfo cleanupMethod, bool dispose)
        {
            var startTime = DateTime.UtcNow.Ticks;
            var testClassInstance = _testClassInstance;
            if (dispose)
            {
                _testClassInstance = null;
            }
            try
            {
                if (cleanupMethod is not null)
                {
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Cleanup}");
                    cleanupMethod.Invoke(testClassInstance, s_noArguments);
                }
                if (dispose && (testClassInstance is IDisposable disposable))
                {
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Dispose}");
                    disposable.Dispose();
                }
                Console.WriteLine($"{prefix}:0:{Communication.Done}");
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

