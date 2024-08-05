// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest
{
    [TestClass(true, false)]
    public class TestRunInParallelOverruled
    {
        [TestMethod]
        public void RunInIsolationBecauseOfAssemblyAttribute()
        {
        }

        [RunInParallel]
        public void RunInParallelBecauseOfMethodAttribute()
        {
        }
    }

    [TestClass(true, false)]
    [RunInParallel]
    public class TestRunInParallel
    {
        [RunInIsolation]
        public void RunInIsolationBecauseOfMethodAttribute()
        {
        }

        [TestMethod]
        public void RunInParallelBecauseOfClassAttribute()
        {
        }
    }

    [TestClass(false, false)]
    [RunInParallel]
    public class TestRunInParallelButNotItsMethods
    {
        [TestMethod]
        public void RunParallelWithOthersOneByOneInClass1()
        {
        }

        [TestMethod]
        public void RunParallelWithOthersOneByOneInClass2()
        {
        }
    }
}
