// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Interface implemented by attributes that mark a method as being a test method.
    /// The class that defines the method should have an attribute that implements the
    /// <see cref="ITestClass"/> interface.
    /// </summary>
#if NFTF_REFERENCED_SOURCE_FILE
    internal
#else
    public
#endif
     interface ITestMethod
    {
        /// <summary>
        /// Indicates whether the test method can be run. If the property is <c>false</c>,
        /// the test shows up in the Test Explorer but cannot be executed.
        /// </summary>
        /// <remarks>
        /// Attributes that allow for non-executable test should implement <see cref="ITestCategories"/>
        /// to pass the reason why the test cannot be run as a catogory.
        /// </remarks>
        bool CanBeRun
        {
            get;
        }
    }
}
