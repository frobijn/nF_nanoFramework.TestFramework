// Copyright (c) .NET Foundation and Contributors.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using nanoFramework.TestFramework;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for an attribute that implements <see cref="ITrait"/>
    /// </summary>
    public sealed class TraitsProxy : AttributeProxy
    {
        #region Fields
        private readonly Attribute _attribute;
        private static PropertyInfo s_traits;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal TraitsProxy(Attribute attribute, Type interfaceType)
        {
            TestDeviceProxy.FoundITestDevice(interfaceType);
            _attribute = attribute;
            if (s_traits is null)
            {
                s_traits = interfaceType.GetProperty(nameof(ITraits.Traits));
                if (s_traits is null
                    || s_traits.PropertyType != typeof(string[]))
                {
                    s_traits = null;
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
            => (string[])s_traits.GetValue(_attribute, null);
        #endregion
    }
}
