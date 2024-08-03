// Copyright (c) .NET Foundation and Contributors.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using nanoFramework.TestFramework;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for an <see cref="ITestMethod"/> implementation
    /// </summary>
    public sealed class TestMethodProxy : AttributeProxy
    {
        #region Fields
        private static PropertyInfo s_canBeRun;
        private readonly Attribute _attribute;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal TestMethodProxy(Attribute attribute, Type interfaceType)
        {
            _attribute = attribute;

            if (s_canBeRun is null)
            {
                s_canBeRun = interfaceType.GetProperty(nameof(ITestMethod.CanBeRun));
                if (s_canBeRun is null
                    || s_canBeRun.PropertyType != typeof(bool))
                {
                    s_canBeRun = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(ITestMethod)}.${nameof(ITestMethod.CanBeRun)}");
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
        public bool CanBeRun
            => (bool)s_canBeRun.GetValue(_attribute, null);
        #endregion
    }
}
