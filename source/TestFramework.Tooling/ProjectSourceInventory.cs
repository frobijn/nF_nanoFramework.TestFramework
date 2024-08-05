// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Analyzer to get the location of classes and methods and their attributes in the project source code.
    /// Information on all classes and methods is returned, not just test classes, test methods or attributes
    /// that are related to the nanoFramework test framework. The analyzer only works for nanoFramework projects
    /// where the project file explicitly lists the included source files.
    /// </summary>
    public sealed class ProjectSourceInventory
    {
        #region Fields
        private readonly Dictionary<string, ClassDeclaration> _classes = new Dictionary<string, ClassDeclaration>();
        #endregion

        #region Construction
        /// <summary>
        /// Get the relevant elements from the source files of a nanoFramework project
        /// </summary>
        /// <param name="projectFilePath">Path to the nanoFramework project file (*.nfproj)</param>
        /// <param name="logger">Method to pass information about the discovery process to the caller.</param>
        public ProjectSourceInventory(string projectFilePath, LogMessenger logger)
        {
            var sourceFiles = new List<string>();
            try
            {
                var nfProject = XDocument.Load(projectFilePath);
                if (!(nfProject.Root is null))
                {
                    sourceFiles.AddRange(from item in nfProject.Root.Descendants(nfProject.Root.Name.Namespace + "Compile")
                                         where !(item.Attribute("Include") is null)
                                         select item.Attribute("Include")?.Value);
                }
            }
            catch (Exception ex)
            {
                logger(LoggingLevel.Error, $"Cannot read the nanoFramework project '{projectFilePath}': {ex.Message}");
                return;
            }

            string projectDirectory = Path.GetDirectoryName(projectFilePath);
            foreach (string sourceFile in sourceFiles)
            {
                string sourceFilePath = Path.Combine(projectDirectory, sourceFile);
                if (!File.Exists(sourceFilePath))
                {
                    logger(LoggingLevel.Verbose, $"Source file not found: '{sourceFilePath}'");
                }
                else
                {
                    Analyze(sourceFilePath, File.ReadAllText(sourceFilePath), logger);
                }
            }
        }

        /// <summary>
        /// Get the relevant elements from the source files of a nanoFramework project
        /// </summary>
        /// <param name="sourceFiles">Enumeration of the project's source files, with the path and content of each file</param>
        /// <param name="logger">Method to pass information about the discovery process to the caller.</param>
        public ProjectSourceInventory(IEnumerable<(string sourceFilePath, string sourceCode)> sourceFiles, LogMessenger logger)
        {
            foreach ((string sourceFilePath, string sourceCode) in sourceFiles)
            {
                Analyze(sourceFilePath, sourceCode, logger);
            }
        }

        /// <summary>
        /// Analyze a single source file
        /// </summary>
        /// <param name="sourceFilePath">Path to the source file</param>
        /// <param name="sourceCode">Content of the source file</param>
        /// <param name="logger">Method to pass information about the discovery process to the caller.</param>
        private void Analyze(string sourceFilePath, string sourceCode, LogMessenger logger)
        {
            SyntaxNode programTree;
            try
            {
                programTree = CSharpSyntaxTree.ParseText(sourceCode)?.GetRoot();
            }
            catch (Exception ex)
            {
                logger(LoggingLevel.Verbose, $"Cannot analyse source code in '{sourceFilePath}': {ex.Message}");
                return;
            }
            if (programTree is null)
            {
                return;
            }

            foreach (NamespaceDeclarationSyntax namespaceInSource in programTree.DescendantNodes().OfType<NamespaceDeclarationSyntax>())
            {
                foreach (ClassDeclarationSyntax classInSource in namespaceInSource.ChildNodes().OfType<ClassDeclarationSyntax>())
                {
                    string fullClassName = $"{namespaceInSource.Name}.{classInSource.Identifier.ValueText}";

                    // Only remember the first location of a partial class
                    if (!_classes.TryGetValue(fullClassName, out ClassDeclaration classDeclaration))
                    {
                        classDeclaration = new ClassDeclaration(fullClassName, sourceFilePath, classInSource.GetLocation());
                        _classes[fullClassName] = classDeclaration;
                    }

                    foreach (AttributeListSyntax list in classInSource.AttributeLists)
                    {
                        foreach (AttributeSyntax attribute in list.Attributes)
                        {
                            // Assume that attributes of a test class in the source code and type information are in the same order
                            // This is true for non-partial classes, not sure about partial classes
                            classDeclaration._attributes.Add(new ElementDeclaration(attribute.Name.ToString().Split('.').Last(), sourceFilePath, attribute.GetLocation()));
                        }
                    }

                    foreach (MethodDeclarationSyntax methodInSource in classInSource.ChildNodes().OfType<MethodDeclarationSyntax>())
                    {
                        string methodName = methodInSource.Identifier.ValueText;
                        var methodDeclaration = new MethodDeclaration(methodName, sourceFilePath, methodInSource.Identifier.GetLocation());
                        classDeclaration._methods.Add(methodDeclaration);

                        foreach (AttributeListSyntax list in methodInSource.AttributeLists)
                        {
                            foreach (AttributeSyntax attribute in list.Attributes)
                            {
                                methodDeclaration._attributes.Add(new ElementDeclaration(attribute.Name.ToString().Split('.').Last(), sourceFilePath, attribute.GetLocation()));
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Extra type declarations
        /// <summary>
        /// Information about the location of a source code element in a source code file
        /// </summary>
        public class ElementDeclaration
        {
            #region Construction
            /// <summary>
            /// Create the declaration information
            /// </summary>
            /// <param name="name"></param>
            /// <param name="sourceFilePath"></param>
            /// <param name="location"></param>
            internal ElementDeclaration(string name, string sourceFilePath, Location location)
            {
                Name = name;
                SourceFilePath = sourceFilePath;
                LinePosition start = location.GetLineSpan().StartLinePosition;
                LineNumber = start.Line;
                Position = start.Character;
            }
            #endregion

            #region Properties and methods
            /// <summary>
            /// Name of the element
            /// </summary>
            public string Name
            {
                get;
            }

            /// <summary>
            /// Path of the source file the element is defined in
            /// </summary>
            public string SourceFilePath
            {
                get;
            }

            /// <summary>
            /// Line number in the source file (0 = first line)
            /// </summary>
            public int LineNumber
            {
                get;
            }

            /// <summary>
            /// Position of the first character of the element in the source file (0 = start of line)
            /// </summary>
            public int Position
            {
                get;
            }

            /// <summary>
            /// Get the position to use in a message
            /// </summary>
            /// <returns></returns>
            public string ForMessage()
                => $"{SourceFilePath}({LineNumber + 1},{Position + 1})";
            #endregion
        }

        /// <summary>
        /// Information about a method declaration in a source code file.
        /// </summary>
        public sealed class MethodDeclaration : ElementDeclaration
        {
            #region Construction
            /// <summary>
            /// Create the method declaration information
            /// </summary>
            /// <param name="name"></param>
            /// <param name="sourceFilePath"></param>
            /// <param name="location"></param>
            internal MethodDeclaration(string name, string sourceFilePath, Location location)
                : base(name, sourceFilePath, location)
            {
            }
            #endregion

            #region Properties
            /// <summary>
            /// Get a list of attributes that are applied to the method
            /// </summary>
            public IReadOnlyList<ElementDeclaration> Attributes
                => _attributes;
            internal readonly List<ElementDeclaration> _attributes = new List<ElementDeclaration>();
            #endregion
        }

        /// <summary>
        /// Information about a class declaration in a source code file. For partial classes
        /// the location is the location of the first declaration encountered; the methods
        /// of all declarations are merged.
        /// </summary>
        public sealed class ClassDeclaration : ElementDeclaration
        {
            #region Construction
            /// <summary>
            /// Create the class declaration information
            /// </summary>
            /// <param name="fullClassName"></param>
            /// <param name="sourceFilePath"></param>
            /// <param name="location"></param>
            public ClassDeclaration(string fullClassName, string sourceFilePath, Location location)
                : base(fullClassName, sourceFilePath, location)
            {
            }
            #endregion

            #region Properties
            /// <summary>
            /// Get a list of attributes that are applied to the class
            /// </summary>
            public IReadOnlyList<ElementDeclaration> Attributes
                => _attributes;
            internal readonly List<ElementDeclaration> _attributes = new List<ElementDeclaration>();

            /// <summary>
            /// Get a list of methods declared by the class
            /// </summary>
            public IReadOnlyList<MethodDeclaration> Methods
                => _methods;
            internal readonly List<MethodDeclaration> _methods = new List<MethodDeclaration>();
            #endregion
        }
        #endregion

        #region Properties / methods
        /// <summary>
        /// Get all classes defined in the project
        /// </summary>
        public IEnumerable<ClassDeclaration> ClassDeclarations
            => _classes.Values;

        /// <summary>
        /// Get the declaration of a class
        /// </summary>
        /// <param name="fullClassName"></param>
        /// <returns></returns>
        public ClassDeclaration TryGet(string fullClassName)
        {
            _classes.TryGetValue(fullClassName, out ClassDeclaration result);
            return result;
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Find the nanoFramework project file that was used to create assembly
        /// </summary>
        /// <param name="assemblyFilePath">Path to the assembly. The assembly must be located within the project directory
        /// of the project that created or uses the assembly.</param>
        /// <param name="logger">Method to pass messages to the caller.</param>
        /// <returns></returns>
        public static string FindProjectFilePath(string assemblyFilePath, LogMessenger logger)
        {
            string assemblyName = Path.GetFileNameWithoutExtension(assemblyFilePath);
            string candidateDirectoryPath = Path.GetDirectoryName(assemblyFilePath);
            while (true)
            {
                if (Directory.Exists(candidateDirectoryPath))
                {
                    // Check the nanoFramework projects first
                    foreach (string projectFilePath in Directory.EnumerateFiles(candidateDirectoryPath, "*.nfproj"))
                    {
                        try
                        {
                            var projectRoot = XDocument.Load(projectFilePath);
                            if (!(projectRoot.Root is null))
                            {
                                foreach (XElement propertyGroup in projectRoot.Root.Elements(projectRoot.Root.Name.Namespace + "PropertyGroup"))
                                {
                                    if ((from property in propertyGroup.Elements(projectRoot.Root.Name.Namespace + "AssemblyName")
                                         where property.Value == assemblyName
                                         select property).Any())
                                    {
                                        return projectFilePath;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.Invoke(LoggingLevel.Detailed, $"Cannot read the XML for the project '{projectFilePath}': {ex.Message}");
                        }

                        // The default for the assembly name is based on the project file name
                        if (Path.GetFileNameWithoutExtension(projectFilePath) == assemblyName)
                        {
                            return projectFilePath;
                        }
                    }

                    foreach (string projectFilePath in Directory.EnumerateFiles(candidateDirectoryPath))
                    {
                        if (Path.GetExtension(projectFilePath)?.EndsWith("proj") ?? false)
                        {
                            // Assume that the assembly is part of another type of project, and the source code is not available
                            return null;
                        }
                    }
                }

                string newCandidateDirectoryPath = Path.GetDirectoryName(candidateDirectoryPath);
                if (string.IsNullOrEmpty(newCandidateDirectoryPath) || newCandidateDirectoryPath == candidateDirectoryPath)
                {
                    return null;
                }
                candidateDirectoryPath = newCandidateDirectoryPath;
            }
        }

        /// <summary>
        /// Enumerate all static and instantiable classes in the assembly.
        /// </summary>
        /// <param name="assembly">Assembly to enumerate the classes for</param>
        /// <param name="sourceInventory">Locations of the classes, methods and attributes in the source for the assembly. Pass <c>null</c> is no source is available.</param>
        /// <returns>
        /// Returns an enumeration of the class type and the location of its declaration in the source code. Also included is a function that can be used to
        /// enumerate the class methods and the declaration of the method in the source. For inherited methods no source code location is available. The source code location
        /// of the method declaration is unreliable if the method has overloads (same method name but different argument lists).
        /// </returns>
        public static IEnumerable<(
            Type classType,
            ClassDeclaration sourceLocation,
            Func<IEnumerable<(MethodInfo method, MethodDeclaration sourceLocation)>> enumerateMethods)> EnumerateNonAbstractClasses(Assembly assembly, ProjectSourceInventory sourceInventory)
        {
            foreach (Type classType in assembly.GetTypes())
            {
                if (classType.IsGenericTypeDefinition ||
                    (classType.IsAbstract && !classType.IsSealed) // Abstract and sealed is a static class
                    )
                {
                    continue;
                }

                ClassDeclaration classDeclaration = sourceInventory?.TryGet(classType.FullName);
                yield return (
                    classType,
                    classDeclaration,
                    () => EnumerateClassMethods(classType, classDeclaration)
                );
            }
        }
        private static IEnumerable<(MethodInfo method, MethodDeclaration sourceLocation)> EnumerateClassMethods(Type classType, ClassDeclaration classDeclaration)
        {
            List<MethodDeclaration> remainingSourceLocations = classDeclaration is null ? null : new List<MethodDeclaration>(classDeclaration.Methods);

            foreach (MethodInfo method in classType.GetMethods((classType.IsAbstract && classType.IsSealed ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public))
            {
                #region Find the position in the source
                MethodDeclaration sourceLocation = null;
                if (method.DeclaringType == classType)
                {
                    if (!(remainingSourceLocations is null))
                    {
                        // Assume that the methods in the class's Type are listed in the same order as in the source code,
                        // at least for methods with the same name. This may not be true for overloaded methods that are located
                        // in multiple source files for a partial class.
                        for (int i = 0; i < remainingSourceLocations.Count; i++)
                        {
                            if (remainingSourceLocations[i].Name == method.Name)
                            {
                                sourceLocation = remainingSourceLocations[i];
                                remainingSourceLocations.RemoveAt(i);
                                if (remainingSourceLocations.Count == 0)
                                {
                                    remainingSourceLocations = null;
                                }
                                break;
                            }
                        }
                    }
                }
                #endregion

                yield return (method, sourceLocation);
            }
        }

        /// <summary>
        /// Enumerate all custom attributes of a method or class.
        /// </summary>
        /// <param name="element">Reflected information on the method or class</param>
        /// <param name="sourceLocations">The source code locations of the attributes of the method or class. Pass <c>null</c> is no source is available.</param>
        /// <returns>
        /// An enumeration of the custom attributes and corresponding source code locations. The source code location is unreliable if attributes with the same name are inherited
        /// from a base class.
        /// </returns>
        public static IEnumerable<(Attribute attribute, ElementDeclaration sourceLocation)> EnumerateCustomAttributes(ICustomAttributeProvider element, IEnumerable<ElementDeclaration> sourceLocations)
        {
            if (!(element is null))
            {
                List<ElementDeclaration> remainingSourceLocations = sourceLocations is null ? null : new List<ElementDeclaration>(sourceLocations);


#pragma warning disable IDE0220 // Add explicit cast - GetCustomAttributes is very old .NET and would have returned Attribute[] if it were newer
                foreach (Attribute attribute in element.GetCustomAttributes(true))
                {
                    #region Find the position in the source
                    string fullName = attribute.GetType().FullName;
                    ElementDeclaration attributeInSource = null;
                    if (!(remainingSourceLocations is null))
                    {
                        string attributeNameLong = attribute.GetType().Name;
                        string attributeNameShort = attributeNameLong.EndsWith("Attribute")
                            ? attributeNameLong.Substring(0, attributeNameLong.Length - "Attribute".Length)
                            : attributeNameLong;

                        // The assumption is that the custom attributes are listed in the same order as in the source,
                        // and the inherited attributes come last.
                        // Then it is possible to find the correct source location even if there are multiple attributes with the same name.
                        for (int i = 0; i < remainingSourceLocations.Count; i++)
                        {
                            if (remainingSourceLocations[i].Name == attributeNameShort || remainingSourceLocations[i].Name == attributeNameLong)
                            {
                                attributeInSource = remainingSourceLocations[i];
                                remainingSourceLocations.RemoveAt(i);
                                if (remainingSourceLocations.Count == 0)
                                {
                                    remainingSourceLocations = null;
                                }
                                break;
                            }
                        }
                    }
                    #endregion

                    yield return (attribute, attributeInSource);
                }
#pragma warning restore IDE0220 // Add explicit cast
            }
        }
        #endregion
    }
}
