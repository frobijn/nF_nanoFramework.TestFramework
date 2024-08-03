// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for an <see cref="IRunInParallel"/> implementation
    /// </summary>
    public sealed class RunInParallelProxy : AttributeProxy
    {
        #region Fields
        private static PropertyInfo s_canRunInParallel;
        private readonly Attribute _attribute;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal RunInParallelProxy(Attribute attribute, Type interfaceType)
        {
            _attribute = attribute;

            if (s_canRunInParallel is null)
            {
                s_canRunInParallel = interfaceType.GetProperty(nameof(IRunInParallel.CanRunInParallel));
                if (s_canRunInParallel is null
                    || s_canRunInParallel.PropertyType != typeof(bool))
                {
                    s_canRunInParallel = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(IRunInParallel)}.${nameof(IRunInParallel.CanRunInParallel)}");
                }
            }
        }
        #endregion

        #region Properties
        // <summary>
        /// Indicates whether the method can be run in parallel with other test methods.
        /// If multiple attributes that implement this interface are applied to the same
        /// assembly, class or method, the result is <c>false</c> if this property is
        /// <c>false</c> for any of the attributes.
        /// </summary>
        public bool CanRunInParallel
            => (bool)s_canRunInParallel.GetValue(_attribute, null);
        #endregion

        #region Helper methods
        /// <summary>
        /// Combine the settings from multiple attributes
        /// </summary>
        /// <param name="attributes">The attributes that determine whether or not to run a test in parallel with other tests</param>
        /// <param name="defaultValue">The default value</param>
        /// <returns>The resulting value that indicates whether tests can be run in parallel</returns>
        public static bool RunInParallel(IEnumerable<RunInParallelProxy> attributes, bool defaultValue = true)
        {
            if (attributes.Any())
            {
                bool result = true;
                foreach (RunInParallelProxy attribute in attributes)
                {
                    if (!attribute.CanRunInParallel)
                    {
                        result = false;
                        break;
                    }
                }
                return result;
            }
            else
            {
                return defaultValue;
            }
        }
        #endregion
    }
}
