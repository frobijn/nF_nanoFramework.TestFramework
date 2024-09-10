// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.Hardware.Esp32;
using nanoFramework.TestFramework;

namespace NFUnitTest
{
    [TestClass]
    public class ESP32Test
    {
        [TestMethod]
        public void TestMemory()
        {
            NativeMemory.GetMemoryInfo(NativeMemory.MemoryType.All, out uint total, out uint free, out uint block);
            OutputHelper.Write($"All memory - total: {total}, free: {free}, largest free block: {block}");
            Assert.AreNotEqual(0, total);
            Assert.AreNotEqual(0, free);
            Assert.AreNotEqual(0, block);
        }
    }
}
