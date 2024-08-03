// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
        #region Construction of the various proxies
        /// <summary>
        /// Get proxies for the nanoFramework.TestFramework-related attributes for an assembly
        /// </summary>
        /// <param name="assembly">The assembly to get the proxies for</param>
        /// <param name="logger">Method to pass a message to the caller</param>
        /// <returns>Proxies for the nanoFramework.TestFramework-related attributes</returns>
        public static List<AttributeProxy> GetAttributeProxies(Assembly assembly, LogMessenger logger)
        {
            return GetAttributeProxies(assembly.GetName().Name, assembly, null, logger);
        }

        /// <summary>
        /// Get proxies for the nanoFramework.TestFramework-related attributes for a candidate test class
        /// </summary>
        /// <param name="candidateTestClass">The class to get the proxies for</param>
        /// <param name="sourceAttributes">Source positions for the attributes; can be <c>null</c></param>
        /// <param name="logger">Method to pass a message to the caller</param>
        /// <returns>Proxies for the nanoFramework.TestFramework-related attributes. Returns an empty list if the <paramref name="candidateTestClass"/>
        /// is not suitable to be a test class (e.g., is an abstract non-static class).
        /// </returns>
        public static List<AttributeProxy> GetAttributeProxies(Type candidateTestClass, IEnumerable<ProjectSourceInventory.ElementDeclaration> sourceAttributes, LogMessenger logger)
        {
            if (!candidateTestClass.IsClass
                || (candidateTestClass.IsAbstract && !candidateTestClass.IsSealed) // abstract & sealed = static class
                || candidateTestClass.IsGenericTypeDefinition
                )
            {
                return new List<AttributeProxy>();
            }
            else
            {
                return GetAttributeProxies($"{candidateTestClass.Assembly.GetName().Name}:{candidateTestClass.FullName}", candidateTestClass, sourceAttributes, logger);
            }
        }

        /// <summary>
        /// Get proxies for the nanoFramework.TestFramework-related attributes for a candidate test method
        /// </summary>
        /// <param name="candidateTestMethod">The method of a test class to get the proxies for</param>
        /// <param name="sourceAttributes">Source positions for the attributes; can be <c>null</c></param>
        /// <param name="logger">Method to pass a message to the caller</param>
        /// <returns>Proxies for the nanoFramework.TestFramework-related attributes</returns>
        public static List<AttributeProxy> GetAttributeProxies(MethodBase candidateTestMethod, IEnumerable<ProjectSourceInventory.ElementDeclaration> sourceAttributes, LogMessenger logger)
        {
            return GetAttributeProxies($"{candidateTestMethod.ReflectedType.Assembly.GetName().Name}:{candidateTestMethod.ReflectedType.FullName}.{candidateTestMethod.Name}", candidateTestMethod, sourceAttributes, logger);
        }

        /// <summary>
        /// Get proxies for the nanoFramework.TestFramework-related attributes for an assembly, class or method
        /// </summary>
        /// <param name="element">The assembly (<see cref="Assembly"/>), class (<see cref="Type"/>) or method (<see cref="MethodInfo"/>) as obtained via reflection</param>
        /// <param name="sourceAttributes">Source positions for the attributes; can be <c>null</c></param>
        /// <param name="logger">Method to pass a message to the caller</param>
        /// <returns>Proxies for the nanoFramework.TestFramework-related attributes</returns>
        private static List<AttributeProxy> GetAttributeProxies(string pathToElement, ICustomAttributeProvider element, IEnumerable<ProjectSourceInventory.ElementDeclaration> sourceAttributes, LogMessenger logger)
        {
            var result = new List<AttributeProxy>();

            #region Asserts
            bool AssertElementIsClass(string attributeOrInterfaceName, ProjectSourceInventory.ElementDeclaration attributeInSource)
            {
                if (!(element is Type))
                {
                    logger?.Invoke(LoggingLevel.Error, $"{attributeInSource?.ForMessage() ?? pathToElement}: {attributeOrInterfaceName} can only be applied to a class. Attribute is ignored.");
                    return false;
                }
                return true;
            }

            bool AssertElementIsMethod(string attributeOrInterfaceName, ProjectSourceInventory.ElementDeclaration attributeInSource)
            {
                if (!(element is MethodBase))
                {
                    logger?.Invoke(LoggingLevel.Error, $"{attributeInSource?.ForMessage() ?? pathToElement}: {attributeOrInterfaceName} can only be applied to a method. Attribute is ignored.");
                    return false;
                }
                return true;
            }
            #endregion

            int numITestClassAttributes = 0;
            foreach ((Attribute attribute, ProjectSourceInventory.ElementDeclaration attributeInSource) in ProjectSourceInventory.EnumerateCustomAttributes(element, sourceAttributes))
            {
                string fullName = attribute.GetType().FullName;

                bool reportMissingSourceAttribute = attributeInSource is null && !(sourceAttributes is null);
                void ReportMissingSourceAttribute()
                {
                    if (reportMissingSourceAttribute)
                    {
                        reportMissingSourceAttribute = false;
                        logger?.Invoke(LoggingLevel.Detailed, $"{pathToElement}: location of attribute '{fullName}' in the source code cannot be determined.");
                    }
                }

                #region Fixed-name attributes
                if (fullName == typeof(CleanupAttribute).FullName)
                {
                    if (AssertElementIsMethod($"'{nameof(CleanupAttribute)}'", attributeInSource))
                    {
                        result.Add(new CleanupProxy()
                        {
                            Source = attributeInSource
                        });
                    }
                    ReportMissingSourceAttribute();
                    continue;
                }
                else if (fullName == typeof(SetupAttribute).FullName)
                {
                    if (AssertElementIsMethod($"'{nameof(SetupAttribute)}'", attributeInSource))
                    {
                        result.Add(new SetupProxy()
                        {
                            Source = attributeInSource
                        });
                    }
                    ReportMissingSourceAttribute();
                    continue;
                }
                #endregion

                #region Interfaces implemented by attributes
                foreach (Type attributeInterface in attribute.GetType().GetInterfaces())
                {
                    if (attributeInterface.FullName == typeof(IDataRow).FullName)
                    {
                        if (AssertElementIsMethod($"attribute implementing '{nameof(IDataRow)}'", attributeInSource))
                        {
                            result.Add(new DataRowProxy(attribute, attributeInterface)
                            {
                                Source = attributeInSource
                            });
                            ReportMissingSourceAttribute();
                        }
                    }
                    else if (attributeInterface.FullName == typeof(IRunInParallel).FullName)
                    {
                        result.Add(new RunInParallelProxy(attribute, attributeInterface)
                        {
                            Source = attributeInSource
                        });
                        ReportMissingSourceAttribute();
                    }
                    else if (attributeInterface.FullName == typeof(ITestClass).FullName)
                    {
                        if (AssertElementIsClass($"attribute implementing '{nameof(ITestClass)}'", attributeInSource))
                        {
                            if (numITestClassAttributes == 0)
                            {
                                result.Add(new TestClassProxy(element as Type, attribute, attributeInterface)
                                {
                                    Source = attributeInSource
                                });
                                ReportMissingSourceAttribute();
                            }
                            else if (numITestClassAttributes == 1)
                            {
                                logger?.Invoke(LoggingLevel.Error, $"{pathToElement}: only one attribute implementing '{nameof(ITestClass)}' is allowed. Subsequent attributes are ignored.");
                            }
                            numITestClassAttributes++;
                        }
                    }
                    else if (attributeInterface.FullName == typeof(ITestMethod).FullName)
                    {
                        if (AssertElementIsMethod($"attribute implementing '{nameof(ITestMethod)}'", attributeInSource))
                        {
                            result.Add(new TestMethodProxy(attribute, attributeInterface)
                            {
                                Source = attributeInSource
                            });
                            ReportMissingSourceAttribute();
                        }
                    }
                    else if (attributeInterface.FullName == typeof(ITestOnRealHardware).FullName)
                    {
                        var proxy = new TestOnRealHardwareProxy(attribute, attributeInterface)
                        {
                            Source = attributeInSource
                        };
                        ReportMissingSourceAttribute();

                        if (string.IsNullOrWhiteSpace(proxy.Description))
                        {
                            logger?.Invoke(LoggingLevel.Error, $"{attributeInSource?.ForMessage() ?? pathToElement}: '{nameof(ITestOnRealHardware)}.{nameof(ITestOnRealHardware.Description)}' of attribute '{fullName}' should return a non-empty string; attribute is ignored.");
                        }
                        else
                        {
                            result.Add(proxy);
                        }
                    }
                    else if (attributeInterface.FullName == typeof(ITestOnVirtualDevice).FullName)
                    {
                        result.Add(new TestOnVirtualDeviceProxy()
                        {
                            Source = attributeInSource
                        });
                        ReportMissingSourceAttribute();
                    }
                    else if (attributeInterface.FullName == typeof(ITraits).FullName)
                    {
                        if (AssertElementIsMethod($"attribute implementing '{nameof(ITraits)}'", attributeInSource))
                        {
                            result.Add(new TraitsProxy(attribute, attributeInterface)
                            {
                                Source = attributeInSource
                            });
                            ReportMissingSourceAttribute();
                        }
                    }
                }
                #endregion
            }

            return result;
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
