// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Interface implemented by attributes that provide information on whether a test method
    /// can be run in parallel with other test methods. The attributes can be applied to an assembly,
    /// class or method. When applied to an assembly it is the default for all test classes,
    /// when applied to a test class it is the default for all test methods.
    /// The default value for a class is used if there are no attributes applied to a test method
    /// that implement this interface; the default value for an assembly to a test class if there are no
    /// attributes applied to the class.
    /// </summary>
#if REFERENCED_IN_NFUNITMETADATA
    internal
#else
    public
#endif
     interface IRunInParallel
    {
        /// <summary>
        /// Indicates whether the method can be run in parallel with other test methods.
        /// If multiple attributes that implement this interface are applied to the same
        /// assembly, class or method, the result is <c>false</c> if this property is
        /// <c>false</c> for any of the attributes.
        /// </summary>
        bool CanRunInParallel
        {
            get;
        }
    }
}
