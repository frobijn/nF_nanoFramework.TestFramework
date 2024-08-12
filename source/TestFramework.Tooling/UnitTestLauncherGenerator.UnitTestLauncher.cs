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

        public delegate void SetupCleanup(object testClassInstance);

        public delegate void ForTestMethod(string testMethodName);

        public delegate void ForDataRowTestMethod(string testMethodName, params int[] dataRowIndices);

        /// <summary>
        /// Run the unit tests in the assembly
        /// </summary>
        public void ForClass(Type testClass, bool instantiate, string setupMethodName, string cleanupMethodName, RunTestMethods runMethods)
        {
            #region Create and setup the test class instance
            string prefix = _reportPrefix + ":C:" + testClass.FullName;
            Console.WriteLine($"{prefix}:0:{Communication.Start}");
            object testClassInstance = null;
            long startTime = DateTime.UtcNow.Ticks;

            try
            {
                if (instantiate)
                {
                    ConstructorInfo constructor = testClass.GetConstructor(s_defaultConstructor);
                    if (constructor is null)
                    {
                        Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.MethodError}:Class does not have a public default constructor");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Instantiate}");
                    }
                    testClassInstance = constructor.Invoke(null);
                }

                if (setupMethodName is not null)
                {
                    MethodInfo method = testClass.GetMethod(setupMethodName);
                    if (method is null)
                    {
                        Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.MethodError}:Method '{setupMethodName}' not found");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Setup}");
                        method.Invoke(testClassInstance, s_noArguments);
                    }
                }
                if (instantiate || setupMethodName is not null)
                {
                    Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.SetupComplete}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.SetupFail}:{ex.Message}");
                return;
            }
            #endregion

            #region Run the test methods
            runMethods(
                (testMethodName)
                    => RunTestMethod(testClass, testClassInstance, testMethodName),

                (testMethodName, dataRowIndices)
                    => RunDataRowTests(testClass, testClassInstance, testMethodName, dataRowIndices)
            );
            #endregion

            #region Cleanup and disposal
            startTime = DateTime.UtcNow.Ticks;
            try
            {
                if (cleanupMethodName is not null)
                {
                    MethodInfo method = testClass.GetMethod(cleanupMethodName);
                    if (method is null)
                    {
                        Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.MethodError}:Method '{cleanupMethodName}' not found");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:{Communication.Cleanup}");
                        method.Invoke(testClassInstance, s_noArguments);
                    }
                }
                if (testClassInstance is IDisposable disposable)
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
            #endregion
        }
        private static readonly Type[] s_defaultConstructor = new Type[] { };


        private void RunTestMethod(Type testClass, object testClassInstance, string testMethodName)
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
                    () => testMethod.Invoke(testClassInstance, s_noArguments)
                );
            }
        }
        private static readonly object[] s_noArguments = new object[] { };

        private void RunDataRowTests(Type testClass, object testClassInstance, string testMethodName, int[] dataRowIndices)
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
                                    () => testMethod.Invoke(testClassInstance, dataRow.MethodParameters)
                                );
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void RunTest(string prefix, Action testMethod)
        {
            long startTime = DateTime.UtcNow.Ticks;
            Console.WriteLine($"{prefix}:0:{Communication.Start}");
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
        #endregion
    }
}

