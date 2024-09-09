﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace TestFramework.Tooling.UnitTestLauncher.Tests
{
    /// <summary>
    /// This is a program that tests the non-generated code of the
    /// TestFramework.Tooling.UnitTestLauncherGenerator This project
    /// can also be used to code and debug that code.
    ///
    /// The code is included in the TestFramework.Tooling as embedded resource.
    /// It is not tested in the build of TestFramework.Tooling.
    ///
    /// The code below the line depends on the TestFramework.Tooling.Tests.Execution.v3
    /// project. Check and run the test TestFramework_Tooling_UnitTestLauncher_Tests_Asserts
    /// in TestFramework.Tooling.Tests to verify that the data is still correct.
    /// </summary>
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("UnitTestLauncher test program");
            Console.WriteLine("========================================");

            nanoFramework.TestFramework.Tools.UnitTestLauncher.Run("***");
        }
    }
}

//===========================================================================================
//
// The code below is normally generated by the UnitTestLauncherGenerator.
//
// The code may need to be adapted if the TestFramework.Tooling.Tests.Execution.v3
// project is updated.
//

namespace nanoFramework.TestFramework.Tools
{
    public partial class UnitTestLauncher
    {
        private partial void RunUnitTests()
        {
            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.StaticTestClass), (int)TestClassInitialisation.NoInstantiation,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.StaticTestClass.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.StaticTestClass.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.StaticTestClass.Method1), null);
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.StaticTestClass.Method2), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod), (int)TestClassInitialisation.SetupCleanupPerTestMethod,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod.Method1), null);
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod.Method2), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClass), (int)TestClassInitialisation.InstantiateForAllMethods,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClass.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClass.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClass.Method1), null);
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClass.Method2), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod), (int)(TestClassInitialisation.InstantiateForAllMethods | TestClassInitialisation.SetupCleanupPerTestMethod),
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod.Method1), null);
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod.Method2), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod), (int)(TestClassInitialisation.InstantiatePerTestMethod | TestClassInitialisation.SetupCleanupPerTestMethod),
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod.Method1), null);
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod.Method2), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.FailInConstructor), (int)TestClassInitialisation.InstantiateForAllMethods,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInConstructor.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInConstructor.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInConstructor.Test), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.FailInSetup), (int)TestClassInitialisation.InstantiateForAllMethods,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInSetup.Setup), null);
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInSetup.Setup2), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInSetup.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInSetup.Test), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.FailInFirstSetup), (int)TestClassInitialisation.InstantiateForAllMethods,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInFirstSetup.Setup), null);
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInFirstSetup.Setup2), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInFirstSetup.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInFirstSetup.Test), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.FailInTest), (int)TestClassInitialisation.InstantiateForAllMethods,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInTest.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInTest.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInTest.Test), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.SkippedInTest), (int)TestClassInitialisation.InstantiateForAllMethods,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.SkippedInTest.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.SkippedInTest.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.SkippedInTest.Test), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.CleanupFailedInTest), (int)TestClassInitialisation.InstantiateForAllMethods,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.CleanupFailedInTest.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.CleanupFailedInTest.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.CleanupFailedInTest.Test), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.FailInCleanUp), (int)TestClassInitialisation.InstantiateForAllMethods,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInCleanUp.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInCleanUp.Cleanup));
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInCleanUp.Cleanup2));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInCleanUp.Test), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.FailInFirstCleanUp), (int)TestClassInitialisation.InstantiateForAllMethods,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInFirstCleanUp.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInFirstCleanUp.Cleanup));
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInFirstCleanUp.Cleanup2));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInFirstCleanUp.Test), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.FailInDispose), (int)TestClassInitialisation.InstantiateForAllMethods,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInDispose.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInDispose.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.FailInDispose.Test), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.NonFailingTest), (int)TestClassInitialisation.InstantiateForAllMethods,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.NonFailingTest.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Execution.Tests.NonFailingTest.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.NonFailingTest.Test), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.TestWithMethods), (int)TestClassInitialisation.InstantiateForAllMethods,
                null,
                null,
                (rtm, rdr) =>
                {
                    rdr(nameof(global::TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1), null, 0, 1);
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.TestWithMethods.Test2), null);
                }
            );

            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions), (int)TestClassInitialisation.InstantiateForAllMethods,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.Setup),
                        new object[] {
                            s_cfg_3,
                            CFG_4 // setup will fail
                        });
                },
                null,
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile), new object[] { CFG_1 });
                }
            );


            ForClass(
                typeof(global::TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes), (int)TestClassInitialisation.InstantiateForAllMethods,
                null,
                null,
                (rtm, rdr) =>
                {
                    rdr(nameof(global::TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardwareWithData), new object[] { CFG_2 }, 0);
                    rtm(nameof(global::TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware), new object[] { CFG_5 });
                }
            );
        }

        private const string CFG_1 = "ConfigValue";
        private const long CFG_2 = 42;
        private static readonly byte[] s_cfg_3 = new byte[] { 42 };
        private const string CFG_4 = "";
        private const int CFG_5 = 42;
    }
}
