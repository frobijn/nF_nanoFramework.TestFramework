// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for a <see cref="IDataRow"/> implementation
    /// </summary>
    public sealed class DataRowProxy : AttributeProxy
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
        internal DataRowProxy(object attribute, TestFrameworkImplementation framework, Type interfaceType)
        {
            _attribute = attribute;
            _framework = framework;

            if (_framework._property_IDataRow_MethodParameters is null)
            {
                _framework._property_IDataRow_MethodParameters = interfaceType.GetProperty(nameof(IDataRow.MethodParameters));
                if (_framework._property_IDataRow_MethodParameters is null
                    || _framework._property_IDataRow_MethodParameters.PropertyType != typeof(object[]))
                {
                    _framework._property_IDataRow_MethodParameters = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(IDataRow)}.${nameof(IDataRow.MethodParameters)}");
                }
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Array containing all passed parameters
        /// </summary>
        public object[] MethodParameters
            => (object[])_framework._property_IDataRow_MethodParameters.GetValue(_attribute, null);

        /// <summary>
        /// Presents the <see cref="MethodParameters"/> as a string "(..,..,..)"
        /// </summary>
        public string MethodParametersAsString
        {
            get
            {
                object[] parameters = MethodParameters;
                var result = new StringBuilder("(");
                if (!(parameters is null))
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (i > 0)
                        {
                            result.Append(',');
                        }
                        string value;
                        try
                        {
                            value = parameters[i].ToString();
                            if (value == parameters[i].GetType().ToString())
                            {
                                value = "[object]";
                            }
                            else
                            {
                                int idx = value.IndexOfAny(new char[] { '\r', '\n' });
                                if (idx > 0)
                                {
                                    value = value.Substring(0, idx) + "...";
                                }
                            }
                        }
                        catch
                        {
                            value = "[object]";
                        }
                        result.Append(value);
                    }
                }
                result.Append(')');
                return result.ToString();
            }
        }
        #endregion
    }
}
