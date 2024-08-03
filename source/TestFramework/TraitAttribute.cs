// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Attribute that publishes a characterization of the test.
    /// The category are listed under 'traits' in the Visual Studio test explorer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TraitAttribute : Attribute, ITraits
    {
        #region Fields
        private readonly string _traits;
        #endregion

        #region Construction
        /// <summary>
        /// Assign a category to a test
        /// </summary>
        /// <param name="trait">The trait/category to assign</param>
        public TraitAttribute(string trait)
        {
            _traits = trait;
        }
        #endregion

        #region ITraits implementation
        string[] ITraits.Traits
            => new string[] { _traits };
        #endregion
    }
}
