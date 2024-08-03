// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Interface implemented by attributes that provide the data to pass
    /// as parameters into a call to a test method. A test method can have multiple
    /// attributes that implement <see cref="IDataRow"/>. Each instance of an attribute
    /// corresponds to a test run.
    /// </summary>
#if REFERENCED_IN_NFUNITMETADATA
    internal
#else
    public
#endif
     interface IDataRow
    {
        /// <summary>
        /// Array containing all passed parameters
        /// </summary>
        object[] MethodParameters { get; }
    }
}
