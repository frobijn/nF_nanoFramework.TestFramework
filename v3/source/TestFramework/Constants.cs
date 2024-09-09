// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    internal static class Constants
    {
        /// <summary>
        /// The description used for a real hardware nanoDevice.
        /// </summary>
        public const string RealHardware_Description = "Hardware nanoDevice";
#if NFTF_REFERENCED_SOURCE_FILE
        /// <summary>
        /// The description used for a virtual nanoDevice.
        /// </summary>
        public const string VirtualDevice_Description = "Virtual nanoDevice";

        /// <summary>
        /// Test category for tests that run on the virtual device.
        /// </summary>
        public const string RealHardware_TestCategory = "@Hardware nanoDevice";

        /// <summary>
        /// Test category for tests that run on the virtual device.
        /// </summary>
        public const string VirtualDevice_TestCategory = "@Virtual nanoDevice";
#endif
    }
}
