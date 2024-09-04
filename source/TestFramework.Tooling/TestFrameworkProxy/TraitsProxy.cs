// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for an attribute that implements <see cref="ITrait"/>
    /// </summary>
    public sealed class TraitsProxy : AttributeProxy
    {
        #region Fields
        private readonly object _attribute;
        private readonly TestFrameworkImplementation _framework;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="framework">Information about the implementation of the test framework</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal TraitsProxy(object attribute, TestFrameworkImplementation framework, Type interfaceType)
        {
            _attribute = attribute;
            _framework = framework;
            _framework.AddProperty<string[]>(typeof(ITraits).FullName, interfaceType, nameof(ITraits.Traits));
        }
        #endregion

        #region Access to the ITrait interface
        /// <summary>
        /// Get the traits or categories that are assigned to the test
        /// </summary>
        public string[] Traits
            => _framework.GetPropertyValue<string[]>(typeof(ITraits).FullName, nameof(ITraits.Traits), _attribute);
        #endregion

        #region Helpers
        /// <summary>
        /// Collect all <see cref="Traits"/> in a set of unique traits values.
        /// </summary>
        /// <param name="inherited">Inherited collection of traits. Pass <c>null</c> if no traits are inherited.</param>
        /// <param name="attributes">Attributes that specify traits to add to the collection; may be <c>null</c> if no attributes have to be added.</param>
        /// <param name="extraTraits">Traits that are not passed via an attribute; may be empty if there are no such traits.</param>
        /// <returns>The collection of traits, or <c>null</c> if there are none.</returns>
        internal static HashSet<string> Collect(HashSet<string> inherited, IEnumerable<TraitsProxy> attributes, IEnumerable<string> extraTraits = null)
        {
            if ((attributes is null || !attributes.Any())
                && (extraTraits is null || !extraTraits.Any()))
            {
                return inherited;
            }

            HashSet<string> result = inherited is null ? null : new HashSet<string>(inherited);

            if (!(attributes is null))
            {
                foreach (TraitsProxy attribute in attributes)
                {
                    string[] traits = attribute.Traits;
                    if (!(traits is null) && traits.Length > 0)
                    {
                        result ??= new HashSet<string>();
                        result.UnionWith(traits);
                    }
                }
            }
            if (!(extraTraits is null))
            {
                result ??= new HashSet<string>();
                result.UnionWith(extraTraits);
            }
            return result;
        }
        #endregion
    }
}
