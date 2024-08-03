// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for an attribute that implements <see cref="ITestOnVirtualDevice"/>
    /// </summary>
    public sealed class TestOnVirtualDeviceProxy : AttributeProxy
    {
        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        internal TestOnVirtualDeviceProxy()
        {
        }
        #endregion
    }
}
