//
// Copyright (c) .NET Foundation and Contributors
// Portions Copyright (c) Microsoft Corporation.  All rights reserved.
// See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Reflection;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// This class is a unit test launcher for nanoFramework
    /// </summary>
    public class UnitTestLauncher
    {
        /// <summary>
        /// Main function
        /// </summary>
        public static void Main()
        {
            Assembly test = Assembly.Load("NFUnitTest");

            Type[] allTypes = test.GetTypes();

            foreach (Type type in allTypes)
            {
                if (!type.IsClass)
                {
                    continue;
                }

                object[] typeAttribs = type.GetCustomAttributes(true);

                foreach (object typeAttrib in typeAttribs)
                {
                    if (typeof(TestClassAttribute) != typeAttrib.GetType())
                    {
                        continue;
                    }

                    MethodInfo[] methods = type.GetMethods();

                    // First we look at Setup
                    bool continueTests = RunTest(methods, typeof(SetupAttribute));

                    if (continueTests)
                    {
                        // then we run the tests
                        RunTest(methods, typeof(TestMethodAttribute));
                        RunTest(methods, typeof(DataRowAttribute));

                        // last we handle Cleanup
                        RunTest(methods, typeof(CleanupAttribute));
                    }
                }
            }
        }

        private static bool RunTest(
            MethodInfo[] methods,
            Type attribToRun)
        {
            long dt;
            long totalTicks;
            bool isSetupMethod = attribToRun == typeof(SetupAttribute);

            foreach (MethodInfo method in methods)
            {
                object[] attribs = method.GetCustomAttributes(true);
                attribs = Helper.RemoveTestMethodIfDataRowExists(attribs);

                for (int i = 0; i < attribs.Length; i++)
                {
                    object attrib = attribs[i];
                    string methodName = $"{method.DeclaringType.FullName}.{method.Name}.{i}";

                    if (attribToRun != attrib.GetType())
                    {
                        continue;
                    }

                    try
                    {
                        dt = DateTime.UtcNow.Ticks;
                        object[] parameters = GetParameters(attrib);
                        method.Invoke(null, parameters);
                        totalTicks = DateTime.UtcNow.Ticks - dt;

                        // on change this pattern it has to be updated at Executor.CheckAllTests 
                        Console.WriteLine($"Test passed,{methodName},{totalTicks}");
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType() == typeof(SkipTestException))
                        {
                            // on change this pattern it has to be updated at Executor.CheckAllTests
                            Console.WriteLine($"Test skipped,{methodName},{ex.Message}");

                            if (isSetupMethod)
                            {
                                // In case the Setup attribute test is skipped, we will skip
                                // All the other tests
                                return false;
                            }
                        }
                        else
                        {
                            // on change this pattern it has to be updated at Executor.CheckAllTests 
                            Console.WriteLine($"Test failed,{methodName},{ex.Message}");
                        }
                    }
                }
            }

            return true;
        }

        private static object[] GetParameters(object attribute)
        {
            if (attribute.GetType() != typeof(DataRowAttribute))
            {
                return null;
            }

            var testCaseAttribute = (DataRowAttribute)attribute;
            return testCaseAttribute.MethodParameters;
        }
    }
}
