// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// The attribute to indicate that a test method cannot be run at this moment.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SkipTestAttribute : Attribute, ITestMethod, ITestCategories
    {
        #region Fields
        private readonly string[] _testCategories;
        #endregion

        #region Construction
        /// <summary>
        /// Indicate that the test method cannot not be executed at the moment,
        /// but should show up in the Test Explorer.
        /// </summary>
        /// <param name="reason">Reason why the test cannot be run. This is shown in the test explorer as a trait.</param>
        public SkipTestAttribute(string reason)
        {
            _testCategories = new string[] { reason };
        }
        #endregion

        #region ITestMethod implementation
        bool ITestMethod.CanBeRun
            => false;
        #endregion

        #region ITestCategories implementation
        string[] ITestCategories.Categories
            => _testCategories;
        #endregion
    }
}
