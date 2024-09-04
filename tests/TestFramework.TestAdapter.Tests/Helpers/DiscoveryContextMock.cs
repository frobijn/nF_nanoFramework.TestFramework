// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace TestFramework.TestAdapter.Tests.Helpers
{
    /// <summary>
    /// The test adapter does not use the interface implementation.
    /// </summary>
    public sealed class DiscoveryContextMock : IDiscoveryContext
    {
        public IRunSettings RunSettings
            => throw new NotImplementedException();
    }
}
