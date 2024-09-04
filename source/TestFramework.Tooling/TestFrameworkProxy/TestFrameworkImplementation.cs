// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Contains information about the implementation of the test framework
    /// on the nanoFramework platform.
    /// </summary>
    public sealed class TestFrameworkImplementation
    {
        #region Fields
        private readonly Dictionary<string, Dictionary<string, PropertyInfo>> _properties = new Dictionary<string, Dictionary<string, PropertyInfo>>();
        private readonly Dictionary<string, Dictionary<string, MethodInfo>> _methods = new Dictionary<string, Dictionary<string, MethodInfo>>();
        #endregion

        #region Properties
        /// <summary>
        /// Get the .NET type for the nanoFramework.TestFramework.Tools.TestDeviceProxy
        /// class in the test assembly.
        /// </summary>
        internal Type TestDeviceProxyType
        {
            get; private set;
        }

        // <summary>
        /// Get the .NET type for the nanoFramework.TestFramework.ITestDevice
        /// interface in the test assembly.
        /// </summary>
        internal Type ITestDeviceType
        {
            get; private set;
        }
        #endregion

        #region Methods for attribute properties
        /// <summary>
        /// Add the implementation information of a property of a nanoFramework class or interface.
        /// </summary>
        /// <typeparam name="PropertyType">Type of the value of the property.</typeparam>
        /// <param name="nanoFrameworkTypeFullName">Full name of the nanoFramework class or interface.</param>
        /// <param name="nanoFrameworkType">The .NET type that represents the nanoFramework class or interface.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <exception cref="FrameworkMismatchException">Thrown if the property is not found in <paramref name="nanoFrameworkType"/>
        /// or if the <typeparamref name="PropertyType"/> does not match the type of the property.</exception>
        public void AddProperty<PropertyType>(string nanoFrameworkTypeFullName, Type nanoFrameworkType, string propertyName)
        {
            AddProperty(nanoFrameworkTypeFullName, nanoFrameworkType, propertyName, typeof(PropertyType));
        }

        /// <summary>
        /// Add the implementation information of a property of a nanoFramework class or interface.
        /// </summary>
        /// <param name="nanoFrameworkTypeFullName">Full name of the nanoFramework class or interface.</param>
        /// <param name="nanoFrameworkType">The .NET type that represents the nanoFramework class or interface.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="propertyType">Type of the value of the property.</param>
        /// <exception cref="FrameworkMismatchException">Thrown if the property is not found in <paramref name="nanoFrameworkType"/>
        /// or if the <paramref name="propertyType"/> does not match the type of the property.</exception>
        public void AddProperty(string nanoFrameworkTypeFullName, Type nanoFrameworkType, string propertyName, Type propertyType)
        {
            if (!_properties.TryGetValue(nanoFrameworkTypeFullName, out Dictionary<string, PropertyInfo> properties))
            {
                _properties[nanoFrameworkTypeFullName] = properties = new Dictionary<string, PropertyInfo>();
            }
            if (!properties.ContainsKey(propertyName))
            {
                PropertyInfo property = nanoFrameworkType.GetProperty(propertyName);
                properties[propertyName] = property;
                if (property is null)
                {
                    throw new FrameworkMismatchException($"The nanoFramework type '{nanoFrameworkTypeFullName}' has no public property '{propertyName}'.");
                }
                else if (property.PropertyType != propertyType)
                {
                    properties[propertyName] = null;
                    throw new FrameworkMismatchException($"The nanoFramework type of the value of property '{propertyName}' of '{nanoFrameworkTypeFullName}' is '{property.PropertyType.FullName}', not '{propertyType.FullName}'.");
                }
            }
        }

        /// <summary>
        /// Get the value of a property of a nanoFramework attribute.
        /// </summary>
        /// <typeparam name="PropertyType">Type of the value of the property.</typeparam>
        /// <param name="nanoFrameworkTypeFullName">Full name of the nanoFramework class or interface.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="attribute">The instance of the attribute to get the property of.</param>
        /// <exception cref="InvalidOperationException">Thrown if the property was found to be invalid when it was added,
        /// or if it was never added at all.</exception>
        public PropertyType GetPropertyValue<PropertyType>(string nanoFrameworkTypeFullName, string propertyName, object attribute)
        {
            return (PropertyType)GetPropertyValue(nanoFrameworkTypeFullName, propertyName, attribute);
        }

        /// <summary>
        /// Get the value of a property of a nanoFramework attribute.
        /// </summary>
        /// <param name="nanoFrameworkTypeFullName">Full name of the nanoFramework class or interface.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="attribute">The instance of the attribute to get the property of.</param>
        /// <exception cref="InvalidOperationException">Thrown if the property was found to be invalid when it was added,
        /// or if it was never added at all.</exception>
        public object GetPropertyValue(string nanoFrameworkTypeFullName, string propertyName, object attribute)
        {
            if (_properties.TryGetValue(nanoFrameworkTypeFullName, out Dictionary<string, PropertyInfo> properties))
            {
                if (properties.TryGetValue(propertyName, out PropertyInfo property) && !(property is null))
                {
                    return property.GetValue(attribute);
                }
            }
            throw new InvalidOperationException($"Unknown property '{propertyName}' of nanoFramework type '{nanoFrameworkTypeFullName}'.");
        }
        #endregion

        #region Methods for attribute methods
        /// <summary>
        /// Add the implementation information of a method of a nanoFramework class or interface.
        /// </summary>
        /// <typeparam name="ReturnType">Return type of the method.</typeparam>
        /// <param name="nanoFrameworkTypeFullName">Full name of the nanoFramework class or interface.</param>
        /// <param name="nanoFrameworkType">The .NET type that represents the nanoFramework class or interface.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="methodArguments">The types of the arguments of the method,</param>
        /// <exception cref="FrameworkMismatchException">Thrown if the method is not found in <paramref name="nanoFrameworkType"/>
        /// or if the <typeparamref name="ReturnType"/> or <paramref name="methodArguments"/> do not match the corresponding
        /// types of the method.</exception>
        public void AddMethod<ReturnType>(string nanoFrameworkTypeFullName, Type nanoFrameworkType, string methodName, params Type[] methodArguments)
        {
            AddMethod(nanoFrameworkTypeFullName, nanoFrameworkType, typeof(ReturnType), methodName, methodArguments);
        }

        /// <summary>
        /// Add the implementation information of a method of a nanoFramework class or interface
        /// that does not return a value.
        /// </summary>
        /// <param name="nanoFrameworkTypeFullName">Full name of the nanoFramework class or interface.</param>
        /// <param name="nanoFrameworkType">The .NET type that represents the nanoFramework class or interface.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="methodArguments">The types of the arguments of the method,</param>
        /// <exception cref="FrameworkMismatchException">Thrown if the method is not found in <paramref name="nanoFrameworkType"/>
        /// or if the <paramref name="methodArguments"/> do not match the corresponding
        /// types of the method.</exception>
        public void AddMethod(string nanoFrameworkTypeFullName, Type nanoFrameworkType, string methodName, params Type[] methodArguments)
        {
            AddMethod(nanoFrameworkTypeFullName, nanoFrameworkType, null, methodName, methodArguments);
        }

        /// <summary>
        /// Add the implementation information of a property of a nanoFramework class or interface.
        /// </summary>
        /// <param name="nanoFrameworkTypeFullName">Full name of the nanoFramework class or interface.</param>
        /// <param name="nanoFrameworkType">The .NET type that represents the nanoFramework class or interface.</param>
        /// <param name="returnType">Return type of the method.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="methodArguments">The types of the arguments of the method,</param>
        /// <exception cref="FrameworkMismatchException">Thrown if the method is not found in <paramref name="nanoFrameworkType"/>
        /// or if the <paramref name="returnType"/> or <paramref name="methodArguments"/> do not match the corresponding
        /// types of the method.</exception>
        public void AddMethod(string nanoFrameworkTypeFullName, Type nanoFrameworkType, Type returnType, string methodName, params Type[] methodArguments)
        {
            if (!_methods.TryGetValue(nanoFrameworkTypeFullName, out Dictionary<string, MethodInfo> methods))
            {
                _methods[nanoFrameworkTypeFullName] = methods = new Dictionary<string, MethodInfo>();
            }
            if (!methods.ContainsKey(methodName))
            {
                MethodInfo method = nanoFrameworkType.GetMethod(methodName);
                methods[methodName] = method;
                try
                {
                    if (method is null)
                    {
                        throw new FrameworkMismatchException($"The nanoFramework type '{nanoFrameworkTypeFullName}' has no public method '{methodName}'.");
                    }
                    else if (method.ReturnType != returnType)
                    {
                        throw new FrameworkMismatchException($"The nanoFramework type of the value of method '{methodName}' of '{nanoFrameworkTypeFullName}' is '{method.ReturnType?.FullName ?? "void"}' instead of '{returnType?.FullName ?? "void"}'.");
                    }
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != methodArguments.Length)
                    {
                        throw new FrameworkMismatchException($"The method '{methodName}' of the nanoFramework type '{nanoFrameworkTypeFullName}' has {parameters.Length} instead of {methodArguments.Length} arguments.");
                    }
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].ParameterType != methodArguments[i])
                        {
                            throw new FrameworkMismatchException($"Argument '{parameters[i].Name}' of method '{methodName}' of the nanoFramework type '{nanoFrameworkTypeFullName}' is of type '{parameters[i].ParameterType.FullName}' instead of '{methodArguments[i].FullName}'.");
                        }
                    }
                }
                catch
                {
                    methods[methodName] = null;
                    throw;
                }
            }
        }

        /// <summary>
        /// Call a method of a nanoFramework attribute.
        /// </summary>
        /// <typeparam name="ReturnType">Type of the value of the property.</typeparam>
        /// <param name="nanoFrameworkTypeFullName">Full name of the nanoFramework class or interface.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="attribute">The instance of the attribute call the method of.</param>
        /// <param name="arguments">Arguments to pass to the method.</param>
        /// <exception cref="InvalidOperationException">Thrown if the property was found to be invalid when it was added,
        /// or if it was never added at all.</exception>
        public ReturnType CallMethod<ReturnType>(string nanoFrameworkTypeFullName, string methodName, object attribute, params object[] arguments)
        {
            return (ReturnType)CallMethod(nanoFrameworkTypeFullName, methodName, attribute, arguments);
        }

        /// <summary>
        /// Call a method of a nanoFramework attribute.
        /// </summary>
        /// <param name="nanoFrameworkTypeFullName">Full name of the nanoFramework class or interface.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="attribute">The instance of the attribute call the method of.</param>
        /// <param name="arguments">Arguments to pass to the method.</param>
        /// <exception cref="InvalidOperationException">Thrown if the method was found to be invalid when it was added,
        /// or if it was never added at all.</exception>
        public object CallMethod(string nanoFrameworkTypeFullName, string methodName, object attribute, params object[] arguments)
        {
            if (_methods.TryGetValue(nanoFrameworkTypeFullName, out Dictionary<string, MethodInfo> methods))
            {
                if (methods.TryGetValue(methodName, out MethodInfo method) && !(method is null))
                {
                    return method.Invoke(attribute, arguments);
                }
            }
            throw new InvalidOperationException($"Unknown method '{methodName}' of nanoFramework type '{nanoFrameworkTypeFullName}'.");
        }
        #endregion

        #region Internal methods
        /// <summary>
        /// This method has to be called by code that discovers an interface from the test framework.
        /// It is used to find the <see cref="nanoFramework.TestFramework.Tools.TestDeviceProxy"/> type
        /// in the assembly that implements the test framework.
        /// </summary>
        /// <param name="interfaceType">One of the interface types defined in the test framework</param>
        internal void FoundTestFrameworkInterface(Type interfaceType)
        {
            if (TestDeviceProxyType is null)
            {
                TestDeviceProxyType = (from type in interfaceType.Assembly.GetTypes()
                                       where type.FullName == typeof(nanoFramework.TestFramework.Tools.TestDeviceProxy).FullName
                                       select type).FirstOrDefault();

                if (!(TestDeviceProxyType is null))
                {
                    ITestDeviceType = (from type in interfaceType.Assembly.GetTypes()
                                       where type.FullName == typeof(nanoFramework.TestFramework.ITestDevice).FullName
                                       select type).FirstOrDefault();

                    if (ITestDeviceType is null)
                    {
                        ITestDeviceType = (from type in interfaceType.Assembly.GetTypes()
                                           where type.FullName == typeof(nanoFramework.TestFramework.ITestDevice).FullName
                                           select type).FirstOrDefault();

                        foreach (AssemblyName assemblyName in interfaceType.Assembly.GetReferencedAssemblies())
                        {
                            ITestDeviceType = (from type in (from a in AppDomain.CurrentDomain.GetAssemblies()
                                                             where a.GetName().FullName == assemblyName.FullName
                                                             select a).First().GetTypes()
                                               where type.FullName == typeof(nanoFramework.TestFramework.ITestDevice).FullName
                                               select type).FirstOrDefault();
                            if (!(ITestDeviceType is null))
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
