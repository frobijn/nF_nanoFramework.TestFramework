// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Interface implemented by attributes that assign one or more traits or categories
    /// as a characterization of the test. The categories are listed under 'traits' in
    /// the Visual Studio test explorer. If no attribute that implements this interface
    /// is applied to a <see cref="TestMethodAttribute"/>, a default category is
    /// assigned.
    /// </summary>
#if REFERENCED_IN_NFUNITMETADATA
    internal
#else
    public
#endif
        interface ITraits
    {
        /// <summary>
        /// Get the traits or categories that are assigned to the test
        /// </summary>
        string[] Traits
        {
            get;
        }
    }
}
