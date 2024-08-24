// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestFramework.Tooling.BuildTools.Tests.Helpers
{
    public static class TestDirectoryHelper
    {
        public static string GetTestDirectory(TestContext context)
        {
            lock (typeof(TestDirectoryHelper))
            {
                s_lastIndex++;
                string path = Path.Combine(context.ResultsDirectory, s_lastIndex.ToString());
                Debug.WriteLine($"Test directory: {path}");
                Directory.CreateDirectory(path);
                return path;
            }
        }
        private static int s_lastIndex;
    }
}
