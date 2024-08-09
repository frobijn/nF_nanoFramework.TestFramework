// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace nanoFramework.TestFramework.Tools
{
    /// <summary>
    /// This class is a unit test launcher for nanoFramework
    /// </summary>
    public partial class UnitTestLauncher
    {
        private readonly string _reportPrefix;

        public UnitTestLauncher(string assemblyName, string reportPrefix)
        {
            _reportPrefix = reportPrefix;
            Assembly test;
            try
            {
                test = Assembly.Load(assemblyName);
            }
            catch
            {
                Console.WriteLine($"{_reportPrefix}:Error:Cannot load assembly {assemblyName}");
                return;
            }
            RunUnitTests();
        }

        #region Selection of tests
        /// <summary>
        /// Get test class information for a candidate test class
        /// The isTestClass method will be generated based on the selection of tests
        /// </summary>
        partial void RunUnitTests();
        #endregion

        public delegate void RunTestMethods(object testClassInstance, ForTestMethod runTestMethod, ForDataRowTestMethod runDataRowTest);

        public delegate void SetupCleanup(object testClassInstance);

        public delegate void ForTestMethod(Action testMethod, int testMethodIndex);

        public delegate void ForDataRowTestMethod(string testMethodName, int testMethodIndex, params int[] dataRowIndices);


        /// <summary>
        /// Run the unit tests in the assembly
        /// </summary>
        public void ForClass(Type testClass, int classIndex, bool instantiate, SetupCleanup setup, SetupCleanup cleanup, RunTestMethods runMethods)
        {
            #region Create and setup the test class instance
            Console.WriteLine($"{_reportPrefix}:C{classIndex}:0:Start");
            object testClassInstance = null;
            long startTime = DateTime.UtcNow.Ticks;

            try
            {
                if (instantiate)
                {
                    ConstructorInfo constructor = testClass.GetConstructor(s_defaultConstructor);
                    if (constructor is null)
                    {
                        Console.WriteLine($"{_reportPrefix}:C{classIndex}:{DateTime.UtcNow.Ticks - startTime}:SetupError:Class does not have a public default constructor");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"{_reportPrefix}:C{classIndex}:{DateTime.UtcNow.Ticks - startTime}:Instantiate");
                    }
                    testClassInstance = constructor.Invoke(null);
                }

                if (setup is not null)
                {
                    Console.WriteLine($"{_reportPrefix}:C{classIndex}:{DateTime.UtcNow.Ticks - startTime}:Setup");
                    setup(testClassInstance);
                }
                Console.WriteLine($"{_reportPrefix}:C{classIndex}:{DateTime.UtcNow.Ticks - startTime}:SetupComplete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_reportPrefix}:C{classIndex}:{DateTime.UtcNow.Ticks - startTime}:SetupError:{ex.Message}");
                return;
            }
            #endregion

            #region Run the test methods
            runMethods(
                testClassInstance,

                (testMethod, testMethodIndex)
                    => RunTestMethod($"{_reportPrefix}:C{classIndex}T{testMethodIndex}", testMethod),

                (testMethodName, testMethodIndex, dataRowIndices)
                    => RunDataRowTests($"{_reportPrefix}:C{classIndex}T{testMethodIndex}", testClass, testClassInstance, testMethodName, dataRowIndices)
            );
            #endregion

            #region Cleanup and disposal
            Console.WriteLine($"{_reportPrefix}:C{classIndex}:0:TestsComplete");
            startTime = DateTime.UtcNow.Ticks;
            try
            {
                if (cleanup is not null)
                {
                    Console.WriteLine($"{_reportPrefix}:C{classIndex}:{DateTime.UtcNow.Ticks - startTime}:Cleanup");
                    cleanup(testClassInstance);
                }
                if (testClassInstance is IDisposable disposable)
                {
                    Console.WriteLine($"{_reportPrefix}:C{classIndex}:{DateTime.UtcNow.Ticks - startTime}:Dispose");
                    disposable.Dispose();
                }

                Console.WriteLine($"{_reportPrefix}:C{classIndex}:{DateTime.UtcNow.Ticks - startTime}:Done");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_reportPrefix}:C{classIndex}:{DateTime.UtcNow.Ticks - startTime}:CleanupError:{ex.Message}");
                return;
            }
            #endregion
        }
        private static readonly Type[] s_defaultConstructor = new Type[] { };

        private void RunDataRowTests(string prefix, Type testClass, object testClassInstance, string testMethodName, int[] dataRowIndices)
        {
            MethodInfo testMethod = testClass.GetMethod(testMethodName);

            if (testMethod is null)
            {
                foreach (int dataRowIndex in dataRowIndices)
                {
                    Console.WriteLine($"{prefix}D{dataRowIndex}:0:Start");
                    Console.WriteLine($"{prefix}D{dataRowIndex}:0:SetupError:Test method not found");
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
                                RunTestMethod(
                                    $"{prefix}D{dataRowIndex}",
                                    () => testMethod.Invoke(testClassInstance, dataRow.MethodParameters)
                                );
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void RunTestMethod(string prefix, Action testMethod)
        {
            long startTime = DateTime.UtcNow.Ticks;
            Console.WriteLine($"{prefix}:0:Start");
            try
            {
                testMethod();
                Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:Success");
            }
            catch (InconclusiveException ex)
            {
                Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:SetupError:{ex.Message}");
            }
            catch (CleanupFailedException ex)
            {
                Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:CleanUpError:{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{prefix}:{DateTime.UtcNow.Ticks - startTime}:Failed:{ex.Message}");
            }
        }
    }
}

