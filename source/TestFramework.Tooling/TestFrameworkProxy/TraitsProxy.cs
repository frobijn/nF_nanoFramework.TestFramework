// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for an attribute that implements <see cref="ITrait"/>
    /// </summary>
    public sealed class TraitsProxy : AttributeProxy
    {
        #region Fields
        private readonly Attribute _attribute;
        private readonly TestFrameworkImplementation _framework;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="framework">Information about the implementation of the test framework</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal TraitsProxy(Attribute attribute, TestFrameworkImplementation framework, Type interfaceType)
        {
            _attribute = attribute;
            _framework = framework;
            if (_framework._property_ITraits_Traits is null)
            {
                _framework._property_ITraits_Traits = interfaceType.GetProperty(nameof(ITraits.Traits));
                if (_framework._property_ITraits_Traits is null
                    || _framework._property_ITraits_Traits.PropertyType != typeof(string[]))
                {
                    _framework._property_ITraits_Traits = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(ITraits)}.${nameof(ITraits.Traits)}");
                }
            }
        }
        #endregion

        #region Access to the ITrait interface
        /// <summary>
        /// Get the traits or categories that are assigned to the test
        /// </summary>
        public string[] Traits
            => (string[])_framework._property_ITraits_Traits.GetValue(_attribute, null);
        #endregion
    }
}
