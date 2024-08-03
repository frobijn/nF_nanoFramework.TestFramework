// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Clean up attribute typically used to clean up after the tests, it will always been called the last after all the Test Method run.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
#if REFERENCED_IN_NFUNITMETADATA
    internal
#else
    public
#endif
    sealed class CleanupAttribute : Attribute
    {
    }
}
