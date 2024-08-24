// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace TestOfTestDebugProjectByReference
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Unit tests debugger PoC");
            Console.WriteLine("========================================");

            nanoFramework.TestFramework.Tools.UnitTestLauncher.Run("***");
        }
    }
}
