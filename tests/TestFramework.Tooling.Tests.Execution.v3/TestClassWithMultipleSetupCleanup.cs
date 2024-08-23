// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Execution.Tests
{
    [TestClass]
    public sealed class TestClassWithMultipleSetupCleanup : IDisposable
    {
        public TestClassWithMultipleSetupCleanup()
        {
            _constructorCalled = true;
        }
        private readonly bool _constructorCalled;

        [Setup]
        public void Setup1()
        {
            _setup1Called = true;
        }
        private bool _setup1Called;

        [Setup]
        public void Setup2()
        {
            _setup2Called = true;
        }
        private bool _setup2Called;

        [Cleanup]
        public void Cleanup1()
        {
            _cleanup1Called = true;
        }
        private bool _cleanup1Called;

        [Cleanup]
        public void Cleanup2()
        {
            _cleanup2Called = true;
        }
        private bool _cleanup2Called;


        public void Dispose()
        {
            Assert.IsTrue(_constructorCalled);
            Assert.IsTrue(_setup1Called);
            Assert.IsTrue(_setup2Called);
            Assert.IsTrue(_cleanup1Called);
            Assert.IsTrue(_cleanup2Called);
        }
    }
}
