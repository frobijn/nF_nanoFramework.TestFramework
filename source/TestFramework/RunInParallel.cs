// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// The attribute marks a test method as being capable to be run in parallel
    /// with other test methods. When applied to a class is the default for its test methods, and can be overridden
    /// by applying <see cref="DoNotRunInParallelAttribute"/> to a test method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RunInParallelAttribute : Attribute, IRunInParallel
    {
        #region Construction
        /// <summary>
        /// Indicate that a test method cannot be executed in parallel with other test methods.
        /// If applied to a class, indicates that none of its test methods can be executed in parallel,
        /// unless that is overridden by a test method attribute that implements <see cref="IRunInParallel"/>,
        /// e.g., <see cref="RunParallelAttribute"/>.
        /// </summary>
        public RunInParallelAttribute()
        {
        }
        #endregion

        #region IRunInParallel implementation
        /// <inheritdoc/>
        bool IRunInParallel.CanRunInParallel
            => true;
        #endregion
    }
}
