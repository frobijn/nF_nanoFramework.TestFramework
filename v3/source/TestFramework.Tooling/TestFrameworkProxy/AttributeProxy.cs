// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// The nanoFramework.TestFramework.* types as used in the test assembly
    /// are not considered to be the same .NET types as in this assembly,
    /// as they are compiled for a different platform. The <see cref="AttributeProxy"/>
    /// is the base class of a proxy that lives in this .NET assembly and is able to interpret
    /// and access the matching attribute in the nanoCLR assembly.
    /// A selection of the nanoFramework.TestFramework.* types are included
    /// in this assembly to ensure consistency of definitions in both the .NET and nanoCLR platforms,
    /// but cannot be used to access information in the test assembly.
    /// </summary>
    public abstract class AttributeProxy
    {
        #region Fields
        private static readonly Dictionary<string, (bool forMethod, bool forClass, bool forAssembly, bool isCustom, AttributeProxyConstructor constructor)> s_attributeProxies = new Dictionary<string, (bool, bool, bool, bool, AttributeProxyConstructor)>();
        private static readonly Dictionary<string, (bool forMethod, bool forClass, bool forAssembly, bool isCustom, AttributeProxyConstructor constructor)> s_interfaceProxies = new Dictionary<string, (bool, bool, bool, bool, AttributeProxyConstructor)>()
        {
            { typeof (ICleanup).FullName, (true, false, false, false, (a,f,t) => new CleanupProxy ()) },
            { typeof (IDataRow).FullName, (true, false, false, false, (a,f,t) => new DataRowProxy (a, f, t)) },
            { typeof (IDeploymentConfiguration).FullName, (true, false, false, false, (a,f,t) => new DeploymentConfigurationProxy (a, f, t)) },
            { typeof (ISetup).FullName, (true, false, false, false, (a,f,t) => new SetupProxy ()) },
            { typeof (ITestClass).FullName, (false, true, false, false, (a,f,t) => new TestClassProxy (a, f, t)) },
            { typeof (ITestMethod).FullName, (true, false, false, false, (a,f,t) => new TestMethodProxy (a, f, t)) },
            { typeof (ITestOnRealHardware).FullName, (true, true, true, false, (a,f,t) => new TestOnRealHardwareProxy (a, f, t)) },
            { typeof (ITestOnVirtualDevice).FullName, (true, true, true, false, (a,f,t) => new TestOnVirtualDeviceProxy ()) },
            { typeof (ITestCategories).FullName, (true, true, true, false, (a,f,t) => new TestCategoriesProxy (a, f, t)) },
        };
        #endregion

        #region Registration
        /// <summary>
        /// Function to create a proxy for a nanoFramework attribute
        /// </summary>
        /// <param name="attribute">Attribute to create the proxy for.</param>
        /// <param name="framework">Information about the implementation of the test framework</param>
        /// <param name="nanoFrameworkType">Type corresponding to the type of the attribute or interface.</param>
        /// <returns>The created proxy.</returns>
        public delegate AttributeProxy AttributeProxyConstructor(object attribute, TestFrameworkImplementation framework, Type nanoFrameworkType);

        /// <summary>
        /// Register the constructor of a proxy for an attribute or interface.
        /// </summary>
        /// <param name="nanoFrameworkTypeFullName">Full type name of the nanoFramework attribute or interface.</param>
        /// <param name="isInterface">Indicates whether the type is an interface rather than an attribute type.</param>
        /// <param name="constructor">Constructor for the proxy.</param>
        /// <param name="forTestMethod">Indicates whether the attribute can be applied to a test method.</param>
        /// <param name="forTestClass">Indicates whether the attribute can be applied to a test class.</param>
        /// <param name="forTestAssembly">Indicates whether the attribute can be applied to a test assembly.</param>
        public static void Register(string nanoFrameworkTypeFullName, bool isInterface,
            AttributeProxyConstructor constructor,
            bool forTestMethod = true,
            bool forTestClass = false,
            bool forTestAssembly = false)
        {
            (isInterface ? s_interfaceProxies : s_attributeProxies)[nanoFrameworkTypeFullName]
                = (forTestMethod, forTestClass, forTestAssembly, true, constructor);
        }
        #endregion

        #region Construction of the various proxies
        /// <summary>
        /// Get proxies for the nanoFramework.TestFramework-related attributes for an assembly
        /// </summary>
        /// <param name="assembly">The assembly to get the proxies for</param>
        /// <param name="framework">Implementation details of the test framework used by the assembly.
        /// It will be updated (if needed) by this method.</param>
        /// <param name="logger">Method to pass a message to the caller</param>
        /// <returns>Proxies for the nanoFramework.TestFramework-related attributes</returns>
        public static (List<AttributeProxy> framework, List<AttributeProxy> custom) GetAssemblyAttributeProxies(Assembly assembly, TestFrameworkImplementation framework, LogMessenger logger)
        {
            var frameworkProxies = new List<AttributeProxy>();
            var customProxies = new List<AttributeProxy>();
            foreach (Type candidateAssemblyAttributesClass in assembly.GetTypes())
            {
                if (candidateAssemblyAttributesClass.IsClass
                    && candidateAssemblyAttributesClass.IsPublic
                    && (from t in candidateAssemblyAttributesClass.GetInterfaces()
                        where t.FullName == typeof(IAssemblyAttributes).FullName
                        select t).Any())
                {
                    (List<AttributeProxy> f, List<AttributeProxy> c) = GetAttributeProxies($"{candidateAssemblyAttributesClass.Assembly.GetName().Name}:{candidateAssemblyAttributesClass.FullName}",
                                                                            candidateAssemblyAttributesClass,
                                                                            ElementType.AssemblyAttributesClass,
                                                                            framework,
                                                                            null,
                                                                            logger);
                    frameworkProxies.AddRange(f);
                    customProxies.AddRange(c);
                }
            }
            return (frameworkProxies, customProxies);
        }

        /// <summary>
        /// Get proxies for the nanoFramework.TestFramework-related attributes for a candidate test class
        /// </summary>
        /// <param name="candidateTestClass">The class to get the proxies for</param>
        /// <param name="framework">Implementation details of the test framework used by the assembly that contains the class.
        /// It will be updated (if needed) by this method.</param>
        /// <param name="sourceAttributes">Source positions for the attributes; can be <c>null</c></param>
        /// <param name="logger">Method to pass a message to the caller</param>
        /// <returns>Proxies for the nanoFramework.TestFramework-related attributes. Returns an empty list if the <paramref name="candidateTestClass"/>
        /// is not suitable to be a test class (e.g., is an abstract non-static class).
        /// </returns>
        public static (List<AttributeProxy> framework, List<AttributeProxy> custom) GetClassAttributeProxies(Type candidateTestClass, TestFrameworkImplementation framework, IEnumerable<ProjectSourceInventory.ElementDeclaration> sourceAttributes, LogMessenger logger)
        {
            if (!candidateTestClass.IsClass
                || !candidateTestClass.IsPublic
                || (candidateTestClass.IsAbstract && !candidateTestClass.IsSealed) // abstract & sealed = static class
                || candidateTestClass.IsGenericTypeDefinition
                || (from t in candidateTestClass.GetInterfaces()
                    where t.FullName == nameof(IAssemblyAttributes)
                    select t).Any()
                )
            {
                return (new List<AttributeProxy>(), new List<AttributeProxy>());
            }
            else
            {
                return GetAttributeProxies(
                    $"{candidateTestClass.Assembly.GetName().Name}:{candidateTestClass.FullName}",
                    candidateTestClass,
                    ElementType.TestClass,
                    framework,
                    sourceAttributes,
                    logger);
            }
        }

        /// <summary>
        /// Get proxies for the nanoFramework.TestFramework-related attributes for a candidate test method
        /// </summary>
        /// <param name="candidateTestMethod">The method of a test class to get the proxies for</param>
        /// <param name="framework">Implementation details of the test framework used by the assembly that contains the method.
        /// It will be updated (if needed) by this method.</param>
        /// <param name="sourceAttributes">Source positions for the attributes; can be <c>null</c></param>
        /// <param name="logger">Method to pass a message to the caller</param>
        /// <returns>Proxies for the nanoFramework.TestFramework-related attributes</returns>
        public static (List<AttributeProxy> framework, List<AttributeProxy> custom) GetMethodAttributeProxies(MethodBase candidateTestMethod, TestFrameworkImplementation framework, IEnumerable<ProjectSourceInventory.ElementDeclaration> sourceAttributes, LogMessenger logger)
        {
            if (!candidateTestMethod.IsPublic
                || candidateTestMethod.IsAbstract
                || candidateTestMethod.IsGenericMethodDefinition)
            {
                return (new List<AttributeProxy>(), new List<AttributeProxy>());
            }
            return GetAttributeProxies(
                $"{candidateTestMethod.ReflectedType.Assembly.GetName().Name}:{candidateTestMethod.ReflectedType.FullName}.{candidateTestMethod.Name}",
                candidateTestMethod,
                ElementType.TestMethod,
                framework,
                sourceAttributes,
                logger);
        }

        private enum ElementType
        {
            AssemblyAttributesClass,
            TestClass,
            TestMethod
        }

        /// <summary>
        /// Get proxies for the nanoFramework.TestFramework-related attributes for an assembly, class or method
        /// </summary>
        /// <param name="pathToElement">The full name for the <paramref name="element"/> in the code</param>
        /// <param name="element">The assembly attributes class, test class or test method as obtained via reflection</param>
        /// <param name="elementType">Type of element the attributes are applied to</param>
        /// <param name="framework">Implementation details of the test framework used by the assembly that contains the element.
        /// It will be updated (if needed) by this method.</param>
        /// <param name="sourceAttributes">Source positions for the attributes; can be <c>null</c></param>
        /// <param name="logger">Method to pass a message to the caller</param>
        /// <returns>Proxies for the attributes, either custom or from the framework.</returns>
        private static (List<AttributeProxy> framework, List<AttributeProxy> custom) GetAttributeProxies(string pathToElement, ICustomAttributeProvider element, ElementType elementType, TestFrameworkImplementation framework, IEnumerable<ProjectSourceInventory.ElementDeclaration> sourceAttributes, LogMessenger logger)
        {
            var frameworkProxies = new List<AttributeProxy>();
            var customProxies = new List<AttributeProxy>();

            foreach ((object attribute, ProjectSourceInventory.ElementDeclaration attributeInSource) in ProjectSourceInventory.EnumerateCustomAttributes(element, sourceAttributes))
            {

                bool reportMissingSourceAttribute = attributeInSource is null && !(sourceAttributes is null);

                #region Proxy creation
                void CreateProxy(Dictionary<string, (bool forMethod, bool forClass, bool forAssembly, bool isCustom, AttributeProxyConstructor constructor)> registered, Type nanoFrameworkType)
                {
                    if (registered.TryGetValue(nanoFrameworkType.FullName, out (bool forMethod, bool forClass, bool forAssembly, bool isCustom, AttributeProxyConstructor constructor) registeredProxy))
                    {
                        bool isCorrect = true;
                        if (elementType == ElementType.TestMethod)
                        {
                            if (!registeredProxy.forMethod)
                            {
                                logger?.Invoke(LoggingLevel.Error, $"{attributeInSource?.ForMessage() ?? pathToElement}: Error: Attribute implementing '{nanoFrameworkType.FullName}' cannot be applied to a test method. Attribute is ignored.");
                                isCorrect = false;
                            }
                        }
                        else if (elementType == ElementType.TestClass)
                        {
                            if (!registeredProxy.forClass)
                            {
                                logger?.Invoke(LoggingLevel.Error, $"{attributeInSource?.ForMessage() ?? pathToElement}: Error: Attribute implementing '{nanoFrameworkType.FullName}' cannot be applied to a test class. Attribute is ignored.");
                                isCorrect = false;
                            }
                        }
                        else if (elementType == ElementType.AssemblyAttributesClass)
                        {
                            if (!registeredProxy.forAssembly)
                            {
                                logger?.Invoke(LoggingLevel.Error, $"{attributeInSource?.ForMessage() ?? pathToElement}: Error: Attribute implementing '{nanoFrameworkType.FullName}' cannot be applied to a class implementing '{nameof(IAssemblyAttributes)}'. Attribute is ignored.");
                                isCorrect = false;
                            }
                        }
                        if (isCorrect)
                        {
                            if (reportMissingSourceAttribute)
                            {
                                reportMissingSourceAttribute = false;
                                logger?.Invoke(LoggingLevel.Detailed, $"{pathToElement}: Warning: Location of attribute '{attribute.GetType().FullName}' in the source code cannot be determined.");
                            }

                            AttributeProxy proxy = registeredProxy.constructor(attribute, framework, nanoFrameworkType);
                            proxy.Source = attributeInSource;

                            if (registeredProxy.isCustom)
                            {
                                customProxies.Add(proxy);
                            }
                            else
                            {
                                frameworkProxies.Add(proxy);
                            }
                        }
                    }
                }
                #endregion

                CreateProxy(s_attributeProxies, attribute.GetType());

                foreach (Type attributeInterface in attribute.GetType().GetInterfaces())
                {
                    CreateProxy(s_interfaceProxies, attributeInterface);
                }
            }

            return (frameworkProxies, customProxies);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the position of the attribute in the source.
        /// Can be <c>null</c> if the position cannot be determined.
        /// </summary>
        public ProjectSourceInventory.ElementDeclaration Source
        {
            get;
            private set;
        }
        #endregion
    }
}
