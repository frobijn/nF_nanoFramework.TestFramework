// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for an attribute that implements <see cref="ITestCategories"/>
    /// </summary>
    public sealed class TestCategoriesProxy : AttributeProxy
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
        internal TestCategoriesProxy(object attribute, TestFrameworkImplementation framework, Type interfaceType)
        {
            _attribute = attribute;
            _framework = framework;
            _framework.AddProperty<string[]>(typeof(ITestCategories).FullName, interfaceType, nameof(ITestCategories.Categories));
        }
        #endregion

        #region Access to the ITestCategories interface
        /// <summary>
        /// Get the categories that are assigned to the test
        /// </summary>
        public string[] Categories
            => _framework.GetPropertyValue<string[]>(typeof(ITestCategories).FullName, nameof(ITestCategories.Categories), _attribute);
        #endregion

        #region Helpers
        /// <summary>
        /// Collect all <see cref="Categories"/> in a set of unique categories values.
        /// </summary>
        /// <param name="inherited">Inherited collection of categories. Pass <c>null</c> if no categories are inherited.</param>
        /// <param name="attributes">Attributes that specify categories to add to the collection; may be <c>null</c> if no attributes have to be added.</param>
        /// <param name="extraCategories">Categories that are not passed via an attribute; may be empty if there are no such categories.</param>
        /// <returns>The collection of categories, or <c>null</c> if there are none.</returns>
        internal static HashSet<string> Collect(HashSet<string> inherited, IEnumerable<TestCategoriesProxy> attributes, IEnumerable<string> extraCategories = null)
        {
            if ((attributes is null || !attributes.Any())
                && (extraCategories is null || !extraCategories.Any()))
            {
                return inherited;
            }

            HashSet<string> result = inherited is null ? null : new HashSet<string>(inherited);

            if (!(attributes is null))
            {
                foreach (TestCategoriesProxy attribute in attributes)
                {
                    string[] categories = attribute.Categories;
                    if (!(categories is null) && categories.Length > 0)
                    {
                        result ??= new HashSet<string>();
                        result.UnionWith(categories);
                    }
                }
            }
            if (!(extraCategories is null))
            {
                result ??= new HashSet<string>();
                result.UnionWith(extraCategories);
            }
            return result;
        }
        #endregion
    }
}
